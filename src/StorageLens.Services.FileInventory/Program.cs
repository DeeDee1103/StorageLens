using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StorageLens.Services.FileInventory.Data;
using StorageLens.Services.FileInventory.Extensions;
using StorageLens.Services.FileInventory.Interfaces;
using StorageLens.Services.FileInventory.Services;
using StorageLens.Shared.Infrastructure;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

KeyVaultIntegration.AddKeyVaultConfiguration(builder);
builder.AddStorageLensObservability("StorageLens.FileInventory");

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

builder.Services.AddDbContext<FileInventoryDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("StorageLens_FileInventory");
        return;
    }

    options.UseSqlServer(builder.Configuration.GetConnectionString("FileInventoryDb"));
});

builder.Services.AddScoped<IFileInventoryService, FileInventoryService>();


var sqlConnStr = useInMemory ? null : builder.Configuration.GetConnectionString("FileInventoryDb");
builder.AddStorageLensHealthChecks(
    sqlConnectionString: sqlConnStr);

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

BackgroundDatabaseInitialization.Start<FileInventoryDbContext>(
    app,
    "StorageLens.Services.FileInventory",
    static (_, context, _) => FileInventorySeeder.SeedAsync(context));

app.Run();
