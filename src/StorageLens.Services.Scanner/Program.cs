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

var upstream = builder.Configuration.GetSection(ScannerUpstreamOptions.SectionName).Get<ScannerUpstreamOptions>()
              ?? new ScannerUpstreamOptions();

var serviceApiKey = builder.Configuration["ServiceApiKey"] ?? string.Empty;

builder.Services.AddHttpClient("fileinventory", client =>
{
    client.BaseAddress = new Uri(upstream.FileInventory);
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

builder.Services.AddSingleton<IScannerPathResolver, ScannerPathResolver>();
builder.Services.AddScoped<IScannerExecutionService, ScannerExecutionService>();

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
    var context = scope.ServiceProvider.GetRequiredService<ScannerDbContext>();
    await StartupDatabaseInitializer.EnsureCreatedWithRetryAsync(context, logger);
}

app.Run();
