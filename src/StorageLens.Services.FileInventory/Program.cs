using Microsoft.EntityFrameworkCore;
using StorageLens.Services.FileInventory.Data;
using StorageLens.Services.FileInventory.Extensions;
using StorageLens.Services.FileInventory.Interfaces;
using StorageLens.Services.FileInventory.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
    var context = scope.ServiceProvider.GetRequiredService<FileInventoryDbContext>();
    await context.Database.EnsureCreatedAsync();
    await FileInventorySeeder.SeedAsync(context);
}

app.Run();
