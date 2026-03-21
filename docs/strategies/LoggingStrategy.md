# Logging Strategy

## Executive Summary

StorageLens will implement **structured logging with correlation ID propagation** across all layers:
- Correlation IDs flowing through HTTP headers and service calls
- Structured, contextual logging in Controllers, Services, and Data Access layers
- Middleware for request/response logging with sensitive data sanitization
- Production-safe logging (no file paths, credentials, or personal data)
- Centralized log aggregation for incident debugging

**Current State**: 7.5/10 - Minimal structured logging, inconsistent correlation ID usage  
**Target State**: 9.5/10 - Comprehensive, auditable, production-safe logging

---

## 1. Correlation ID Propagation Pattern

### 1.1 HTTP Client Extension

Every HTTP call propagates correlation ID:

```csharp
namespace StorageLens.Shared.Infrastructure;

/// <summary>
/// Extension methods for HTTP clients to propagate correlation IDs.
/// </summary>
public static class CorrelationIdHttpClientExtensions
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    /// <summary>
    /// Creates an HTTP client configured with correlation ID propagation.
    /// Place in service's httpclient factory configuration.
    /// </summary>
    public static HttpClient AddCorrelationIdHandler(
        this HttpClient client,
        string correlationId)
    {
        client.DefaultRequestHeaders.Add(CorrelationIdHeader, correlationId);
        return client;
    }

    /// <summary>
    /// Adds correlation ID to outgoing request.
    /// </summary>
    public static HttpRequestMessage WithCorrelationId(
        this HttpRequestMessage request,
        string correlationId)
    {
        request.Headers.Add(CorrelationIdHeader, correlationId);
        return request;
    }

    /// <summary>
    /// Extracts correlation ID from HTTP context.
    /// Falls back to generating a new GUID if not present.
    /// </summary>
    public static string GetOrGenerateCorrelationId(this HttpContext context)
    {
        const string headerName = "X-Correlation-Id";

        if (context.Request.Headers.TryGetValue(headerName, out var existing))
        {
            return existing.ToString();
        }

        var newId = Guid.NewGuid().ToString();
        context.Response.Headers.Add(headerName, newId);
        return newId;
    }
}
```

### 1.2 Correlation ID Middleware

Add to `StorageLens.Shared.Infrastructure`:

```csharp
namespace StorageLens.Shared.Infrastructure;

/// <summary>
/// Middleware that ensures every request has a correlation ID.
/// Executes early in the pipeline to inject correlation ID into HttpContext.Items.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.GetOrGenerateCorrelationId();
        context.Items["CorrelationId"] = correlationId;

        // Add to response headers
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey("X-Correlation-Id"))
            {
                context.Response.Headers.Add("X-Correlation-Id", correlationId);
            }
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object>
               {
                   ["CorrelationId"] = correlationId,
                   ["RequestPath"] = context.Request.Path,
                   ["RequestMethod"] = context.Request.Method
               }))
        {
            await _next(context);
        }
    }
}

/// <summary>
/// Extension for middleware registration
/// </summary>
public static class CorrelationIdExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
```

### 1.3 Dependency Injection Helper

```csharp
namespace StorageLens.Shared.Infrastructure;

/// <summary>
/// Scoped service to capture correlation ID from current HTTP context.
/// Inject this into services to access correlation ID in business logic.
/// </summary>
public interface ICorrelationIdAccessor
{
    string GetCorrelationId();
}

public class HttpContextCorrelationIdAccessor : ICorrelationIdAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCorrelationIdAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetCorrelationId()
    {
        return (string?)_httpContextAccessor.HttpContext?.Items["CorrelationId"]
            ?? Guid.NewGuid().ToString();
    }
}

/// <summary>
/// Extension for DI registration in Program.cs
/// </summary>
public static class CorrelationIdExtensions
{
    public static IServiceCollection AddCorrelationIdAccessor(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICorrelationIdAccessor, HttpContextCorrelationIdAccessor>();
        return services;
    }
}
```

