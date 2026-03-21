using System.Net;
using Microsoft.Extensions.DependencyInjection;
using StorageLens.Shared.Infrastructure;
using Xunit;

namespace StorageLens.Shared.Infrastructure.Tests;

public class HttpClientPolicyPipelineIntegrationTests
{
    [Fact]
    public async Task HttpClient_WithCorrelationIdHandler_AddsCorrelationIdHeader()
    {
        HttpRequestMessage? capturedRequest = null;

        var services = new ServiceCollection();
        services.AddSingleton<CorrelationIdDelegatingHandler>();
        services.AddHttpClient("test")
            .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
            .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler(req =>
            {
                capturedRequest = req;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }));

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("test");

        await client.GetAsync("http://localhost/test");

        Assert.NotNull(capturedRequest);
        Assert.True(capturedRequest.Headers.Contains(CorrelationIdDelegatingHandler.HeaderName));
    }

    [Fact]
    public async Task HttpClient_WithCorrelationIdHandler_DoesNotOverwriteExistingHeader()
    {
        HttpRequestMessage? capturedRequest = null;
        const string existingCorrelationId = "existing-correlation-id";

        var services = new ServiceCollection();
        services.AddSingleton<CorrelationIdDelegatingHandler>();
        services.AddHttpClient("test")
            .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
            .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler(req =>
            {
                capturedRequest = req;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }));

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("test");

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test");
        request.Headers.TryAddWithoutValidation(CorrelationIdDelegatingHandler.HeaderName, existingCorrelationId);
        await client.SendAsync(request);

        Assert.NotNull(capturedRequest);
        var headerValues = capturedRequest.Headers.GetValues(CorrelationIdDelegatingHandler.HeaderName);
        Assert.Equal(existingCorrelationId, headerValues.Single());
    }

    [Fact]
    public async Task HttpClient_WithCorrelationIdHandler_GeneratesUniqueIdsPerRequest()
    {
        var correlationIds = new List<string>();

        var services = new ServiceCollection();
        services.AddSingleton<CorrelationIdDelegatingHandler>();
        services.AddHttpClient("test")
            .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
            .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler(req =>
            {
                var id = req.Headers.GetValues(CorrelationIdDelegatingHandler.HeaderName).First();
                correlationIds.Add(id);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }));

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("test");

        await client.GetAsync("http://localhost/test");
        await client.GetAsync("http://localhost/test");

        Assert.Equal(2, correlationIds.Count);
        Assert.NotEqual(correlationIds[0], correlationIds[1]);
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => handler(request);
    }
}
