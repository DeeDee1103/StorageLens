# Configuration Strategy

## Executive Summary

StorageLens will implement **strongly-typed configuration with validation**, moving away from magic strings and hardcoded defaults:
- All configuration sections mapped to strongly-typed C# classes
- Configuration validation at startup (fail fast if misconfigured)
- Secrets managed via Azure Key Vault (never in appsettings.json)
- Environment-specific overrides supported cleanly
- Configuration unit tests to validate binding and defaults

**Current State**: 7.5/10 - Partially typed, hardcoded defaults, connection strings in appsettings  
**Target State**: 9.5/10 - Type-safe, validated, secure configuration

---

## 1. Base Configuration Classes Pattern

### 1.1 Core AppOptions Template

Create `AppOptions.cs` in the shared infrastructure or each service:

```csharp
namespace StorageLens.Services.Locations.Configuration;

/// <summary>
/// Root configuration options for Locations Service.
/// Bind from appsettings.json:Configuration section.
/// Validate at startup in Program.cs.
/// </summary>
public class LocationsServiceOptions
{
    public const string ConfigSectionName = "LocationsService";

    /// <summary>
    /// HTTP client timeout for external service calls.
    /// Default: 30 seconds. Should not exceed 60 seconds.
    /// </summary>
    public TimeSpan HttpClientTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum number of locations per tenant.
    /// Default: 100. Should validate >= 1 and <= 1000.
    /// </summary>
    public int MaxLocationsPerTenant { get; set; } = 100;

    /// <summary>
    /// Rate limiting configuration for location endpoints.
    /// </summary>
    public RateLimitingOptions RateLimiting { get; set; } = new();

    /// <summary>
    /// Database connection settings.
    /// Never contains actual connection string; uses Key Vault.
    /// </summary>
    public DatabaseOptions Database { get; set; } = new();

    /// <summary>
    /// Health check configuration.
    /// </summary>
    public HealthCheckOptions HealthCheck { get; set; } = new();

    /// <summary>
    /// Logging configuration (verbosity levels).
    /// </summary>
    public LoggingOptions Logging { get; set; } = new();

    /// <summary>
    /// Feature flags for gradual rollout.
    /// </summary>
    public FeatureFlagsOptions FeatureFlags { get; set; } = new();
}

/// <summary>
/// Rate limiting options for the service.
/// </summary>
public class RateLimitingOptions
{
    public bool Enabled { get; set; } = true;
    public int MaxRequestsPerMinute { get; set; } = 1000;
    public int MaxRequestsPerHour { get; set; } = 50000;
    public int BurstLimit { get; set; } = 100;
}

/// <summary>
/// Database-specific options.
/// Connection strings come from Key Vault, not appsettings.
/// </summary>
public class DatabaseOptions
{
    /// <summary>
    /// Whether to use an in-memory database for testing.
    /// Should never be true in production.
    /// </summary>
    public bool UseInMemory { get; set; } = false;

    /// <summary>
    /// Name of the database when using LocalDb.
    /// Example: "StorageLensLocations"
    /// </summary>
    public string LocalDbName { get; set; } = "StorageLensLocations";

    /// <summary>
    /// Whether to enable EF Core sensitive data logging (debug only).
    /// Must NEVER be true in production.
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; } = false;

    /// <summary>
    /// Maximum number of retries for transient database errors.
    /// Default: 3. Max: 10.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Initial delay for exponential backoff (milliseconds).
    /// Default: 100ms.
    /// </summary>
    public int InitialDelayMilliseconds { get; set; } = 100;

    /// <summary>
    /// Query timeout (seconds). Default: 30s.
    /// If query takes longer, it times out.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Health check configuration.
/// </summary>
public class HealthCheckOptions
{
    /// <summary>
    /// Timeout for health check probes (milliseconds).
    /// Default: 3 seconds.
    /// </summary>
    public int TimeoutMilliseconds { get; set; } = 3000;

    /// <summary>
    /// Whether to include detailed diagnostics in health response.
    /// Should be false in production for security.
    /// </summary>
    public bool IncludeDetailedDiagnostics { get; set; } = false;
}

/// <summary>
/// Logging configuration.
/// </summary>
public class LoggingOptions
{
    /// <summary>
    /// Minimum log level (Debug, Information, Warning, Error, Critical).
    /// Default: Information.
    /// </summary>
    public string MinimumLevel { get; set; } = "Information";

    /// <summary>
    /// Whether to include scopes in logs (correlation IDs, etc.).
    /// Default: true.
    /// </summary>
    public bool IncludeScopes { get; set; } = true;

    /// <summary>
    /// Whether to include request/response bodies in logs (debug only).
    /// Must NEVER be true in production.
    /// </summary>
    public bool LogRequestResponseBodies { get; set; } = false;
}

/// <summary>
/// Feature flags for gradual feature rollout.
/// </summary>
public class FeatureFlagsOptions
{
    /// <summary>
    /// Enable new API endpoint (percent of requests).
    /// 0 = disabled, 100 = fully enabled.
    /// </summary>
    public int NewLocationApiPercentage { get; set; } = 0;

    /// <summary>
    /// Enable batch location import feature.
    /// </summary>
    public bool BatchImportEnabled { get; set; } = false;

    /// <summary>
    /// Maximum file size for batch imports (bytes).
    /// Default: 10 MB.
    /// </summary>
    public long MaxBatchImportSizeBytes { get; set; } = 10 * 1024 * 1024;
}
```

