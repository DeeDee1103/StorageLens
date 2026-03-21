namespace StorageLens.Shared.Infrastructure;

public class UpstreamResilienceOptions
{
    public const string SectionName = "UpstreamResilience";

    public int MaxRetryAttempts { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 30;
    public double BaseDelaySeconds { get; set; } = 1;
    public double MaxDelaySeconds { get; set; } = 8;
    public int CircuitBreakerFailures { get; set; } = 5;
    public int CircuitBreakerBreakSeconds { get; set; } = 30;

    public Dictionary<string, UpstreamClientResilienceOptions> ClientOverrides { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

public class UpstreamClientResilienceOptions
{
    public int? MaxRetryAttempts { get; set; }
    public int? TimeoutSeconds { get; set; }
    public double? BaseDelaySeconds { get; set; }
    public double? MaxDelaySeconds { get; set; }
    public int? CircuitBreakerFailures { get; set; }
    public int? CircuitBreakerBreakSeconds { get; set; }
}

public sealed record ResolvedUpstreamResilienceProfile(
    int MaxRetryAttempts,
    TimeSpan Timeout,
    double BaseDelaySeconds,
    double MaxDelaySeconds,
    int CircuitBreakerFailures,
    int CircuitBreakerBreakSeconds);

public static class UpstreamResilienceProfileResolver
{
    public static ResolvedUpstreamResilienceProfile ResolveForClient(
        string clientName,
        UpstreamResilienceOptions options)
    {
        options.ClientOverrides.TryGetValue(clientName, out var clientOverride);

        var retryAttempts = Math.Max(1, clientOverride?.MaxRetryAttempts ?? options.MaxRetryAttempts);
        var timeoutSeconds = Math.Max(1, clientOverride?.TimeoutSeconds ?? options.TimeoutSeconds);
        var baseDelay = Math.Max(0.1, clientOverride?.BaseDelaySeconds ?? options.BaseDelaySeconds);
        var maxDelay = Math.Max(baseDelay, clientOverride?.MaxDelaySeconds ?? options.MaxDelaySeconds);
        var breakerFailures = Math.Max(2, clientOverride?.CircuitBreakerFailures ?? options.CircuitBreakerFailures);
        var breakerBreakSeconds = Math.Max(5, clientOverride?.CircuitBreakerBreakSeconds ?? options.CircuitBreakerBreakSeconds);

        return new ResolvedUpstreamResilienceProfile(
            retryAttempts,
            TimeSpan.FromSeconds(timeoutSeconds),
            baseDelay,
            maxDelay,
            breakerFailures,
            breakerBreakSeconds);
    }
}