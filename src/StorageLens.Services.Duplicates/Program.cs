using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StorageLens.Services.Duplicates.Clients;
using StorageLens.Services.Duplicates.Data;
using StorageLens.Services.Duplicates.Extensions;
using StorageLens.Services.Duplicates.Interfaces;
using StorageLens.Services.Duplicates.Repositories;
using StorageLens.Services.Duplicates.Services;
using StorageLens.Shared.Infrastructure;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

KeyVaultIntegration.AddKeyVaultConfiguration(builder);
builder.AddStorageLensObservability("StorageLens.Duplicates");

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

builder.Services.AddDbContext<DuplicatesDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("StorageLens_Duplicates");
        return;
    }

    options.UseSqlServer(builder.Configuration.GetConnectionString("DuplicatesDb"));
});

builder.Services.Configure<DuplicateUpstreamOptions>(
    builder.Configuration.GetSection(DuplicateUpstreamOptions.SectionName));
builder.Services.Configure<UpstreamResilienceOptions>(
    builder.Configuration.GetSection(UpstreamResilienceOptions.SectionName));

var upstream = builder.Configuration.GetSection(DuplicateUpstreamOptions.SectionName).Get<DuplicateUpstreamOptions>()
              ?? new DuplicateUpstreamOptions();
var upstreamResilience = builder.Configuration.GetSection(UpstreamResilienceOptions.SectionName).Get<UpstreamResilienceOptions>()
                       ?? new UpstreamResilienceOptions();

var serviceApiKey = builder.Configuration["ServiceApiKey"] ?? string.Empty;

builder.Services.AddConfiguredUpstreamClient("fileinventory", upstream.FileInventory, serviceApiKey, upstreamResilience);

builder.Services.AddScoped<IDuplicateRepository, DuplicateRepository>();
builder.Services.AddScoped<IDuplicatesService, DuplicatesService>();


var sqlConnStr = useInMemory ? null : builder.Configuration.GetConnectionString("DuplicatesDb");
builder.AddStorageLensHealthChecks(
    sqlConnectionString: sqlConnStr,
    upstreamHealthUris: [
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

BackgroundDatabaseInitialization.Start<DuplicatesDbContext>(
    app,
    "StorageLens.Services.Duplicates",
    static (_, context, _) => DuplicatesSeeder.SeedAsync(context));

app.Run();
