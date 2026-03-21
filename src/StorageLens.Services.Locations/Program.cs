using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StorageLens.Services.Locations.Data;
using StorageLens.Services.Locations.Extensions;
using StorageLens.Services.Locations.Interfaces;
using StorageLens.Services.Locations.Services;
using StorageLens.Shared.Infrastructure;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.AddStorageLensObservability("StorageLens.Locations");

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

builder.Services.AddDbContext<LocationsDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("StorageLens_Locations");
        return;
    }

    options.UseSqlServer(builder.Configuration.GetConnectionString("LocationsDb"));
});

builder.Services.AddScoped<ILocationService, LocationService>();

var sqlConn = useInMemory ? null : builder.Configuration.GetConnectionString("LocationsDb");
builder.AddStorageLensHealthChecks(sqlConnectionString: sqlConn);

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

BackgroundDatabaseInitialization.Start<LocationsDbContext>(
    app,
    "StorageLens.Services.Locations",
    static (_, context, cancellationToken) => LocationsSeeder.SeedAsync(context, cancellationToken));

app.Run();
