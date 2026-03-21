using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StorageLens.Services.ScanJobs.Data;
using StorageLens.Services.ScanJobs.Extensions;
using StorageLens.Services.ScanJobs.Interfaces;
using StorageLens.Services.ScanJobs.Services;
using StorageLens.Shared.Infrastructure;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.AddStorageLensObservability("StorageLens.ScanJobs");

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

builder.Services.AddDbContext<ScanJobsDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("StorageLens_ScanJobs");
        return;
    }

    options.UseSqlServer(builder.Configuration.GetConnectionString("ScanJobsDb"));
});

builder.Services.AddScoped<IScanJobsService, ScanJobsService>();


var sqlConnStr = useInMemory ? null : builder.Configuration.GetConnectionString("ScanJobsDb");
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

BackgroundDatabaseInitialization.Start<ScanJobsDbContext>(
    app,
    "StorageLens.Services.ScanJobs",
    static (_, context, cancellationToken) => ScanJobsSeeder.SeedAsync(context, cancellationToken));

app.Run();