---

## 2. Configuration Validation

### 2.1 IValidatableOptions Interface

```csharp
namespace StorageLens.Shared.Infrastructure.Configuration;

/// <summary>
/// Interface for configuration classes that need validation.
/// Implement to validate options at startup.
/// </summary>
public interface IValidatableOptions
{
    /// <summary>
    /// Validate configuration and throw if invalid.
    /// Called at startup via ValidateOptionsStartupFilter.
    /// </summary>
    void Validate();
}
```

### 2.2 Validation Implementation

```csharp
namespace StorageLens.Services.Locations.Configuration;

public class LocationsServiceOptions : IValidatableOptions
{
    // ... previous fields ...

    public void Validate()
    {
        ValidateHttpClientTimeout();
        ValidateMaxLocations();
        ValidateRateLimiting();
        ValidateDatabase();
        ValidateHealth();
        ValidateLogging();
        ValidateFeatureFlags();
    }

    private void ValidateHttpClientTimeout()
    {
        if (HttpClientTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                $"HttpClientTimeout must be greater than zero. Got: {HttpClientTimeout}");
        }

        if (HttpClientTimeout > TimeSpan.FromSeconds(60))
        {
            throw new InvalidOperationException(
                $"HttpClientTimeout must not exceed 60 seconds. Got: {HttpClientTimeout}");
        }
    }

    private void ValidateMaxLocations()
    {
        if (MaxLocationsPerTenant < 1 || MaxLocationsPerTenant > 1000)
        {
            throw new InvalidOperationException(
                $"MaxLocationsPerTenant must be between 1 and 1000. Got: {MaxLocationsPerTenant}");
        }
    }

    private void ValidateRateLimiting()
    {
        if (RateLimiting.MaxRequestsPerMinute < 1)
        {
            throw new InvalidOperationException(
                "RateLimiting.MaxRequestsPerMinute must be >= 1");
        }

        if (RateLimiting.MaxRequestsPerHour < RateLimiting.MaxRequestsPerMinute)
        {
            throw new InvalidOperationException(
                "RateLimiting.MaxRequestsPerHour must be >= MaxRequestsPerMinute");
        }

        if (RateLimiting.BurstLimit > RateLimiting.MaxRequestsPerMinute / 2)
        {
            throw new InvalidOperationException(
                "RateLimiting.BurstLimit should not exceed half of MaxRequestsPerMinute");
        }
    }

    private void ValidateDatabase()
    {
        if (Database.UseInMemory && !IsDevelopment())
        {
            throw new InvalidOperationException(
                "Database.UseInMemory must not be true outside development environment");
        }

        if (Database.MaxRetries < 0 || Database.MaxRetries > 10)
        {
            throw new InvalidOperationException(
                "Database.MaxRetries must be between 0 and 10");
        }

        if (Database.InitialDelayMilliseconds < 10)
        {
            throw new InvalidOperationException(
                "Database.InitialDelayMilliseconds must be >= 10");
        }

        if (Database.CommandTimeoutSeconds < 5 || Database.CommandTimeoutSeconds > 300)
        {
            throw new InvalidOperationException(
                "Database.CommandTimeoutSeconds must be between 5 and 300");
        }

        if (Database.EnableSensitiveDataLogging && !IsDevelopment())
        {
            throw new InvalidOperationException(
                "Database.EnableSensitiveDataLogging must not be true in production");
        }
    }

    private void ValidateHealth()
    {
        if (HealthCheck.TimeoutMilliseconds < 100)
        {
            throw new InvalidOperationException(
                "HealthCheck.TimeoutMilliseconds must be >= 100");
        }

        if (HealthCheck.IncludeDetailedDiagnostics && !IsDevelopment())
        {
            throw new InvalidOperationException(
                "HealthCheck.IncludeDetailedDiagnostics must not be true in production");
        }
    }

    private void ValidateLogging()
    {
        var validLevels = new[] { "Debug", "Information", "Warning", "Error", "Critical" };
        if (!validLevels.Contains(Logging.MinimumLevel, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Logging.MinimumLevel must be one of: {string.Join(", ", validLevels)}");
        }

        if (Logging.LogRequestResponseBodies && !IsDevelopment())
        {
            throw new InvalidOperationException(
                "Logging.LogRequestResponseBodies must not be true in production");
        }
    }

    private void ValidateFeatureFlags()
    {
        if (FeatureFlags.NewLocationApiPercentage < 0 || FeatureFlags.NewLocationApiPercentage > 100)
        {
            throw new InvalidOperationException(
                "FeatureFlags.NewLocationApiPercentage must be between 0 and 100");
        }

        if (FeatureFlags.MaxBatchImportSizeBytes < 1024) // At least 1 KB
        {
            throw new InvalidOperationException(
                "FeatureFlags.MaxBatchImportSizeBytes must be at least 1024 bytes");
        }
    }

    private static bool IsDevelopment()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? false;
    }
}
```

