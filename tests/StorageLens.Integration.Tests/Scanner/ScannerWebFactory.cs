extern alias ScannerService;

using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ScannerProgram = ScannerService::Program;
using ScannerDbContext = ScannerService::StorageLens.Services.Scanner.Data.ScannerDbContext;

namespace StorageLens.Integration.Tests.Scanner;

/// <summary>
/// A simple delegating handler that always returns a preconfigured response.
/// Used to replace live HTTP clients for upstream services (fileinventory, scanjobs)
/// during integration tests so that no real network calls are made.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private Func<HttpRequestMessage, HttpResponseMessage> _factory;

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage>? factory = null)
    {
        _factory = factory ?? (_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        });
    }

    public void SetFactory(Func<HttpRequestMessage, HttpResponseMessage> factory) =>
        _factory = factory;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        Task.FromResult(_factory(request));
}

/// <summary>
/// Provides a reusable stub <see cref="IHttpClientFactory"/> for the Scanner service.
/// Named clients ("fileinventory", "scanjobs") return <see cref="StubHttpMessageHandler"/>
/// responses by default so tests remain fully in-process.
/// </summary>
public sealed class ScannerStubClientFactory : IHttpClientFactory
{
    public StubHttpMessageHandler FileInventoryHandler { get; } = new();
    public StubHttpMessageHandler ScanJobsHandler { get; } = new();

    public HttpClient CreateClient(string name) => name switch
    {
        "fileinventory" => new HttpClient(FileInventoryHandler) { BaseAddress = new Uri("http://stub-fileinventory/") },
        "scanjobs" => new HttpClient(ScanJobsHandler) { BaseAddress = new Uri("http://stub-scanjobs/") },
        _ => new HttpClient(new StubHttpMessageHandler()) { BaseAddress = new Uri("http://stub-default/") }
    };
}

/// <summary>
/// WebApplicationFactory that wires the Scanner service for integration testing.
///
/// Replaces:
///   - SQL Server with a per-instance in-memory EF Core database
///   - Live HTTP clients (fileinventory, scanjobs) with in-process stub handlers
///
/// Usage:
///   public class MyTests(ScannerWebFactory factory) : IClassFixture&lt;ScannerWebFactory&gt;
///   {
///       private readonly HttpClient _client = factory.CreateAuthenticatedClient();
///   }
/// </summary>
public sealed class ScannerWebFactory
    : WebApplicationFactory<ScannerProgram>
{
    internal const string TestApiKey = "integration-test-key";

    private readonly string _dbName = $"integration-scanner-{Guid.NewGuid()}";

    /// <summary>
    /// Provides direct access to stub handlers so individual tests can configure
    /// per-test response payloads if needed.
    /// </summary>
    public ScannerStubClientFactory StubClients { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.UseSetting("Database:UseInMemory", "true");
        builder.UseSetting("ServiceApiKey", TestApiKey);

        // Disable path auto-mapping so tests can control root paths directly.
        builder.UseSetting("ScannerPathMappings:EnableAutoMapping", "false");
        builder.UseSetting("ScannerPathMappings:AutoMapInContainerOnly", "false");

        builder.ConfigureServices(services =>
        {
            // Replace ScannerDbContext with isolated in-memory instance.
            services.RemoveAll<DbContextOptions<ScannerDbContext>>();
            services.AddDbContext<ScannerDbContext>(opts =>
                opts.UseInMemoryDatabase(_dbName));

            // Replace named HTTP clients used by ScannerExecutionService.
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
