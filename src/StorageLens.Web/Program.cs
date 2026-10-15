using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StorageLens.Shared.Infrastructure;
using StorageLens.Web.Clients;
using StorageLens.Web.Data;
using StorageLens.Web.Services;
using System.Threading.RateLimiting;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

KeyVaultIntegration.AddKeyVaultConfiguration(builder);
builder.AddStorageLensObservability("StorageLens.Web");

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddPageRoute("/Index", "");
});

builder.Services.Configure<ServiceEndpointsOptions>(
    builder.Configuration.GetSection(ServiceEndpointsOptions.SectionName));

builder.Services.Configure<DatabaseConfigOptions>(
    builder.Configuration.GetSection(DatabaseConfigOptions.SectionName));

builder.Services.Configure<AppearanceOptions>(
    builder.Configuration.GetSection(AppearanceOptions.SectionName));

builder.Services.Configure<NotificationOptions>(
    builder.Configuration.GetSection(NotificationOptions.SectionName));

builder.Services.Configure<AlertThresholdOptions>(
    builder.Configuration.GetSection(AlertThresholdOptions.SectionName));

builder.Services.Configure<ScannerPerformanceOptions>(
    builder.Configuration.GetSection(ScannerPerformanceOptions.SectionName));

builder.Services.Configure<PricingOptions>(
    builder.Configuration.GetSection(PricingOptions.SectionName));
builder.Services.Configure<UpstreamResilienceOptions>(
    builder.Configuration.GetSection(UpstreamResilienceOptions.SectionName));

builder.Services.AddMemoryCache();
builder.Services.AddRateLimiter(options =>
{
    options.AddSlidingWindowLimiter("global", limiterOptions =>
    {
        limiterOptions.PermitLimit = 200;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.SegmentsPerWindow = 6;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});
builder.Services.AddScoped<IAlertEmailService, SmtpAlertEmailService>();

builder.Services.AddScoped<ITierAccessService, TierAccessService>();
builder.Services.AddSingleton<OwnerSession>();

var dbCfg = builder.Configuration.GetSection(DatabaseConfigOptions.SectionName).Get<DatabaseConfigOptions>()
            ?? new DatabaseConfigOptions();

if (!dbCfg.UseInMemory && string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase))
{
    dbCfg.UseInMemory = true;
}

builder.Services.AddDbContext<ClientsDbContext>(options =>
{
    if (dbCfg.UseInMemory)
    {
        options.UseInMemoryDatabase("StorageLens_Clients");
    }
    else
    {
        var password = builder.Configuration["ClientsDbPassword"];
        var connectionString = ClientsDatabaseConnectionBuilder.Build(dbCfg, password);
        options.UseSqlServer(connectionString);
    }
});

var endpoints = builder.Configuration.GetSection(ServiceEndpointsOptions.SectionName).Get<ServiceEndpointsOptions>()
               ?? new ServiceEndpointsOptions();
var upstreamResilience = builder.Configuration.GetSection(UpstreamResilienceOptions.SectionName).Get<UpstreamResilienceOptions>()
                       ?? new UpstreamResilienceOptions();

var serviceApiKey = builder.Configuration["ServiceApiKey"] ?? string.Empty;

builder.Services.AddConfiguredUpstreamClient<LocationsApiClient>("locations", endpoints.Locations, serviceApiKey, upstreamResilience);
builder.Services.AddConfiguredUpstreamClient<ScanJobsApiClient>("scanjobs", endpoints.ScanJobs, serviceApiKey, upstreamResilience);
builder.Services.AddConfiguredUpstreamClient<FileInventoryApiClient>("fileinventory", endpoints.FileInventory, serviceApiKey, upstreamResilience);
builder.Services.AddConfiguredUpstreamClient<DuplicatesApiClient>("duplicates", endpoints.Duplicates, serviceApiKey, upstreamResilience);
builder.Services.AddConfiguredUpstreamClient<AnalyticsApiClient>("analytics", endpoints.Analytics, serviceApiKey, upstreamResilience);
builder.Services.AddConfiguredUpstreamClient<OrchestratorApiClient>("orchestrator", endpoints.Orchestrator, serviceApiKey, upstreamResilience);

builder.AddStorageLensHealthChecks(
    upstreamHealthUris:
    [
        new Uri($"{endpoints.Locations.TrimEnd('/')}/health/live"),
        new Uri($"{endpoints.ScanJobs.TrimEnd('/')}/health/live"),
        new Uri($"{endpoints.FileInventory.TrimEnd('/')}/health/live"),
        new Uri($"{endpoints.Duplicates.TrimEnd('/')}/health/live"),
        new Uri($"{endpoints.Analytics.TrimEnd('/')}/health/live"),
        new Uri($"{endpoints.Orchestrator.TrimEnd('/')}/health/live")
    ]);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();   // GlobalExceptionHandler registered via AddStorageLensObservability
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseCorrelationId();
app.UseRateLimiter();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
MapWebHealthEndpoints(app);

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup.ClientsDatabase");
    var clientsDb = scope.ServiceProvider.GetRequiredService<ClientsDbContext>();
    await StartupDatabaseInitializer.EnsureCreatedWithRetryAsync(clientsDb, logger);
}

