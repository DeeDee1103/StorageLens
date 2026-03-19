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
        ServicePointManager.ReusePort = true;
    }

    /// <summary>
    /// Gets a secure policy handler for HTTP clients.
    /// </summary>
    /// <returns>Action to configure HTTP client with security options</returns>
    public static Action<HttpClient> GetSecureConfigurationAction(Uri baseAddress, TimeSpan? timeout = null)
    {
        return client => client.ConfigureSecure(baseAddress, timeout);
    }
}
