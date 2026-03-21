using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StorageLens.Services.Orchestrator.Clients;
using StorageLens.Services.Orchestrator.Interfaces;
using StorageLens.Services.Orchestrator.Services;
using StorageLens.Shared.Contracts;
using Xunit;

namespace StorageLens.Services.Orchestrator.Tests;

public class WorkflowResiliencePerformanceTests
{
    [Fact]
    [Trait("Category", "Chaos")]
    public async Task ExecuteAsync_WhenOneWorkflowThrows_ContinuesProcessingRemainingWorkflows()
    {
        var queue = new WorkflowQueue();
        var failingWorkflowId = Guid.NewGuid();
        var succeedingIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var allIds = new[] { failingWorkflowId }.Concat(succeedingIds).ToList();

        var processed = new System.Collections.Concurrent.ConcurrentBag<Guid>();
        var attemptsDone = new CountdownEvent(allIds.Count);

        var orchestrator = new FakeOrchestrator(async (id, ct) =>
        {
            try
            {
                await Task.Delay(40, ct);
                if (id == failingWorkflowId)
                {
                    throw new HttpRequestException("Injected chaos fault.");
                }

                processed.Add(id);
            }
            finally
            {
                attemptsDone.Signal();
            }
        });

        var services = new ServiceCollection();
        services.AddSingleton<IWorkflowOrchestratorService>(orchestrator);
        var provider = services.BuildServiceProvider();

        var options = Options.Create(new WorkflowProcessingOptions { MaxConcurrentWorkflows = 2 });
        var service = new WorkflowBackgroundService(
            queue,
            provider.GetRequiredService<IServiceScopeFactory>(),
            options,
            NullLogger<WorkflowBackgroundService>.Instance);

        foreach (var id in allIds)
        {
            await queue.EnqueueAsync(id, CancellationToken.None);
        }

        using var cts = new CancellationTokenSource();
        _ = service.StartAsync(cts.Token);

        Assert.True(attemptsDone.Wait(TimeSpan.FromSeconds(5)), "Timed out waiting for workflow attempts.");

        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        Assert.DoesNotContain(failingWorkflowId, processed);
        Assert.All(succeedingIds, id => Assert.Contains(id, processed));
    }

    [Fact]
    [Trait("Category", "Performance")]
    public async Task ExecuteAsync_ProcessesQueuedWorkWithinExpectedWindow()
    {
        const int totalWorkflows = 24;
        const int maxConcurrent = 6;
        var queue = new WorkflowQueue();
        var ids = Enumerable.Range(0, totalWorkflows).Select(_ => Guid.NewGuid()).ToList();
        var done = new CountdownEvent(totalWorkflows);

        var orchestrator = new FakeOrchestrator(async (_, ct) =>
        {
            try
            {
                await Task.Delay(40, ct);
            }
            finally
            {
                done.Signal();
            }
        });

        var services = new ServiceCollection();
        services.AddSingleton<IWorkflowOrchestratorService>(orchestrator);
        var provider = services.BuildServiceProvider();

        var options = Options.Create(new WorkflowProcessingOptions { MaxConcurrentWorkflows = maxConcurrent });
        var service = new WorkflowBackgroundService(
            queue,
            provider.GetRequiredService<IServiceScopeFactory>(),
            options,
            NullLogger<WorkflowBackgroundService>.Instance);

        foreach (var id in ids)
        {
            await queue.EnqueueAsync(id, CancellationToken.None);
        }

        using var cts = new CancellationTokenSource();
        _ = service.StartAsync(cts.Token);

        var started = DateTimeOffset.UtcNow;
        Assert.True(done.Wait(TimeSpan.FromSeconds(8)), "Timed out waiting for workflows to finish.");
        var elapsed = DateTimeOffset.UtcNow - started;

        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        Assert.True(
            elapsed < TimeSpan.FromSeconds(4),
            $"Workflow batch took longer than expected: {elapsed.TotalMilliseconds} ms.");
    }

    private sealed class FakeOrchestrator(Func<Guid, CancellationToken, Task> handler) : IWorkflowOrchestratorService
    {
        public Task<WorkflowStartResultDto> StartScanAsync(Guid locationId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<WorkflowInstanceDto?> GetWorkflowAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<WorkflowStartResultDto?> ResumeAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<WorkflowStartResultDto?> RetryAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public ValueTask EnqueueAsync(Guid workflowId, CancellationToken cancellationToken) => ValueTask.CompletedTask;

        public Task ProcessWorkflowAsync(Guid workflowId, CancellationToken cancellationToken) =>
            handler(workflowId, cancellationToken);
    }
}
