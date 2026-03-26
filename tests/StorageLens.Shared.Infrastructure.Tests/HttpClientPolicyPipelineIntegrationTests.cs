using System.Net;
using Microsoft.Extensions.DependencyInjection;
using StorageLens.Shared.Infrastructure;
using Xunit;

namespace StorageLens.Shared.Infrastructure.Tests;

public class HttpClientPolicyPipelineIntegrationTests
{
    [Fact]
    public async Task ConfiguredTypedClient_AppliesRetryPolicy_ForSafeGetRequestsOnly()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddTransient<CorrelationIdDelegatingHandler>();

        var resilience = new UpstreamResilienceOptions
        {
            MaxRetryAttempts = 2,
            TimeoutSeconds = 30,
            BaseDelaySeconds = 0.01,
            MaxDelaySeconds = 0.02,
            CircuitBreakerFailures = 50,
            CircuitBreakerBreakSeconds = 30
        };

        var getThenSuccessHandler = new SequenceHandler(
            HttpStatusCode.InternalServerError,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.OK);

        services
            .AddConfiguredUpstreamClient<TestTypedClient>(
                profileName: "test-upstream",
                baseAddress: "http://localhost",
                serviceApiKey: string.Empty,
                resilienceOptions: resilience)
            .ConfigurePrimaryHttpMessageHandler(() => getThenSuccessHandler);

        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<TestTypedClient>();

        var getResponse = await client.GetPingAsync();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(3, getThenSuccessHandler.CallCount);
    }

    [Fact]
    public async Task ConfiguredTypedClient_DoesNotRetry_ForPostRequests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddTransient<CorrelationIdDelegatingHandler>();

        var resilience = new UpstreamResilienceOptions
        {
            MaxRetryAttempts = 3,
            TimeoutSeconds = 30,
            BaseDelaySeconds = 0.01,
            MaxDelaySeconds = 0.02,
            CircuitBreakerFailures = 50,
            CircuitBreakerBreakSeconds = 30
        };

        var alwaysFailHandler = new SequenceHandler(HttpStatusCode.InternalServerError);

        services
            .AddConfiguredUpstreamClient<TestTypedClient>(
                profileName: "test-upstream",
                baseAddress: "http://localhost",
                serviceApiKey: string.Empty,
                resilienceOptions: resilience)
            .ConfigurePrimaryHttpMessageHandler(() => alwaysFailHandler);

        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<TestTypedClient>();

        var postResponse = await client.PostPingAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, postResponse.StatusCode);
        Assert.Equal(1, alwaysFailHandler.CallCount);
    }

    private sealed class TestTypedClient(HttpClient httpClient)
    {
        public Task<HttpResponseMessage> GetPingAsync() =>
            httpClient.GetAsync("/ping");

        public Task<HttpResponseMessage> PostPingAsync() =>
            httpClient.PostAsync("/ping", content: null);
    }

    private sealed class SequenceHandler(params HttpStatusCode[] sequence) : HttpMessageHandler
    {
        private readonly HttpStatusCode[] _sequence = sequence.Length == 0
            ? [HttpStatusCode.OK]
            : sequence;

        private int _index;

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount += 1;

            var currentIndex = Math.Min(_index, _sequence.Length - 1);
            var statusCode = _sequence[currentIndex];
            _index += 1;

            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                RequestMessage = request,
                Content = new StringContent("test")
            });
        }
    }
}