---

## 2. Structured Logging by Layer

### 2.1 Controller Layer

```csharp
namespace StorageLens.Services.Locations.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationsController : ControllerBase
{
    private readonly ILocationsService _service;
    private readonly ILogger<LocationsController> _logger;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;

    public LocationsController(
        ILocationsService service,
        ILogger<LocationsController> logger,
        ICorrelationIdAccessor correlationIdAccessor)
    {
        _service = service;
        _logger = logger;
        _correlationIdAccessor = correlationIdAccessor;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StorageLocationDto>> GetLocation(
        Guid id,
        CancellationToken cancellationToken)
    {
        var correlationId = _correlationIdAccessor.GetCorrelationId();

        _logger.LogInformation(
            "Getting location. LocationId: {LocationId}, CorrelationId: {CorrelationId}",
            id,
            correlationId);

        try
        {
            var location = await _service.GetLocationAsync(id, correlationId, cancellationToken);

            _logger.LogInformation(
                "Successfully retrieved location. LocationId: {LocationId}, LocationName: {LocationName}, CorrelationId: {CorrelationId}",
                id,
                location?.Name,
                correlationId);

            return Ok(location);
        }
        catch (ResourceNotFoundException ex)
        {
            _logger.LogWarning(
                "Location not found. LocationId: {LocationId}, CorrelationId: {CorrelationId}",
                id,
                correlationId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving location. LocationId: {LocationId}, CorrelationId: {CorrelationId}",
                id,
                correlationId);
            throw;
        }
    }

    [HttpPost("create")]
    public async Task<ActionResult<StorageLocationDto>> CreateLocation(
        CreateLocationRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = _correlationIdAccessor.GetCorrelationId();

        _logger.LogInformation(
            "Creating new location. LocationName: {LocationName}, LocationType: {LocationType}, CorrelationId: {CorrelationId}",
            request.Name,
            request.LocationType,
            correlationId);

        var result = await _service.CreateLocationAsync(request, correlationId, cancellationToken);

        _logger.LogInformation(
            "Location created successfully. LocationId: {LocationId}, LocationName: {LocationName}, CorrelationId: {CorrelationId}",
            result.Id,
            result.Name,
            correlationId);

        return CreatedAtAction(nameof(GetLocation), new { id = result.Id }, result);
    }
}
```

### 2.2 Service Layer

