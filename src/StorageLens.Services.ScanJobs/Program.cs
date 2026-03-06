using Microsoft.EntityFrameworkCore;
using StorageLens.Services.ScanJobs.Data;
using StorageLens.Services.ScanJobs.Extensions;
using StorageLens.Services.ScanJobs.Interfaces;
using StorageLens.Services.ScanJobs.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ScanJobsDbContext>();
    await context.Database.EnsureCreatedAsync();
    await ScanJobsSeeder.SeedAsync(context);
}

app.Run();
