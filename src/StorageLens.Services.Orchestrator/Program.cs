using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StorageLens.Services.Orchestrator.Clients;
using StorageLens.Services.Orchestrator.Data;
using StorageLens.Services.Orchestrator.Interfaces;
using StorageLens.Services.Orchestrator.Services;
using StorageLens.Shared.Infrastructure;
using System.Threading.RateLimiting;
using Polly;
using Polly.Extensions.Http;
using SharedUpstreamResilienceOptions = StorageLens.Shared.Infrastructure.UpstreamResilienceOptions;
using SharedUpstreamResilienceProfileResolver = StorageLens.Shared.Infrastructure.UpstreamResilienceProfileResolver;

var builder = WebApplication.CreateBuilder(args);

builder.AddStorageLensObservability("StorageLens.Orchestrator");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("global", limiterOptions =>
    {
        limiterOptions.PermitLimit = 1000;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});
builder.Services.AddAuthentication(ApiKeyAuthentication.SchemeName)
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthentication.SchemeName, _ => { });
builder.Services.AddAuthorization();

var useInMemory = builder.Configuration.GetValue<bool>("Database:UseInMemory");

builder.Services.AddDbContext<OrchestratorDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("StorageLens_Orchestrator");
        return;
    }

    options.UseSqlServer(builder.Configuration.GetConnectionString("OrchestratorDb"));
});

builder.Services.Configure<OrchestratorUpstreamOptions>(
    builder.Configuration.GetSection(OrchestratorUpstreamOptions.SectionName));

builder.Services.Configure<WorkflowProcessingOptions>(
    builder.Configuration.GetSection(WorkflowProcessingOptions.SectionName));

builder.Services.Configure<SharedUpstreamResilienceOptions>(
    builder.Configuration.GetSection(SharedUpstreamResilienceOptions.SectionName));

var upstream = builder.Configuration.GetSection(OrchestratorUpstreamOptions.SectionName).Get<OrchestratorUpstreamOptions>()
              ?? new OrchestratorUpstreamOptions();

var upstreamResilience = builder.Configuration.GetSection(SharedUpstreamResilienceOptions.SectionName).Get<SharedUpstreamResilienceOptions>()
                        ?? new SharedUpstreamResilienceOptions();

var serviceApiKey = builder.Configuration["ServiceApiKey"] ?? string.Empty;

IAsyncPolicy<HttpResponseMessage> noOpPolicy = Policy.NoOpAsync<HttpResponseMessage>();

static bool IsSafeRetryMethod(HttpMethod method) =>
    method == HttpMethod.Get ||
    method == HttpMethod.Head ||
    method == HttpMethod.Options;

void AddUpstreamClient(string name, string baseAddress)
{
    var profile = SharedUpstreamResilienceProfileResolver.ResolveForClient(name, upstreamResilience);

    IAsyncPolicy<HttpResponseMessage> retryPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            profile.MaxRetryAttempts,
            retryAttempt =>
            {
                var exponential = profile.BaseDelaySeconds * Math.Pow(2, retryAttempt - 1);
                var bounded = Math.Min(exponential, profile.MaxDelaySeconds);
                return TimeSpan.FromSeconds(bounded);
            });

    IAsyncPolicy<HttpResponseMessage> circuitBreakerPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            profile.CircuitBreakerFailures,
            TimeSpan.FromSeconds(profile.CircuitBreakerBreakSeconds));

    builder.Services
        .AddHttpClient(name, client =>
        {
            client.BaseAddress = new Uri(baseAddress);
            client.Timeout = profile.Timeout;
            if (!string.IsNullOrWhiteSpace(serviceApiKey))
                client.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", serviceApiKey);
        })
        .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
        .AddPolicyHandler(request => IsSafeRetryMethod(request.Method) ? retryPolicy : noOpPolicy)
        .AddPolicyHandler(circuitBreakerPolicy);
}

AddUpstreamClient("locations", upstream.Locations);
AddUpstreamClient("scanjobs", upstream.ScanJobs);
AddUpstreamClient("scanner", upstream.Scanner);
AddUpstreamClient("hashing", upstream.Hashing);
AddUpstreamClient("duplicates", upstream.Duplicates);
AddUpstreamClient("analytics", upstream.Analytics);

builder.Services.AddSingleton<WorkflowQueue>();
builder.Services.AddScoped<IWorkflowOrchestratorService, WorkflowOrchestratorService>();
builder.Services.AddHostedService<WorkflowBackgroundService>();


var orchestratorSqlConn = useInMemory ? null : builder.Configuration.GetConnectionString("OrchestratorDb");
builder.AddStorageLensHealthChecks(
    sqlConnectionString: orchestratorSqlConn,
    upstreamHealthUris: [
        new Uri($"{upstream.Locations.TrimEnd('/')}/health/live"),
        new Uri($"{upstream.ScanJobs.TrimEnd('/')}/health/live"),
        new Uri($"{upstream.Scanner.TrimEnd('/')}/health/live"),
        new Uri($"{upstream.Hashing.TrimEnd('/')}/health/live"),
        new Uri($"{upstream.Duplicates.TrimEnd('/')}/health/live"),
        new Uri($"{upstream.Analytics.TrimEnd('/')}/health/live"),
    ]);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseCorrelationId();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapStorageLensHealthChecks();

BackgroundDatabaseInitialization.Start<OrchestratorDbContext>(
    app,
    "StorageLens.Services.Orchestrator");

app.Run();