```csharp
namespace StorageLens.Services.Locations.Services;

public class LocationsService : ILocationsService
{
    private readonly LocationsDbContext _dbContext;
    private readonly ILogger<LocationsService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public LocationsService(
        LocationsDbContext dbContext,
        ILogger<LocationsService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _dbContext = dbContext;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<StorageLocationDto> CreateLocationAsync(
        CreateLocationRequest request,
        string correlationId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Service: Creating location. Name: {LocationName}, Type: {LocationType}, CorrelationId: {CorrelationId}",
            request.Name,
            request.LocationType,
            correlationId);

        var entity = new StorageLocation
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            RootPath = request.RootPath,
            LocationType = request.LocationType,
            Status = LocationStatus.Online,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.StorageLocations.Add(entity);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Service: Location created. LocationId: {LocationId}, Name: {LocationName}, CorrelationId: {CorrelationId}",
                entity.Id,
                entity.Name,
                correlationId);

            return Map(entity);
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(
                dbEx,
                "Service: Database error creating location. Name: {LocationName}, CorrelationId: {CorrelationId}",
                request.Name,
                correlationId);
            throw;
        }
    }

    public async Task<StorageLocationDto?> GetLocationAsync(
        Guid id,
        string correlationId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Service: Fetching location. LocationId: {LocationId}, CorrelationId: {CorrelationId}",
            id,
            correlationId);

        var entity = await _dbContext.StorageLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            _logger.LogInformation(
                "Service: Location not found. LocationId: {LocationId}, CorrelationId: {CorrelationId}",
                id,
                correlationId);

            return null;
        }

        _logger.LogInformation(
            "Service: Location found. LocationId: {LocationId}, Name: {LocationName}, Status: {Status}, CorrelationId: {CorrelationId}",
            entity.Id,
            entity.Name,
            entity.Status,
            correlationId);

        return Map(entity);
    }

    public async Task<DashboardKpiDto> GetDashboardKpisAsync(
        string correlationId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Service: Calculating dashboard KPIs. CorrelationId: {CorrelationId}",
            correlationId);

        var startTime = DateTime.UtcNow;

        try
        {
            var locationCount = await _dbContext.StorageLocations.CountAsync(cancellationToken);
            var totalCapacity = await _dbContext.StorageLocations
                .SumAsync(x => (long?)x.TotalCapacityBytes, cancellationToken) ?? 0;

            var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation(
                "Service: Dashboard KPIs calculated. LocationCount: {LocationCount}, TotalCapacity: {TotalCapacity}, ElapsedMs: {ElapsedMs}, CorrelationId: {CorrelationId}",
                locationCount,
                totalCapacity,
                elapsedMs,
                correlationId);

            return new DashboardKpiDto(
                LocationCount: locationCount,
                TotalCapacityBytes: totalCapacity);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Service: Error calculating dashboard KPIs. CorrelationId: {CorrelationId}",
                correlationId);
            throw;
        }
    }
}
```

### 2.3 Data Access Layer

```csharp
namespace StorageLens.Services.Locations.Data;

public class LocationsRepository : ILocationsRepository
{
    private readonly LocationsDbContext _dbContext;
    private readonly ILogger<LocationsRepository> _logger;

    public LocationsRepository(
        LocationsDbContext dbContext,
        ILogger<LocationsRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<StorageLocation?> GetByIdAsync(
        Guid id,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        _logger.LogDebug(
            "Data: Querying location by ID. LocationId: {LocationId}, CorrelationId: {CorrelationId}",
            id,
            correlationId);

        try
        {
            var entity = await _dbContext.StorageLocations
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;

            if (entity is null)
            {
                _logger.LogDebug(
                    "Data: Location not found. LocationId: {LocationId}, ElapsedMs: {ElapsedMs}, CorrelationId: {CorrelationId}",
                    id,
                    elapsedMs,
                    correlationId);
                return null;
            }

            _logger.LogDebug(
                "Data: Location retrieved. LocationId: {LocationId}, ElapsedMs: {ElapsedMs}, CorrelationId: {CorrelationId}",
                id,
                elapsedMs,
                correlationId);

            return entity;
        }
        catch (Exception ex)
        {
            var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(
                ex,
                "Data: Error querying location. LocationId: {LocationId}, ElapsedMs: {ElapsedMs}, CorrelationId: {CorrelationId}",
                id,
                elapsedMs,
                correlationId);
            throw;
        }
    }

    public async Task<IReadOnlyList<StorageLocation>> GetAllAsync(
        string correlationId,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        _logger.LogDebug(
            "Data: Querying all locations. CorrelationId: {CorrelationId}",
            correlationId);

        try
        {
            var entities = await _dbContext.StorageLocations
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);

            var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogDebug(
                "Data: Locations retrieved. Count: {LocationCount}, ElapsedMs: {ElapsedMs}, CorrelationId: {CorrelationId}",
                entities.Count,
                elapsedMs,
                correlationId);

            return entities;
        }
        catch (Exception ex)
        {
            var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(
                ex,
                "Data: Error querying locations. ElapsedMs: {ElapsedMs}, CorrelationId: {CorrelationId}",
                elapsedMs,
                correlationId);
            throw;
        }
    }
}
```

---

## 3. Request/Response Logging Middleware

### 3.1 HttpRequestLoggingMiddleware

