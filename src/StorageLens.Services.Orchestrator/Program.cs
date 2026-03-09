using Microsoft.EntityFrameworkCore;
using StorageLens.Services.Orchestrator.Clients;
using StorageLens.Services.Orchestrator.Data;
using StorageLens.Services.Orchestrator.Interfaces;
using StorageLens.Services.Orchestrator.Services;
using StorageLens.Shared.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var useInMemory = builder.Configuration.GetValue<bool>("Database:UseInMemory");

builder.Services.AddDbContext<OrchestratorDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("StorageLens_Orchestrator");
        return;
    }

    options.UseSqlServer(builder.Configuration.GetConnectionString("OrchestratorDb"));
});

builder.Services.Configure<OrchestratorUpstreamOptions>(
    builder.Configuration.GetSection(OrchestratorUpstreamOptions.SectionName));

builder.Services.Configure<WorkflowProcessingOptions>(
    builder.Configuration.GetSection(WorkflowProcessingOptions.SectionName));

var upstream = builder.Configuration.GetSection(OrchestratorUpstreamOptions.SectionName).Get<OrchestratorUpstreamOptions>()
              ?? new OrchestratorUpstreamOptions();

builder.Services.AddHttpClient("locations", client => client.BaseAddress = new Uri(upstream.Locations));
builder.Services.AddHttpClient("scanjobs", client => client.BaseAddress = new Uri(upstream.ScanJobs));
builder.Services.AddHttpClient("scanner", client =>
{
    client.BaseAddress = new Uri(upstream.Scanner);
    client.Timeout = Timeout.InfiniteTimeSpan;
});
builder.Services.AddHttpClient("hashing", client =>
{
    client.BaseAddress = new Uri(upstream.Hashing);
    client.Timeout = Timeout.InfiniteTimeSpan;
});
builder.Services.AddHttpClient("duplicates", client => client.BaseAddress = new Uri(upstream.Duplicates));
builder.Services.AddHttpClient("analytics", client => client.BaseAddress = new Uri(upstream.Analytics));

builder.Services.AddSingleton<WorkflowQueue>();
builder.Services.AddScoped<IWorkflowOrchestratorService, WorkflowOrchestratorService>();
builder.Services.AddHostedService<WorkflowBackgroundService>();

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
    var context = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
    await StartupDatabaseInitializer.EnsureCreatedWithRetryAsync(context, logger);
}

app.Run();
