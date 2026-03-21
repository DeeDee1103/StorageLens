extern alias ScanJobsService;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ScanJobsProgram = ScanJobsService::Program;
using ScanJobsDbContext = ScanJobsService::StorageLens.Services.ScanJobs.Data.ScanJobsDbContext;

namespace StorageLens.Integration.Tests.ScanJobs;

/// <summary>
/// WebApplicationFactory that wires the ScanJobs service for integration testing.
///
/// Replaces SQL Server with an in-memory EF Core database (unique per factory
/// instance) and injects a shared API key so requests from test clients are
/// accepted by the ApiKeyAuthenticationHandler.
///
/// Usage:
///   public class MyTests(ScanJobsWebFactory factory) : IClassFixture&lt;ScanJobsWebFactory&gt;
///   {
///       [Fact]
///       public async Task SomeTest()
///       {
///           var client = factory.CreateAuthenticatedClient();
///           var resp   = await client.GetAsync("/api/scanjobs");
///           resp.EnsureSuccessStatusCode();
///       }
///   }
/// </summary>
public sealed class ScanJobsWebFactory
    : WebApplicationFactory<ScanJobsProgram>
{
    internal const string TestApiKey = "integration-test-key";

    // Each factory instance gets its own isolated in-memory database so that
    // test classes using IClassFixture do not share state.
    private readonly string _dbName = $"integration-scanjobs-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Tell the service to use in-memory mode (skips SQL connection strings).
        builder.UseSetting("Database:UseInMemory", "true");
        builder.UseSetting("ServiceApiKey", TestApiKey);

        builder.ConfigureServices(services =>
        {
            // Replace any registered ScanJobsDbContext with an isolated in-memory instance.
            services.RemoveAll<DbContextOptions<ScanJobsDbContext>>();
            services.AddDbContext<ScanJobsDbContext>(opts =>
                opts.UseInMemoryDatabase(_dbName));
        });
    }

    /// <summary>
    /// Creates an HttpClient pre-configured with the integration-test API key.
    /// </summary>
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", TestApiKey);
        return client;
    }
}
