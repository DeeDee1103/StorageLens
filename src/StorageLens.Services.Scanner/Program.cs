using Microsoft.EntityFrameworkCore;
using StorageLens.Services.Scanner.Clients;
using StorageLens.Services.Scanner.Data;
using StorageLens.Services.Scanner.Interfaces;
using StorageLens.Services.Scanner.Services;
using StorageLens.Shared.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

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

builder.Services.AddHttpClient("fileinventory", client => client.BaseAddress = new Uri(upstream.FileInventory));
builder.Services.AddHttpClient("scanjobs", client => client.BaseAddress = new Uri(upstream.ScanJobs));

builder.Services.AddSingleton<IScannerPathResolver, ScannerPathResolver>();
builder.Services.AddScoped<IScannerExecutionService, ScannerExecutionService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup.Database");
    var context = scope.ServiceProvider.GetRequiredService<ScannerDbContext>();
    await StartupDatabaseInitializer.EnsureCreatedWithRetryAsync(context, logger);
}

app.Run();
