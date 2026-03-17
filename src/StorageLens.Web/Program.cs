using Microsoft.EntityFrameworkCore;
using StorageLens.Shared.Infrastructure;
using StorageLens.Web.Clients;
using StorageLens.Web.Data;
using StorageLens.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddPageRoute("/Landing", "");
});

builder.Services.Configure<ServiceEndpointsOptions>(
    builder.Configuration.GetSection(ServiceEndpointsOptions.SectionName));

builder.Services.Configure<DatabaseConfigOptions>(
    builder.Configuration.GetSection(DatabaseConfigOptions.SectionName));

builder.Services.Configure<AppearanceOptions>(
    builder.Configuration.GetSection(AppearanceOptions.SectionName));

builder.Services.Configure<NotificationOptions>(
    builder.Configuration.GetSection(NotificationOptions.SectionName));

builder.Services.Configure<ScannerPerformanceOptions>(
    builder.Configuration.GetSection(ScannerPerformanceOptions.SectionName));

builder.Services.Configure<PricingOptions>(
    builder.Configuration.GetSection(PricingOptions.SectionName));

builder.Services.AddScoped<ITierAccessService, TierAccessService>();

var dbCfg = builder.Configuration.GetSection(DatabaseConfigOptions.SectionName).Get<DatabaseConfigOptions>()
            ?? new DatabaseConfigOptions();

builder.Services.AddDbContext<ClientsDbContext>(options =>
{
    if (dbCfg.UseInMemory)
    {
        options.UseInMemoryDatabase("StorageLens_Clients");
    }
    else
    {
        var password = builder.Configuration["ClientsDbPassword"];
        var connectionString = ClientsDatabaseConnectionBuilder.Build(dbCfg, password);
        options.UseSqlServer(connectionString);
    }
});

var endpoints = builder.Configuration.GetSection(ServiceEndpointsOptions.SectionName).Get<ServiceEndpointsOptions>()
               ?? new ServiceEndpointsOptions();

builder.Services.AddHttpClient<LocationsApiClient>(client => client.BaseAddress = new Uri(endpoints.Locations));
builder.Services.AddHttpClient<ScanJobsApiClient>(client => client.BaseAddress = new Uri(endpoints.ScanJobs));
builder.Services.AddHttpClient<FileInventoryApiClient>(client => client.BaseAddress = new Uri(endpoints.FileInventory));
builder.Services.AddHttpClient<DuplicatesApiClient>(client => client.BaseAddress = new Uri(endpoints.Duplicates));
builder.Services.AddHttpClient<AnalyticsApiClient>(client => client.BaseAddress = new Uri(endpoints.Analytics));
builder.Services.AddHttpClient<OrchestratorApiClient>(client => client.BaseAddress = new Uri(endpoints.Orchestrator));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup.ClientsDatabase");
    var clientsDb = scope.ServiceProvider.GetRequiredService<ClientsDbContext>();
    await StartupDatabaseInitializer.EnsureCreatedWithRetryAsync(clientsDb, logger);
}

app.Run();