### 2.3 Startup Validation Filter

```csharp
namespace StorageLens.Shared.Infrastructure.Configuration;

/// <summary>
/// Startup filter that validates all IValidatableOptions at application startup.
/// Fails fast if configuration is invalid.
/// </summary>
public class ValidateOptionsStartupFilter : IStartupFilter
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ValidateOptionsStartupFilter> _logger;

    public ValidateOptionsStartupFilter(
        IServiceProvider serviceProvider,
        ILogger<ValidateOptionsStartupFilter> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            ValidateAllOptions();
            next(app);
        };
    }

    private void ValidateAllOptions()
    {
        // Find all registered IValidatableOptions in DI container
        var validatableTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(t => typeof(IValidatableOptions).IsAssignableFrom(t) && !t.IsInterface);

        foreach (var type in validatableTypes)
        {
            try
            {
                var instance = _serviceProvider.GetService(type) as IValidatableOptions;
                if (instance != null)
                {
                    instance.Validate();
                    _logger.LogInformation(
                        "Configuration validated successfully. Type: {OptionsType}",
                        type.Name);
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogCritical(
                    ex,
                    "Configuration validation failed. Type: {OptionsType}",
                    type.Name);
                throw;
            }
        }
    }
}
```

---

## 3. Program.cs Integration

### 3.1 Complete Setup Example