await WarmUpUpstreamDependenciesAsync(app.Services, endpoints);

app.Run();

static async Task WarmUpUpstreamDependenciesAsync(IServiceProvider services, ServiceEndpointsOptions endpoints)
{
    using var scope = services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup.UpstreamWarmup");

    var dependencies = new Dictionary<string, string>
    {
        ["locations"] = endpoints.Locations,
        ["scanjobs"] = endpoints.ScanJobs,
        ["fileinventory"] = endpoints.FileInventory,
        ["duplicates"] = endpoints.Duplicates,
        ["analytics"] = endpoints.Analytics,
        ["orchestrator"] = endpoints.Orchestrator,
    };

    var warmupTasks = dependencies
        .Where(x => !string.IsNullOrWhiteSpace(x.Value))
        .Select(x => WaitForReadyAsync(x.Key, x.Value, logger, TimeSpan.FromSeconds(45)));

    await Task.WhenAll(warmupTasks);
}

static async Task WaitForReadyAsync(
    string dependencyName,
    string baseAddress,
    ILogger logger,
    TimeSpan maxWait)
{
    var healthUri = new Uri(new Uri(baseAddress), "health/ready");
    var deadline = DateTimeOffset.UtcNow + maxWait;
    var attempt = 0;

    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };

    while (DateTimeOffset.UtcNow < deadline)
    {
        attempt++;
        try
        {
            using var response = await client.GetAsync(healthUri);
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Upstream {Dependency} is ready at {HealthUri} after {Attempt} attempt(s).", dependencyName, healthUri, attempt);
                return;
            }

            logger.LogWarning("Upstream {Dependency} readiness check returned status {StatusCode}.", dependencyName, (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Upstream {Dependency} readiness check failed.", dependencyName);
        }

        var backoff = TimeSpan.FromMilliseconds(Math.Min(5000, 500 * attempt));
        await Task.Delay(backoff);
    }

    logger.LogWarning("Upstream {Dependency} did not become ready within {MaxWaitSeconds} seconds.", dependencyName, maxWait.TotalSeconds);
}

static void MapWebHealthEndpoints(WebApplication app)
{
    app.MapGet("/health/live", () => Results.Json(new { status = "Healthy" }))
        .AllowAnonymous();

    app.MapGet("/health/ready", async (HealthCheckService healthChecks, CancellationToken cancellationToken) =>
    {
        var report = await healthChecks.CheckHealthAsync(
            check => check.Tags.Contains("ready"),
            cancellationToken);

        var payload = new
        {
            status = report.Status.ToString(),
            entries = report.Entries.ToDictionary(
                e => e.Key,
                e => new
                {
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = e.Value.Duration.TotalMilliseconds,
                    tags = e.Value.Tags
                })
        };

        return report.Status == HealthStatus.Unhealthy
            ? Results.Json(payload, statusCode: StatusCodes.Status503ServiceUnavailable)
            : Results.Json(payload);
    }).AllowAnonymous();

    app.MapGet("/health", () => Results.Json(new { status = "Healthy" }))
        .AllowAnonymous();
}
