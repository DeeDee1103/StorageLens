using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace StorageLens.Shared.Infrastructure;

/// <summary>
/// HTTP client configuration builder with security and resilience settings.
/// Provides consistent timeouts, retry policies, and connection handling across all services.
/// </summary>
public static class HttpClientConfigurationExtensions
{
    /// <summary>
    /// Default timeout for HTTP requests (30 seconds).
    /// Prevents hung connections from accumulating.
    /// </summary>
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Configures an HTTP client with secure defaults: reasonable timeouts, connection management,
    /// and request header limits.
    /// </summary>
    /// <param name="client">The HttpClient to configure</param>
    /// <param name="baseAddress">Optional base address for the client</param>
    /// <param name="timeout">Optional custom timeout (defaults to 30 seconds)</param>
    public static void ConfigureSecure(
        this HttpClient client,
        Uri? baseAddress = null,
        TimeSpan? timeout = null)
    {
        if (baseAddress != null)
            client.BaseAddress = baseAddress;

        // Set reasonable timeout to prevent hung connections
        client.Timeout = timeout ?? DefaultTimeout;

        // Limit request content buffer size to prevent memory exhaustion
        client.MaxResponseContentBufferSize = 100 * 1024 * 1024; // 100MB

        // Configure HTTP connection pooling
        ServicePointManager.DefaultConnectionLimit = 10;
    }

    /// <summary>
    /// Gets a secure policy handler for HTTP clients.
    /// </summary>
    /// <returns>Action to configure HTTP client with security options</returns>
    public static Action<HttpClient> GetSecureConfigurationAction(Uri baseAddress, TimeSpan? timeout = null)
    {
        return client => client.ConfigureSecure(baseAddress, timeout);
    }

    public static IHttpClientBuilder AddConfiguredUpstreamClient(
        this IServiceCollection services,
        string name,
        string baseAddress,
        string serviceApiKey,
        UpstreamResilienceOptions resilienceOptions)
    {
        var profile = UpstreamResilienceProfileResolver.ResolveForClient(name, resilienceOptions);

        IAsyncPolicy<HttpResponseMessage> noOpPolicy = Policy.NoOpAsync<HttpResponseMessage>();

        IAsyncPolicy<HttpResponseMessage> retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                profile.MaxRetryAttempts,
                retryAttempt =>
                {
                    var exponential = profile.BaseDelaySeconds * Math.Pow(2, retryAttempt - 1);
                    var bounded = Math.Min(exponential, profile.MaxDelaySeconds);
                    return TimeSpan.FromSeconds(bounded);
                });

        IAsyncPolicy<HttpResponseMessage> circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                profile.CircuitBreakerFailures,
                TimeSpan.FromSeconds(profile.CircuitBreakerBreakSeconds));

        return services
            .AddHttpClient(name, client =>
            {
                client.ConfigureSecure(new Uri(baseAddress), profile.Timeout);
                if (!string.IsNullOrWhiteSpace(serviceApiKey))
                    client.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", serviceApiKey);
            })
            .AddPolicyHandler(request => ShouldApplyRetryPolicy(request.Method) ? retryPolicy : noOpPolicy)
            .AddPolicyHandler(circuitBreakerPolicy);
    }

    public static IHttpClientBuilder AddConfiguredUpstreamClient<TClient>(
        this IServiceCollection services,
        string profileName,
        string baseAddress,
        string serviceApiKey,
        UpstreamResilienceOptions resilienceOptions)
        where TClient : class
    {
        var profile = UpstreamResilienceProfileResolver.ResolveForClient(profileName, resilienceOptions);

        IAsyncPolicy<HttpResponseMessage> noOpPolicy = Policy.NoOpAsync<HttpResponseMessage>();

        IAsyncPolicy<HttpResponseMessage> retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                profile.MaxRetryAttempts,
                retryAttempt =>
                {
                    var exponential = profile.BaseDelaySeconds * Math.Pow(2, retryAttempt - 1);
                    var bounded = Math.Min(exponential, profile.MaxDelaySeconds);
                    return TimeSpan.FromSeconds(bounded);
                });

        IAsyncPolicy<HttpResponseMessage> circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                profile.CircuitBreakerFailures,
                TimeSpan.FromSeconds(profile.CircuitBreakerBreakSeconds));

        return services
            .AddHttpClient<TClient>(client =>
            {
                client.ConfigureSecure(new Uri(baseAddress), profile.Timeout);
                if (!string.IsNullOrWhiteSpace(serviceApiKey))
                    client.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", serviceApiKey);
            })
            .AddPolicyHandler(request => ShouldApplyRetryPolicy(request.Method) ? retryPolicy : noOpPolicy)
            .AddPolicyHandler(circuitBreakerPolicy);
    }

    public static bool ShouldApplyRetryPolicy(HttpMethod method) =>
        method == HttpMethod.Get ||
        method == HttpMethod.Head ||
        method == HttpMethod.Options;
}
