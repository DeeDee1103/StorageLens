using Microsoft.EntityFrameworkCore;
using StorageLens.Services.Hashing.Clients;
using StorageLens.Services.Hashing.Data;
using StorageLens.Services.Hashing.Interfaces;
using StorageLens.Services.Hashing.Services;
using StorageLens.Shared.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var useInMemory = builder.Configuration.GetValue<bool>("Database:UseInMemory");

builder.Services.AddDbContext<HashingDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("StorageLens_Hashing");
        return;
    }

    options.UseSqlServer(builder.Configuration.GetConnectionString("HashingDb"));
});

builder.Services.Configure<HashingUpstreamOptions>(
    builder.Configuration.GetSection(HashingUpstreamOptions.SectionName));
builder.Services.Configure<HashingWorkerOptions>(
    builder.Configuration.GetSection(HashingWorkerOptions.SectionName));

var upstream = builder.Configuration.GetSection(HashingUpstreamOptions.SectionName).Get<HashingUpstreamOptions>()
              ?? new HashingUpstreamOptions();

builder.Services.AddHttpClient("fileinventory", client => client.BaseAddress = new Uri(upstream.FileInventory));
builder.Services.AddHttpClient("scanjobs", client => client.BaseAddress = new Uri(upstream.ScanJobs));

builder.Services.AddScoped<IHashingService, HashingService>();

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
    var context = scope.ServiceProvider.GetRequiredService<HashingDbContext>();
    await StartupDatabaseInitializer.EnsureCreatedWithRetryAsync(context, logger);
}

app.Run();
