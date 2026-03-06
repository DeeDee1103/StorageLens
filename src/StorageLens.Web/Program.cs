using StorageLens.Web.Clients;
using StorageLens.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.Configure<ServiceEndpointsOptions>(
    builder.Configuration.GetSection(ServiceEndpointsOptions.SectionName));

var endpoints = builder.Configuration.GetSection(ServiceEndpointsOptions.SectionName).Get<ServiceEndpointsOptions>()
               ?? new ServiceEndpointsOptions();

builder.Services.AddHttpClient<LocationsApiClient>(client => client.BaseAddress = new Uri(endpoints.Locations));
builder.Services.AddHttpClient<ScanJobsApiClient>(client => client.BaseAddress = new Uri(endpoints.ScanJobs));
builder.Services.AddHttpClient<FileInventoryApiClient>(client => client.BaseAddress = new Uri(endpoints.FileInventory));
builder.Services.AddHttpClient<DuplicatesApiClient>(client => client.BaseAddress = new Uri(endpoints.Duplicates));
builder.Services.AddHttpClient<AnalyticsApiClient>(client => client.BaseAddress = new Uri(endpoints.Analytics));

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

app.Run();
