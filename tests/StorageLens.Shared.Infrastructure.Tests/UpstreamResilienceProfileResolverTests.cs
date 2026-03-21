using StorageLens.Shared.Infrastructure;
using Xunit;

namespace StorageLens.Shared.Infrastructure.Tests;

public class UpstreamResilienceProfileResolverTests
{
    [Fact]
    public void ResolveForClient_UsesGlobalDefaults_WhenNoOverrideExists()
    {
        var options = new UpstreamResilienceOptions
        {
            MaxRetryAttempts = 3,
            TimeoutSeconds = 30,
            BaseDelaySeconds = 1,
            MaxDelaySeconds = 8,
            CircuitBreakerFailures = 5,
            CircuitBreakerBreakSeconds = 30
        };

        var profile = UpstreamResilienceProfileResolver.ResolveForClient("locations", options);

        Assert.Equal(3, profile.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(30), profile.Timeout);
        Assert.Equal(1, profile.BaseDelaySeconds);
        Assert.Equal(8, profile.MaxDelaySeconds);
        Assert.Equal(5, profile.CircuitBreakerFailures);
        Assert.Equal(30, profile.CircuitBreakerBreakSeconds);
    }

    [Fact]
    public void ResolveForClient_UsesOverride_WhenProvided()
    {
        var options = new UpstreamResilienceOptions
        {
            MaxRetryAttempts = 3,
            TimeoutSeconds = 30,
            BaseDelaySeconds = 1,
            MaxDelaySeconds = 8,
            CircuitBreakerFailures = 5,
            CircuitBreakerBreakSeconds = 30,
            ClientOverrides = new Dictionary<string, UpstreamClientResilienceOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["orchestrator"] = new UpstreamClientResilienceOptions
                {
                    MaxRetryAttempts = 1,
                    TimeoutSeconds = 60,
                    BaseDelaySeconds = 0.5,
                    MaxDelaySeconds = 2,
                    CircuitBreakerFailures = 3,
                    CircuitBreakerBreakSeconds = 15
                }
            }
        };

        var profile = UpstreamResilienceProfileResolver.ResolveForClient("orchestrator", options);

        Assert.Equal(1, profile.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(60), profile.Timeout);
        Assert.Equal(0.5, profile.BaseDelaySeconds);
        Assert.Equal(2, profile.MaxDelaySeconds);
        Assert.Equal(3, profile.CircuitBreakerFailures);
        Assert.Equal(15, profile.CircuitBreakerBreakSeconds);
    }

    [Fact]
    public void ResolveForClient_ClampsInvalidValues_ToSafeMinimums()
    {
        var options = new UpstreamResilienceOptions
        {
            MaxRetryAttempts = 0,
            TimeoutSeconds = 0,
            BaseDelaySeconds = 0,
            MaxDelaySeconds = 0,
            CircuitBreakerFailures = 0,
            CircuitBreakerBreakSeconds = 0
        };

        var profile = UpstreamResilienceProfileResolver.ResolveForClient("any", options);

        Assert.Equal(1, profile.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(1), profile.Timeout);
        Assert.Equal(0.1, profile.BaseDelaySeconds);
        Assert.Equal(0.1, profile.MaxDelaySeconds);
        Assert.Equal(2, profile.CircuitBreakerFailures);
        Assert.Equal(5, profile.CircuitBreakerBreakSeconds);
    }
}
