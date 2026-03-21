using StorageLens.Services.Orchestrator.Models;
using StorageLens.Shared.Infrastructure;
using Xunit;

namespace StorageLens.Services.Orchestrator.Tests;

public class ArchitectureContractsTests
{
    [Fact]
    public void ResolveForClient_UsesClientOverride_WhenPresent()
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
                ["scanner"] = new UpstreamClientResilienceOptions
                {
                    MaxRetryAttempts = 1,
                    TimeoutSeconds = 14_400,
                    CircuitBreakerFailures = 3,
                    CircuitBreakerBreakSeconds = 20
                }
            }
        };

        var scannerProfile = UpstreamResilienceProfileResolver.ResolveForClient("scanner", options);
        var locationsProfile = UpstreamResilienceProfileResolver.ResolveForClient("locations", options);

        Assert.Equal(1, scannerProfile.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromHours(4), scannerProfile.Timeout);
        Assert.Equal(3, scannerProfile.CircuitBreakerFailures);

        Assert.Equal(3, locationsProfile.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(30), locationsProfile.Timeout);
        Assert.Equal(5, locationsProfile.CircuitBreakerFailures);
    }

    [Fact]
    public void WorkflowStepStateContract_RejectsInvalidTransition()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            WorkflowStepStateContract.EnsureTransitionAllowed(WorkflowStepStatus.Completed, WorkflowStepStatus.Running));

        Assert.Contains("Invalid workflow step status transition", ex.Message);
    }

    [Fact]
    public void WorkflowStepStateContract_RejectsUnknownStepName()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            WorkflowStepStateContract.EnsureKnownStepName("UnknownStep"));

        Assert.Contains("Unknown workflow step", ex.Message);
    }
}
