namespace StorageLens.Services.Orchestrator.Clients;

public sealed record ResolvedUpstreamResilienceProfile(
    int MaxRetryAttempts,
    double BaseDelaySeconds,
    double MaxDelaySeconds,
    int CircuitBreakerFailures,
    int CircuitBreakerBreakSeconds,
    TimeSpan Timeout);

public static class UpstreamResilienceProfileResolver
{
    public static ResolvedUpstreamResilienceProfile ResolveForClient(
        string clientName,
        UpstreamResilienceOptions options)
    {
        options.ClientOverrides.TryGetValue(clientName, out var clientOverride);

        var retryAttempts = Math.Max(1, clientOverride?.MaxRetryAttempts ?? options.MaxRetryAttempts);
        var baseDelay = Math.Max(0.1, clientOverride?.BaseDelaySeconds ?? options.BaseDelaySeconds);
        var maxDelay = Math.Max(baseDelay, clientOverride?.MaxDelaySeconds ?? options.MaxDelaySeconds);
        var breakerFailures = Math.Max(2, clientOverride?.CircuitBreakerFailures ?? options.CircuitBreakerFailures);
        var breakerBreakSeconds = Math.Max(5, clientOverride?.CircuitBreakerBreakSeconds ?? options.CircuitBreakerBreakSeconds);
        var timeoutSeconds = Math.Max(1, clientOverride?.TimeoutSeconds ?? options.TimeoutSeconds);

        return new ResolvedUpstreamResilienceProfile(
            retryAttempts,
            baseDelay,
            maxDelay,
            breakerFailures,
            breakerBreakSeconds,
            TimeSpan.FromSeconds(timeoutSeconds));
    }
}
