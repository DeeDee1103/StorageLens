extern alias OrchestratorService;

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StorageLens.Shared.Contracts;
using OrchestratorProgram = OrchestratorService::Program;
using OrchestratorDbContext = OrchestratorService::StorageLens.Services.Orchestrator.Data.OrchestratorDbContext;

namespace StorageLens.Integration.Tests.Orchestrator;

/// <summary>
/// A delegating handler that records how many times it was called and returns a
/// configurable response.  Tests can inspect <see cref="CallCount"/> to assert
/// that a specific upstream service was (or was not) invoked.
/// </summary>
public sealed class CountingStubHandler : HttpMessageHandler
{
    private Func<HttpRequestMessage, HttpResponseMessage> _factory;
    private int _callCount;

    public CountingStubHandler(Func<HttpRequestMessage, HttpResponseMessage>? factory = null)
    {
        _factory = factory ?? (_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        });
    }

    public int CallCount => _callCount;

    public void SetFactory(Func<HttpRequestMessage, HttpResponseMessage> factory) =>
        _factory = factory;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        System.Threading.Interlocked.Increment(ref _callCount);
        return Task.FromResult(_factory(request));
    }
}

/// <summary>
/// Provides stub named HTTP clients for all six upstream services used by the
/// Orchestrator.  Each upstream gets its own <see cref="CountingStubHandler"/>
/// so individual tests can verify call counts or override responses.
/// </summary>
public sealed class OrchestratorStubClientFactory : IHttpClientFactory
{
    public CountingStubHandler LocationsHandler { get; } = new(BuildLocationHandler());
    public CountingStubHandler ScanJobsHandler { get; } = new(BuildScanJobHandler());
    public CountingStubHandler ScannerHandler { get; } = new(BuildScannerExecutionHandler());
    public CountingStubHandler HashingHandler { get; } = new();
    public CountingStubHandler DuplicatesHandler { get; } = new(BuildDuplicatesHandler());
    public CountingStubHandler AnalyticsHandler { get; } = new();

    public HttpClient CreateClient(string name) => name switch
    {
        "locations" => new HttpClient(LocationsHandler) { BaseAddress = new Uri("http://stub-locations/") },
        "scanjobs" => new HttpClient(ScanJobsHandler) { BaseAddress = new Uri("http://stub-scanjobs/") },
        "scanner" => new HttpClient(ScannerHandler) { BaseAddress = new Uri("http://stub-scanner/") },
        "hashing" => new HttpClient(HashingHandler) { BaseAddress = new Uri("http://stub-hashing/") },
        "duplicates" => new HttpClient(DuplicatesHandler) { BaseAddress = new Uri("http://stub-duplicates/") },
        "analytics" => new HttpClient(AnalyticsHandler) { BaseAddress = new Uri("http://stub-analytics/") },
        _ => new HttpClient(new CountingStubHandler()) { BaseAddress = new Uri("http://stub-default/") }
    };

    // ── Default payload factories ─────────────────────────────────────────

