using Microsoft.EntityFrameworkCore;
using StorageLens.Services.Duplicates.Clients;
using StorageLens.Services.Duplicates.Data;
using StorageLens.Services.Duplicates.Extensions;
using StorageLens.Services.Duplicates.Interfaces;
using StorageLens.Services.Duplicates.Services;
using StorageLens.Shared.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

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

var upstream = builder.Configuration.GetSection(DuplicateUpstreamOptions.SectionName).Get<DuplicateUpstreamOptions>()
              ?? new DuplicateUpstreamOptions();

builder.Services.AddHttpClient("fileinventory", client =>
{
    client.BaseAddress = new Uri(upstream.FileInventory);
});

builder.Services.AddScoped<IDuplicatesService, DuplicatesService>();

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
    var context = scope.ServiceProvider.GetRequiredService<DuplicatesDbContext>();
    await StartupDatabaseInitializer.EnsureCreatedWithRetryAsync(context, logger);
    await DuplicatesSeeder.SeedAsync(context);
}

app.Run();
