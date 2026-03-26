extern alias LocationsService;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using LocationsProgram = LocationsService::Program;
using LocationsDbContext = LocationsService::StorageLens.Services.Locations.Data.LocationsDbContext;

namespace StorageLens.Integration.Tests.Infrastructure;

/// <summary>
/// Base WebApplicationFactory that wires the Locations service for integration testing.
///
/// Replaces SQL Server with an in-memory EF Core database so no external
/// infrastructure is needed.  A shared API key is injected so the service
/// allows requests from test clients.
///
/// Usage:
///   public class MyTests(LocationsWebFactory factory) : IClassFixture&lt;LocationsWebFactory&gt;
///   {
///       [Fact]
///       public async Task SomeTest()
///       {
///           var client = factory.CreateAuthenticatedClient();
///           var resp   = await client.GetAsync("/api/locations");
///           resp.EnsureSuccessStatusCode();
///       }
///   }
/// </summary>
public sealed class LocationsWebFactory : WebApplicationFactory<LocationsProgram>
{
    internal const string TestApiKey = "integration-test-key";
    private readonly string _dbName = $"integration-locations-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.UseSetting("Database:UseInMemory", "true");
        builder.UseSetting("ServiceApiKey", TestApiKey);

        builder.ConfigureServices(services =>
        {
            // Swap out any registered LocationsDbContext for an in-memory one
            services.RemoveAll<DbContextOptions<LocationsDbContext>>();
            services.AddDbContext<LocationsDbContext>(opts =>
                opts.UseInMemoryDatabase(_dbName));
        });
    }

    /// <summary>
    /// Creates an HttpClient pre-configured with the integration test API key.
    /// </summary>
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", TestApiKey);
        return client;
    }
}
