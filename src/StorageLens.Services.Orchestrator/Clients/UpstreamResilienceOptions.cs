namespace StorageLens.Services.Orchestrator.Clients;

public class UpstreamResilienceOptions
{
    public const string SectionName = "UpstreamResilience";

    /// <summary>
    /// Number of retry attempts for transient failures on safe/idempotent requests.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Request timeout in seconds used when client-specific override is not configured.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Base backoff delay in seconds.
    /// </summary>
    public double BaseDelaySeconds { get; set; } = 1;

    /// <summary>
    /// Maximum retry delay cap in seconds.
    /// </summary>
    public double MaxDelaySeconds { get; set; } = 8;

    /// <summary>
    /// Consecutive transient failures before opening the circuit breaker.
    /// </summary>
    public int CircuitBreakerFailures { get; set; } = 5;

    /// <summary>
    /// Duration to keep circuit open in seconds.
    /// </summary>
    public int CircuitBreakerBreakSeconds { get; set; } = 30;

    /// <summary>
    /// Optional per-upstream client overrides keyed by client name.
    /// </summary>
    public Dictionary<string, UpstreamClientResilienceOptions> ClientOverrides { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

public class UpstreamClientResilienceOptions
{
    public int? MaxRetryAttempts { get; set; }
    public double? BaseDelaySeconds { get; set; }
    public double? MaxDelaySeconds { get; set; }
    public int? CircuitBreakerFailures { get; set; }
    public int? CircuitBreakerBreakSeconds { get; set; }
    public int? TimeoutSeconds { get; set; }
}
