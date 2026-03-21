using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using StorageLens.Shared.Infrastructure;
using StorageLens.Web.Clients;
using StorageLens.Web.Data;
using StorageLens.Web.Services;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddPageRoute("/Landing", "");
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

var dbCfg = builder.Configuration.GetSection(DatabaseConfigOptions.SectionName).Get<DatabaseConfigOptions>()
            ?? new DatabaseConfigOptions();

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseRateLimiter();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup.ClientsDatabase");
    var clientsDb = scope.ServiceProvider.GetRequiredService<ClientsDbContext>();
    await StartupDatabaseInitializer.EnsureCreatedWithRetryAsync(clientsDb, logger);
}

app.Run();
