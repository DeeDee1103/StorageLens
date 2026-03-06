using Microsoft.EntityFrameworkCore;
using StorageLens.Services.Duplicates.Data;
using StorageLens.Services.Duplicates.Extensions;
using StorageLens.Services.Duplicates.Interfaces;
using StorageLens.Services.Duplicates.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

builder.Services.AddScoped<IDuplicatesService, DuplicatesService>();

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
    var context = scope.ServiceProvider.GetRequiredService<DuplicatesDbContext>();
    await context.Database.EnsureCreatedAsync();
    await DuplicatesSeeder.SeedAsync(context);
}

app.Run();