```csharp
namespace StorageLens.Shared.Infrastructure;

/// <summary>
/// Middleware that logs incoming HTTP requests and outgoing responses.
/// Captures method, path, status code, and response time.
/// Sanitizes sensitive data (file paths, credentials).
/// </summary>
public class HttpRequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HttpRequestLoggingMiddleware> _logger;
    private readonly SensitiveDataSanitizer _sanitizer;

    public HttpRequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<HttpRequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _sanitizer = new SensitiveDataSanitizer();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = (string?)context.Items["CorrelationId"] ?? Guid.NewGuid().ToString();
        var requestStartTime = DateTime.UtcNow;

        LogRequest(context, correlationId);

        // Swap response stream to capture response
        var originalResponseBody = context.Response.Body;

        using (var responseBuffer = new MemoryStream())
        {
            context.Response.Body = responseBuffer;

            try
            {
                await _next(context);

                await LogResponseAsync(context, correlationId, requestStartTime);

                // Copy response back to original stream
                await responseBuffer.CopyToAsync(originalResponseBody);
            }
            finally
            {
                context.Response.Body = originalResponseBody;
            }
        }
    }

    private void LogRequest(HttpContext context, string correlationId)
    {
        var path = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;
        var queryString = context.Request.QueryString.Value ?? "";

        // Sanitize query string for sensitive data
        var sanitizedQueryString = _sanitizer.SanitizeQueryString(queryString);

        _logger.LogInformation(
            "Incoming HTTP request. Method: {Method}, Path: {Path}, QueryString: {QueryString}, CorrelationId: {CorrelationId}",
            method,
            path,
            sanitizedQueryString,
            correlationId);
    }

    private async Task LogResponseAsync(
        HttpContext context,
        string correlationId,
        DateTime requestStartTime)
    {
        var elapsed = (DateTime.UtcNow - requestStartTime).TotalMilliseconds;
        var statusCode = context.Response.StatusCode;
        var path = context.Request.Path.Value ?? "/";

        _logger.LogInformation(
            "Outgoing HTTP response. StatusCode: {StatusCode}, Path: {Path}, ElapsedMs: {ElapsedMs}, CorrelationId: {CorrelationId}",
            statusCode,
            path,
            elapsed,
            correlationId);

        await Task.CompletedTask;
    }
}

/// <summary>
/// Sanitizes sensitive data from logs (file paths, credentials, API keys, etc.)
/// </summary>
public class SensitiveDataSanitizer
{
    private readonly List<string> _sensitivePatterns = new()
    {
        "password",
        "apikey",
        "api_key",
        "token",
        "authorization",
        "credentials",
        "secret",
        "clientsecret",
        "connectionstring"
    };

    private readonly List<string> _pathPrefixesToRedact = new()
    {
        "C:\\",
        "D:\\",
        "E:\\",
        "\\\\",
        "/home/",
        "/root/",
        "/var/"
    };

    public string SanitizeQueryString(string queryString)
    {
        if (string.IsNullOrEmpty(queryString))
            return queryString;

        // Parse query parameters and redact sensitive ones
        var parts = queryString.TrimStart('?').Split('&');
        var sanitizedParts = parts.Select(part =>
        {
            var keyValue = part.Split('=');
            if (keyValue.Length == 2)
            {
                var key = keyValue[0].ToLowerInvariant();
                var value = keyValue[1];

                if (_sensitivePatterns.Any(p => key.Contains(p)))
                {
                    return $"{keyValue[0]}=[REDACTED]";
                }

                // Redact file paths
                if (IsFilePath(value))
                {
                    return $"{keyValue[0]}=[REDACTED_PATH]";
                }
            }

            return part;
        });

        return "?" + string.Join("&", sanitizedParts);
    }

    public string SanitizeLogMessage(string message)
    {
        var result = message;

        foreach (var pattern in _sensitivePatterns)
        {
            result = RedactPattern(result, pattern);
        }

        foreach (var prefix in _pathPrefixesToRedact)
        {
            result = RedactPaths(result, prefix);
        }

        return result;
    }

    private string RedactPattern(string text, string pattern)
    {
        // Find pattern followed by a value and redact the value
        var regex = new System.Text.RegularExpressions.Regex(
            $@"{pattern}\s*[:=]\s*[\w\-\.]+",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return regex.Replace(text, $"{pattern}=[REDACTED]");
    }

    private string RedactPaths(string text, string prefix)
    {
        var startIndex = 0;
        while ((startIndex = text.IndexOf(prefix, startIndex, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            // Find the end of the path (space, quote, or end of string)
            var endIndex = startIndex + prefix.Length;
            while (endIndex < text.Length && !char.IsWhiteSpace(text[endIndex]) && text[endIndex] != '"' && text[endIndex] != '\'')
            {
                endIndex++;
            }

            text = text.Remove(startIndex, endIndex - startIndex).Insert(startIndex, "[REDACTED_PATH]");
            startIndex += "[REDACTED_PATH]".Length;
        }

        return text;
    }

    private bool IsFilePath(string value)
    {
        return _pathPrefixesToRedact.Any(prefix => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}

public static class HttpRequestLoggingExtensions
{
    public static IApplicationBuilder UseHttpRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<HttpRequestLoggingMiddleware>();
    }
}
```

