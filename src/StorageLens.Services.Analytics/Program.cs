using StorageLens.Services.Analytics.Clients;
using StorageLens.Services.Analytics.Interfaces;
using StorageLens.Services.Analytics.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.Configure<AnalyticsUpstreamOptions>(
    builder.Configuration.GetSection(AnalyticsUpstreamOptions.SectionName));

var upstream = builder.Configuration.GetSection(AnalyticsUpstreamOptions.SectionName).Get<AnalyticsUpstreamOptions>()
              ?? new AnalyticsUpstreamOptions();

builder.Services.AddHttpClient("locations", client => client.BaseAddress = new Uri(upstream.Locations));
builder.Services.AddHttpClient("scanjobs", client => client.BaseAddress = new Uri(upstream.ScanJobs));
builder.Services.AddHttpClient("fileinventory", client => client.BaseAddress = new Uri(upstream.FileInventory));
builder.Services.AddHttpClient("duplicates", client => client.BaseAddress = new Uri(upstream.Duplicates));

builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
