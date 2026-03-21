using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StorageLens.Services.Scanner.Clients;
using StorageLens.Services.Scanner.Data;
using StorageLens.Services.Scanner.Interfaces;
using StorageLens.Services.Scanner.Services;
using StorageLens.Shared.Infrastructure;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.AddStorageLensObservability("StorageLens.Scanner");

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

builder.Services.AddDbContext<ScannerDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("StorageLens_Scanner");
        return;
    }

    options.UseSqlServer(builder.Configuration.GetConnectionString("ScannerDb"));
});

builder.Services.Configure<ScannerUpstreamOptions>(
    builder.Configuration.GetSection(ScannerUpstreamOptions.SectionName));
builder.Services.Configure<ScannerPathMappingOptions>(
    builder.Configuration.GetSection(ScannerPathMappingOptions.SectionName));
builder.Services.Configure<UpstreamResilienceOptions>(
    builder.Configuration.GetSection(UpstreamResilienceOptions.SectionName));

var upstream = builder.Configuration.GetSection(ScannerUpstreamOptions.SectionName).Get<ScannerUpstreamOptions>()
              ?? new ScannerUpstreamOptions();
var upstreamResilience = builder.Configuration.GetSection(UpstreamResilienceOptions.SectionName).Get<UpstreamResilienceOptions>()
                       ?? new UpstreamResilienceOptions();

var serviceApiKey = builder.Configuration["ServiceApiKey"] ?? string.Empty;

builder.Services.AddConfiguredUpstreamClient("fileinventory", upstream.FileInventory, serviceApiKey, upstreamResilience);
builder.Services.AddConfiguredUpstreamClient("scanjobs", upstream.ScanJobs, serviceApiKey, upstreamResilience);

builder.Services.AddSingleton<IScannerPathResolver, ScannerPathResolver>();
builder.Services.AddScoped<IScannerExecutionService, ScannerExecutionService>();


var sqlConnStr = useInMemory ? null : builder.Configuration.GetConnectionString("ScannerDb");
builder.AddStorageLensHealthChecks(
    sqlConnectionString: sqlConnStr,
    upstreamHealthUris: [
        new Uri("http://scanjobs:8080/health/live"),
        new Uri("http://fileinventory:8080/health/live")
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

BackgroundDatabaseInitialization.Start<ScannerDbContext>(
    app,
    "StorageLens.Services.Scanner");

app.Run();