---

## 4. Logging Configuration in appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.EntityFrameworkCore": "Information",
      "StorageLens": "Debug"
    },
    "Console": {
      "IncludeScopes": true,
      "TimestampFormat": "yyyy-MM-ddTHH:mm:ss.fffffffZ"
    }
  }
}
```

---

## 5. Sensitive Data Logging Rules

### 5.1 What NOT to Log

❌ File paths (scanner discovers sensitive directory structures)  
❌ Full file names in warnings (may contain PII)  
❌ Database connection strings  
❌ API keys or bearer tokens  
❌ User credentials  
❌ Full error stack traces in production  
❌ Query string with file paths  

### 5.2 Safe Logging Alternatives

✅ File count instead of file names  
✅ Hashed file paths (use SHA-256)  
✅ Generic errors ("File access failed") instead of specific path messages  
✅ Error codes instead of full exceptions  
✅ Correlation IDs for debugging lookup  

### 5.3 File Path Hashing Example

```csharp
namespace StorageLens.Shared.Infrastructure;

public class FilePath
{
    public static string GetHashedPath(string filePath)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(filePath));
            return Convert.ToHexString(hash)[..8]; // First 8 chars
        }
    }
}

// Usage in Scanner
_logger.LogWarning(
    "File access denied. FilePathHash: {FilePathHash}, Error: {Error}, CorrelationId: {CorrelationId}",
    FilePath.GetHashedPath(problematicPath),
    "Access is denied",
    correlationId);
```

---

## 6. Program.cs Registration

```csharp
// In each service's Program.cs

var builder = WebApplicationBuilder.CreateBuilder(args);

// Configure logging
builder.Logging
    .ClearProviders()
    .AddConsole()
    .AddDebug();

// Add services
builder.Services.AddHttpContextAccessor();
builder.Services.AddCorrelationIdAccessor();

var app = builder.Build();

// Add middleware (in order)
app.UseCorrelationId();              // Must be early to inject correlation ID
app.UseHttpRequestLogging();         // Log all requests/responses
app.UseGlobalExceptionHandler();     // Exception handler with correlation ID
app.UseRouting();

app.MapControllers();

app.Run();
```

---

## 7. Testing Structured Logging

```csharp
namespace StorageLens.Services.Locations.Tests;