    private static Func<HttpRequestMessage, HttpResponseMessage> BuildLocationHandler()
    {
        var locationJson = System.Text.Json.JsonSerializer.Serialize(new StorageLocationDto(
            Id: Guid.NewGuid(),
            Name: "Stub Location",
            Path: System.IO.Path.GetTempPath(),
            Type: "Local",
            Status: "Online",
            LastScanDate: null,
            TotalSizeBytes: 0,
            FileCount: 0,
            CreatedDate: DateTimeOffset.UtcNow,
            SiteLocation: "Test Site",
            Latitude: null,
            Longitude: null));

        return _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(locationJson,
                System.Text.Encoding.UTF8, "application/json")
        };
    }

    private static Func<HttpRequestMessage, HttpResponseMessage> BuildScanJobHandler()
    {
        // The scanjobs stub handles multiple shapes: POST create, PUT status/progress, GET by id.
        return request =>
        {
            if (request.Method == HttpMethod.Post)
            {
                var jobJson = System.Text.Json.JsonSerializer.Serialize(new ScanJobDto(
                    Id: Guid.NewGuid(),
                    StorageLocationId: Guid.NewGuid(),
                    LocationName: "Stub Location",
                    Status: "Queued",
                    StartTime: DateTimeOffset.UtcNow,
                    EndTime: null,
                    FilesScanned: 0,
                    ProgressPercent: 0,
                    Notes: "Stub",
                    CurrentStage: "Queued",
                    CorrelationId: Guid.NewGuid().ToString("N")));

                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(jobJson,
                        System.Text.Encoding.UTF8, "application/json")
                };
            }

            if (request.Method == HttpMethod.Get)
            {
                var jobJson = System.Text.Json.JsonSerializer.Serialize(new ScanJobDto(
                    Id: Guid.NewGuid(),
                    StorageLocationId: Guid.NewGuid(),
                    LocationName: "Stub Location",
                    Status: "Completed",
                    StartTime: DateTimeOffset.UtcNow,
                    EndTime: DateTimeOffset.UtcNow,
                    FilesScanned: 0,
                    ProgressPercent: 100,
                    Notes: "Stub",
                    CurrentStage: "Completed",
                    CorrelationId: Guid.NewGuid().ToString("N")));

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(jobJson,
                        System.Text.Encoding.UTF8, "application/json")
                };
            }

            // PUT status / progress → 200 OK with minimal body
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
            };
        };
    }

    private static Func<HttpRequestMessage, HttpResponseMessage> BuildScannerExecutionHandler()
    {
        var execJson = System.Text.Json.JsonSerializer.Serialize(new ScannerExecutionDto(
            Id: Guid.NewGuid(),
            StorageLocationId: Guid.NewGuid(),
            ScanJobId: Guid.NewGuid(),
            CorrelationId: Guid.NewGuid().ToString("N"),
            RootPath: System.IO.Path.GetTempPath(),
            Status: "Completed",
            StartedAtUtc: DateTimeOffset.UtcNow,
            CompletedAtUtc: DateTimeOffset.UtcNow,
            FilesDiscovered: 0,
            ErrorCount: 0,
            Notes: "Stub scan completed"));

        return _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(execJson,
                System.Text.Encoding.UTF8, "application/json")
        };
    }

    private static Func<HttpRequestMessage, HttpResponseMessage> BuildDuplicatesHandler()
    {
        var dupJson = System.Text.Json.JsonSerializer.Serialize(new DuplicateRecalculationResultDto(
            ScanJobId: Guid.NewGuid(),
            CorrelationId: Guid.NewGuid().ToString("N"),
            DuplicateGroups: 0,
            DuplicateFiles: 0,
            WastedBytes: 0,
            RecalculatedAtUtc: DateTimeOffset.UtcNow));

        return _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(dupJson,
                System.Text.Encoding.UTF8, "application/json")
        };
    }
}

/// <summary>
/// WebApplicationFactory for Orchestrator integration tests.
///
/// Replaces:
///   - SQL Server with a per-instance in-memory EF Core database
///   - All six upstream named HTTP clients with in-process stub handlers
///   - WorkflowProcessing:MaxConcurrentWorkflows is set to 1 for deterministic tests
///
/// Usage:
///   public class MyTests(OrchestratorWebFactory factory)
///       : IClassFixture&lt;OrchestratorWebFactory&gt;
///   {
///       private readonly HttpClient _client = factory.CreateAuthenticatedClient();
///   }
/// </summary>
public sealed class OrchestratorWebFactory
    : WebApplicationFactory<OrchestratorProgram>
{
    internal const string TestApiKey = "integration-test-key";

    private readonly string _dbName = $"integration-orchestrator-{Guid.NewGuid()}";

    /// <summary>
    /// Provides direct access to stub handlers so tests can verify call counts
    /// or override per-request response payloads.
    /// </summary>
    public OrchestratorStubClientFactory StubClients { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.UseSetting("Database:UseInMemory", "true");
        builder.UseSetting("ServiceApiKey", TestApiKey);

        // Limit concurrency to 1 so workflow processing is deterministic in tests.
        builder.UseSetting("WorkflowProcessing:MaxConcurrentWorkflows", "1");

        // Point all upstream addresses at the stub base URI so the real
        // configuration does not interfere. These are overridden at the
        // IHttpClientFactory level below, so actual URL values don't matter.
        builder.UseSetting("UpstreamServices:Locations", "http://stub-locations/");
        builder.UseSetting("UpstreamServices:ScanJobs", "http://stub-scanjobs/");
        builder.UseSetting("UpstreamServices:Scanner", "http://stub-scanner/");
        builder.UseSetting("UpstreamServices:Hashing", "http://stub-hashing/");
        builder.UseSetting("UpstreamServices:Duplicates", "http://stub-duplicates/");
        builder.UseSetting("UpstreamServices:Analytics", "http://stub-analytics/");

        builder.ConfigureServices(services =>
        {
            // Replace OrchestratorDbContext with an isolated in-memory instance.
            services.RemoveAll<DbContextOptions<OrchestratorDbContext>>();
            services.AddDbContext<OrchestratorDbContext>(opts =>
                opts.UseInMemoryDatabase(_dbName));

            // Replace all named HTTP clients with the stub factory.
            services.RemoveAll<IHttpClientFactory>();
            services.AddSingleton<IHttpClientFactory>(StubClients);
        });
    }

    /// <summary>Creates an HttpClient pre-configured with the integration-test API key.</summary>
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", TestApiKey);
        return client;
    }
}
