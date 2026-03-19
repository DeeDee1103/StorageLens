using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StorageLens.Services.Orchestrator.Clients;
using StorageLens.Services.Orchestrator.Data;
using StorageLens.Services.Orchestrator.Interfaces;
using StorageLens.Services.Orchestrator.Services;
using StorageLens.Shared.Infrastructure;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
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

var upstream = builder.Configuration.GetSection(OrchestratorUpstreamOptions.SectionName).Get<OrchestratorUpstreamOptions>()
              ?? new OrchestratorUpstreamOptions();

var serviceApiKey = builder.Configuration["ServiceApiKey"] ?? string.Empty;

builder.Services.AddHttpClient("locations", client =>
{
    client.BaseAddress = new Uri(upstream.Locations);
    if (!string.IsNullOrWhiteSpace(serviceApiKey))
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", serviceApiKey);
});
builder.Services.AddHttpClient("scanjobs", client =>
{
    client.BaseAddress = new Uri(upstream.ScanJobs);
    if (!string.IsNullOrWhiteSpace(serviceApiKey))
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", serviceApiKey);
});
builder.Services.AddHttpClient("scanner", client =>
{
    client.BaseAddress = new Uri(upstream.Scanner);
    // Long-running scans can take hours; use a generous but finite timeout.
    client.Timeout = TimeSpan.FromHours(4);
    if (!string.IsNullOrWhiteSpace(serviceApiKey))
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", serviceApiKey);
});
builder.Services.AddHttpClient("hashing", client =>
{
    client.BaseAddress = new Uri(upstream.Hashing);
    // Long-running hash operations can take hours; use a generous but finite timeout.
    client.Timeout = TimeSpan.FromHours(4);
    if (!string.IsNullOrWhiteSpace(serviceApiKey))
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", serviceApiKey);
});
builder.Services.AddHttpClient("duplicates", client =>
{
    client.BaseAddress = new Uri(upstream.Duplicates);
    if (!string.IsNullOrWhiteSpace(serviceApiKey))
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", serviceApiKey);
});
builder.Services.AddHttpClient("analytics", client =>
{
    client.BaseAddress = new Uri(upstream.Analytics);
    if (!string.IsNullOrWhiteSpace(serviceApiKey))
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", serviceApiKey);
});

builder.Services.AddSingleton<WorkflowQueue>();
builder.Services.AddScoped<IWorkflowOrchestratorService, WorkflowOrchestratorService>();
builder.Services.AddHostedService<WorkflowBackgroundService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup.Database");
    var context = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
    await StartupDatabaseInitializer.EnsureCreatedWithRetryAsync(context, logger);
}

app.Run();
