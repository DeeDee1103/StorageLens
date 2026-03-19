using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StorageLens.Shared.Infrastructure;
using Xunit;

namespace StorageLens.Shared.Infrastructure.Tests;

public class ApiKeyAuthenticationHandlerTests
{
    [Fact]
    public async Task WhenNoApiKeyConfigured_AllowsRequestThrough()
    {
        using var client = CreateTestClient(configuredKey: null);

        var response = await client.GetAsync("/protected");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WhenApiKeyConfigured_ValidKeyInHeader_ReturnsOk()
    {
        using var client = CreateTestClient(configuredKey: "test-secret-key");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", "test-secret-key");

        var response = await client.GetAsync("/protected");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WhenApiKeyConfigured_WrongKeyInHeader_Returns401()
    {
        using var client = CreateTestClient(configuredKey: "correct-key");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", "wrong-key");

        var response = await client.GetAsync("/protected");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WhenApiKeyConfigured_NoHeader_Returns401()
    {
        using var client = CreateTestClient(configuredKey: "test-secret-key");

        var response = await client.GetAsync("/protected");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WhenApiKeyConfigured_EmptyHeader_Returns401()
    {
        using var client = CreateTestClient(configuredKey: "test-secret-key");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", "");

        var response = await client.GetAsync("/protected");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousEndpoint_AlwaysAccessible_EvenWithApiKeyConfigured()
    {
        using var client = CreateTestClient(configuredKey: "test-secret-key");

        var response = await client.GetAsync("/public");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task KeyComparison_IsCaseSensitive()
    {
        using var client = CreateTestClient(configuredKey: "MySecretKey");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", "mysecretkey");

        var response = await client.GetAsync("/protected");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static HttpClient CreateTestClient(string? configuredKey)
    {
        var configValues = new Dictionary<string, string?>();
        if (configuredKey is not null)
        {
            configValues[ApiKeyAuthentication.ConfigurationKey] = configuredKey;
        }

        var host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureAppConfiguration(config =>
                    config.AddInMemoryCollection(configValues));
                webBuilder.ConfigureServices(services =>
                {
                    services.AddAuthentication(ApiKeyAuthentication.SchemeName)
                        .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                            ApiKeyAuthentication.SchemeName, _ => { });
                    services.AddAuthorization();
                    services.AddRouting();
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/protected", () => "OK").RequireAuthorization();
                        endpoints.MapGet("/public", () => "OK").AllowAnonymous();
                    });
                });
            })
            .Build();

        host.Start();
        return host.GetTestClient();
    }
}