```csharp
// In StorageLens.Services.Locations/Program.cs

using StorageLens.Services.Locations.Configuration;
using StorageLens.Shared.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

var builder = WebApplicationBuilder.CreateBuilder(args);

// 1. Add configuration from appsettings.json
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// 2. Bind and register LocationsServiceOptions
builder.Services
    .Configure<LocationsServiceOptions>(builder.Configuration.GetSection(LocationsServiceOptions.ConfigSectionName))
    .AddSingleton(sp => sp.GetRequiredService<IOptions<LocationsServiceOptions>>().Value);

// 3. Add validation filter (will run at startup)
builder.Services.AddSingleton<IStartupFilter, ValidateOptionsStartupFilter>();

// 4. Add other services
builder.Services.AddCors();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCorrelationIdAccessor();
builder.Services.AddScoped<ILocationsService, LocationsService>();

// Add DbContext with options from configuration
builder.Services.AddDbContext<LocationsDbContext>((sp, options) =>
{
    var appOptions = sp.GetRequiredService<LocationsServiceOptions>();
    
    if (appOptions.Database.UseInMemory)
    {
        options.UseInMemoryDatabase("StorageLensLocations");
    }
    else
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not configured");
        
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.CommandTimeout(appOptions.Database.CommandTimeoutSeconds);
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: appOptions.Database.MaxRetries,
                maxRetryDelaySeconds: 30,
                errorNumbersToAdd: null);
        });
    }

    if (appOptions.Database.EnableSensitiveDataLogging)
    {
        options.EnableSensitiveDataLogging();
    }
});

var app = builder.Build();

// Middleware registration
app.UseGlobalExceptionHandler();
app.UseCorrelationId();
app.UseHttpRequestLogging();
app.UseRouting();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

---

## 4. appsettings.json Structure

### 4.1 Development Configuration

```json
{
  "LocationsService": {
    "HttpClientTimeout": "00:00:30",
    "MaxLocationsPerTenant": 100,
    "RateLimiting": {
      "Enabled": true,
      "MaxRequestsPerMinute": 1000,
      "MaxRequestsPerHour": 50000,
      "BurstLimit": 100
    },
    "Database": {
      "UseInMemory": false,
      "LocalDbName": "StorageLensLocations",
      "EnableSensitiveDataLogging": true,
      "MaxRetries": 3,
      "InitialDelayMilliseconds": 100,
      "CommandTimeoutSeconds": 30
    },
    "HealthCheck": {
      "TimeoutMilliseconds": 3000,
      "IncludeDetailedDiagnostics": true
    },
    "Logging": {
      "MinimumLevel": "Debug",
      "IncludeScopes": true,
      "LogRequestResponseBodies": true
    },
    "FeatureFlags": {
      "NewLocationApiPercentage": 50,
      "BatchImportEnabled": true,
      "MaxBatchImportSizeBytes": 52428800
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=StorageLensLocations;Integrated Security=true;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

### 4.2 Production Configuration

```json
{
  "LocationsService": {
    "HttpClientTimeout": "00:00:30",
    "MaxLocationsPerTenant": 100,
    "RateLimiting": {
      "Enabled": true,
      "MaxRequestsPerMinute": 1000,
      "MaxRequestsPerHour": 50000,
      "BurstLimit": 100
    },
    "Database": {
      "UseInMemory": false,
      "LocalDbName": "StorageLensLocations",
      "EnableSensitiveDataLogging": false,
      "MaxRetries": 5,
      "InitialDelayMilliseconds": 100,
      "CommandTimeoutSeconds": 60
    },
    "HealthCheck": {
      "TimeoutMilliseconds": 5000,
      "IncludeDetailedDiagnostics": false
    },
    "Logging": {
      "MinimumLevel": "Information",
      "IncludeScopes": true,
      "LogRequestResponseBodies": false
    },
    "FeatureFlags": {
      "NewLocationApiPercentage": 100,
      "BatchImportEnabled": true,
      "MaxBatchImportSizeBytes": 52428800
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

---

## 5. Configuration for All 8 Services

### Template for Each Service

Each service should follow this pattern:

- **Service-specific options class** (e.g., `DuplicatesServiceOptions`)
- **IValidatableOptions** implementation with validation rules
- **Configuration section** in appsettings.json matching the class name
- **Dependency injection** in Program.cs
- **Unit tests** for validation rules

### Services to Configure

1. `LocationsServiceOptions`
2. `ScanJobsServiceOptions`
3. `FileInventoryServiceOptions`
4. `DuplicatesServiceOptions`
5. `AnalyticsServiceOptions`
6. `ScannerServiceOptions`
7. `HashingServiceOptions`
8. `OrchestratorServiceOptions`

---

## 6. Key Vault Integration

### 6.1 Azure Key Vault Configuration

In production, connection strings and secrets come from Azure Key Vault, NOT appsettings.json:

```csharp
// In Program.cs, before building app

var keyVaultEndpoint = builder.Configuration["KeyVault:Endpoint"];
if (!string.IsNullOrEmpty(keyVaultEndpoint))
{
    var credential = new DefaultAzureCredential();
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultEndpoint),
        credential);
}
```

### 6.2 Key Vault Secrets Naming

Use standard naming convention:

```
StorageLens--LocationsService--Database--ConnectionString
StorageLens--LocationsService--ApiKey
StorageLens--LocationsService--JwtSecret
StorageLens--FileInventory--Database--ConnectionString
(etc. for each service)
```

### 6.3 Never Commit This

Add to `.gitignore`:

```
appsettings.*.json (except appsettings.Development.json)
*.pfx
*.key
secrets.json
```

---

## 7. Unit Testing Configuration

```csharp
namespace StorageLens.Services.Locations.Tests;

[TestClass]
public class ConfigurationValidationTests
{
    [TestMethod]
    public void LocationsServiceOptions_WithValidConfig_PassesValidation()
    {
        // Arrange
        var options = new LocationsServiceOptions
        {
            HttpClientTimeout = TimeSpan.FromSeconds(30),
            MaxLocationsPerTenant = 100,
            Database = new DatabaseOptions { CommandTimeoutSeconds = 30 }
        };

        // Act & Assert - Should not throw
        options.Validate();
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void LocationsServiceOptions_WithZeroTimeout_ThrowsException()
    {
        // Arrange
        var options = new LocationsServiceOptions
        {
            HttpClientTimeout = TimeSpan.Zero
        };

        // Act
        options.Validate();
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void LocationsServiceOptions_WithInvalidMaxLocations_ThrowsException()
    {
        // Arrange
        var options = new LocationsServiceOptions
        {
            MaxLocationsPerTenant = 0 // Invalid: must be >= 1
        };

        // Act
        options.Validate();
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void LocationsServiceOptions_WithProductionSensitiveLogging_ThrowsException()
    {
        // Arrange
        // Mock environment to return "Production"
        var options = new LocationsServiceOptions
        {
            Database = new DatabaseOptions { EnableSensitiveDataLogging = true }
        };

        // Act
        options.Validate();
    }

    [TestMethod]
    public void LocationsServiceOptions_BindsFromJson()
    {
        // Arrange
        var json = """
        {
          "LocationsService": {
            "MaxLocationsPerTenant": 50,
            "Database": {
              "CommandTimeoutSeconds": 45
            }
          }
        }
        """;

        var config = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
            .Build();

        // Act
        var options = new LocationsServiceOptions();
        config.GetSection("LocationsService").Bind(options);

        // Assert
        Assert.AreEqual(50, options.MaxLocationsPerTenant);
        Assert.AreEqual(45, options.Database.CommandTimeoutSeconds);
    }
}
```

---

## 8. Deployment Checklist

- [ ] Create service-specific `Options` classes for all 8 services
- [ ] Implement `IValidatableOptions` with comprehensive validation
- [ ] Configure each service's `appsettings.json` and `appsettings.Production.json`
- [ ] Update `Program.cs` for each service to bind and validate options
- [ ] Remove hardcoded defaults from service constructors
- [ ] Add Azure Key Vault configuration for connection strings
- [ ] Create unit tests for all configuration validation rules
- [ ] Document configuration options in service-specific READMEs
- [ ] Test configuration validation at startup (should fail fast if misconfigured)
- [ ] Verify environment variables override appsettings.json
- [ ] Test cross-environment configuration (dev vs production values)

---

## Success Criteria

- ✅ All configuration accessed via strongly-typed options classes
- ✅ No magic strings or hardcoded values in services
- ✅ Configuration validation fails at startup if invalid
- ✅ Connection strings never in appsettings.json (use Key Vault)
- ✅ All 8 services follow identical configuration pattern
- ✅ Unit tests validate all configuration rules
- ✅ Environment-specific overrides work correctly
- ✅ Feature flags enable gradual rollout of new features