[TestClass]
public class LoggingTests
{
    private Mock<ILogger<LocationsService>> _mockLogger = null!;
    private LocationsService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<LocationsService>>();
        _service = new LocationsService(
            new Mock<LocationsDbContext>().Object,
            _mockLogger.Object,
            new Mock<IHttpClientFactory>().Object);
    }

    [TestMethod]
    public async Task CreateLocationAsync_LogsCorrelationId()
    {
        // Arrange
        var request = new CreateLocationRequest { Name = "Test", RootPath = "C:\\test" };
        var correlationId = Guid.NewGuid().ToString();

        // Act
        // (This will throw because dbContext is mocked, but logs will be captured)
        try
        {
            await _service.CreateLocationAsync(request, correlationId, CancellationToken.None);
        }
        catch { }

        // Assert - Verify logging calls include correlation ID
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(correlationId)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task GetLocationAsync_LogsPerformanceMetrics()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString();

        // Act
        try
        {
            await _service.GetLocationAsync(locationId, correlationId, CancellationToken.None);
        }
        catch { }

        // Assert - Verify elapsed time is logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ElapsedMs")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
```

---

## 8. Log Aggregation and Queries

### 8.1 Sample Log Lines

```
2026-03-20T14:25:32.123Z [INF] Incoming HTTP request. Method: POST, Path: /api/locations/create, QueryString: , CorrelationId: 550e8400-e29b-41d4-a716-446655440000

2026-03-20T14:25:32.125Z [INF] Service: Creating location. Name: ArchiveShare, Type: FileShare, CorrelationId: 550e8400-e29b-41d4-a716-446655440000

2026-03-20T14:25:32.245Z [DBG] Data: Location created in database. LocationId: 550e8400-e29b-41d4-a716-446655440001, ElapsedMs: 120, CorrelationId: 550e8400-e29b-41d4-a716-446655440000

2026-03-20T14:25:32.246Z [INF] Outgoing HTTP response. StatusCode: 201, Path: /api/locations/create, ElapsedMs: 114, CorrelationId: 550e8400-e29b-41d4-a716-446655440000
```

### 8.2 Useful Log Queries (for Application Insights or ELK)

```
// Find all requests matching a correlation ID
CorrelationId = "550e8400-e29b-41d4-a716-446655440000" | sort by Timestamp desc

// Find slow requests (>500ms)
ElapsedMs > 500 | project Timestamp, Path, Method, ElapsedMs, CorrelationId

// Find all errors for a specific service
ServiceName = "FileInventory" and Level = "Error" | stats count() by ErrorCode

// Find file path redaction usage
"[REDACTED_PATH]" | stats count()

// Performance analysis by endpoint
Path: "/api/*" | stats avg(ElapsedMs), max(ElapsedMs), pct95(ElapsedMs) by Path
```

---

## 9. Deployment Checklist

- [ ] Add `CorrelationIdMiddleware` to shared infrastructure
- [ ] Add `HttpRequestLoggingMiddleware` with `SensitiveDataSanitizer`
- [ ] Add `ICorrelationIdAccessor` DI helper
- [ ] Add HTTP client correlation ID extensions
- [ ] Update all controllers to inject `ICorrelationIdAccessor`
- [ ] Update all service methods to accept and log `correlationId`
- [ ] Update all repository methods to accept `correlationId` for data layer logging
- [ ] Configure logging in `appsettings.json` for each service
- [ ] Update `Program.cs` registration for middleware
- [ ] Add sensitive data sanitization rules for your domain
- [ ] Test correlation ID propagation across service boundaries
- [ ] Verify no file paths or credentials in logs
- [ ] Configure log aggregation endpoint (Application Insights, ELK, etc.)
- [ ] Add performance alerting (requests > 5s)

---

## Success Criteria

- ✅ Every request has a correlation ID in all logs
- ✅ Correlation IDs visible in error responses
- ✅ No file paths, credentials, or API keys in any logs
- ✅ Request/response times logged for performance monitoring
- ✅ Logs structured with named fields for aggregation
- ✅ All services propagate correlation IDs in HTTP calls
- ✅ Integration tests validate log output format

