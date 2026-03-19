using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using StorageLens.Services.Analytics.Clients;
using StorageLens.Services.Analytics.Interfaces;
using StorageLens.Services.Analytics.Services;
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

builder.Services.Configure<AnalyticsUpstreamOptions>(
    builder.Configuration.GetSection(AnalyticsUpstreamOptions.SectionName));

var upstream = builder.Configuration.GetSection(AnalyticsUpstreamOptions.SectionName).Get<AnalyticsUpstreamOptions>()
              ?? new AnalyticsUpstreamOptions();

var serviceApiKey = builder.Configuration["ServiceApiKey"] ?? string.Empty;

builder.Services.AddHttpClient("locations", client =>
{
    client.BaseAddress = new Uri(upstream.Locations);
    client.Timeout = TimeSpan.FromSeconds(30); // SECURITY: Prevent hung connections
    if (!string.IsNullOrWhiteSpace(serviceApiKey))
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", serviceApiKey);
});
builder.Services.AddHttpClient("scanjobs", client =>
{
    client.BaseAddress = new Uri(upstream.ScanJobs);
    client.Timeout = TimeSpan.FromSeconds(30); // SECURITY: Prevent hung connections
    if (!string.IsNullOrWhiteSpace(serviceApiKey))
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", serviceApiKey);
});
builder.Services.AddHttpClient("fileinventory", client =>
{
    client.BaseAddress = new Uri(upstream.FileInventory);
    client.Timeout = TimeSpan.FromSeconds(30); // SECURITY: Prevent hung connections
    if (!string.IsNullOrWhiteSpace(serviceApiKey))
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", serviceApiKey);
});
builder.Services.AddHttpClient("duplicates", client =>
{
    client.BaseAddress = new Uri(upstream.Duplicates);
    client.Timeout = TimeSpan.FromSeconds(30); // SECURITY: Prevent hung connections
    if (!string.IsNullOrWhiteSpace(serviceApiKey))
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", serviceApiKey);
});

builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

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

app.Run();
