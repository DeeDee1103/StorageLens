using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using StorageLens.Services.Orchestrator.Clients;
using StorageLens.Services.Orchestrator.Data;
using StorageLens.Services.Orchestrator.Interfaces;
using StorageLens.Services.Orchestrator.Models;
using StorageLens.Services.Orchestrator.Services;
using StorageLens.Shared.Contracts;
using Xunit;

namespace StorageLens.Services.Orchestrator.Tests;

public class WorkflowBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ProcessesMultipleWorkflowsConcurrently_UpToMaxLimit()
    {
        const int maxConcurrent = 2;
        const int totalWorkflows = 4;

        var queue = new WorkflowQueue();
        var workflowIds = Enumerable.Range(0, totalWorkflows).Select(_ => Guid.NewGuid()).ToList();

        var concurrentCount = 0;
        var peakConcurrent = 0;
        var peakLock = new object();
        var processedIds = new System.Collections.Concurrent.ConcurrentBag<Guid>();
        var allDone = new CountdownEvent(totalWorkflows);

        var mockOrchestrator = new FakeOrchestrator(async (id, ct) =>
        {
            var current = Interlocked.Increment(ref concurrentCount);
            lock (peakLock)
            {
                if (current > peakConcurrent)
                    peakConcurrent = current;
            }

            await Task.Delay(50, ct);

            Interlocked.Decrement(ref concurrentCount);
            processedIds.Add(id);
            allDone.Signal();
        });

        var services = new ServiceCollection();
        services.AddDbContext<OrchestratorDbContext>(options =>
            options.UseInMemoryDatabase($"background-service-tests-{Guid.NewGuid()}"));
        services.AddSingleton<IWorkflowOrchestratorService>(mockOrchestrator);
        var serviceProvider = services.BuildServiceProvider();

        var options = Options.Create(new WorkflowProcessingOptions { MaxConcurrentWorkflows = maxConcurrent });

        var svc = new WorkflowBackgroundService(
            queue,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            options,
            NullLogger<WorkflowBackgroundService>.Instance);

        foreach (var id in workflowIds)
            await queue.EnqueueAsync(id, CancellationToken.None);

        using var cts = new CancellationTokenSource();
        _ = svc.StartAsync(cts.Token);

        // Wait until all workflows have signalled completion (5 s safety timeout).
        Assert.True(allDone.Wait(TimeSpan.FromSeconds(5)), "Timed out waiting for all workflows to complete.");

        await cts.CancelAsync();
        await svc.StopAsync(CancellationToken.None);

        Assert.Equal(totalWorkflows, processedIds.Count);
        Assert.True(peakConcurrent >= 2,
            $"Expected peak concurrent to be >= 2 (actual: {peakConcurrent})");
        Assert.True(peakConcurrent <= maxConcurrent,
            $"Peak concurrent ({peakConcurrent}) exceeded MaxConcurrentWorkflows ({maxConcurrent})");
    }

    [Fact]
    public async Task ExecuteAsync_RespectsCancellation_DoesNotProcessAfterStop()
    {
        var queue = new WorkflowQueue();
        var processedIds = new System.Collections.Concurrent.ConcurrentBag<Guid>();
        var processingStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var mockOrchestrator = new FakeOrchestrator(async (id, ct) =>
        {
            processingStarted.TrySetResult(true);
            await Task.Delay(500, ct);
            processedIds.Add(id);
        });

        var services = new ServiceCollection();
        services.AddDbContext<OrchestratorDbContext>(options =>
            options.UseInMemoryDatabase($"background-service-tests-{Guid.NewGuid()}"));
        services.AddSingleton<IWorkflowOrchestratorService>(mockOrchestrator);
        var serviceProvider = services.BuildServiceProvider();

        var options = Options.Create(new WorkflowProcessingOptions { MaxConcurrentWorkflows = 2 });

        var svc = new WorkflowBackgroundService(
            queue,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            options,
            NullLogger<WorkflowBackgroundService>.Instance);

        using var cts = new CancellationTokenSource();
        _ = svc.StartAsync(cts.Token);

        // Cancel before any workflows are queued so the dequeue loop exits cleanly.
        await cts.CancelAsync();
        await svc.StopAsync(CancellationToken.None);

        // Queue a workflow after stop — the background loop has already exited.
        await queue.EnqueueAsync(Guid.NewGuid(), CancellationToken.None);

        // Give any hypothetical leaked task a moment to run.
        await Task.Delay(50);

        Assert.Empty(processedIds);
    }

    [Fact]
    public async Task EnqueueDueScheduledWorkflowsAsync_QueuesOnlyDuePendingWorkflows()
    {
        var dueWorkflowId = Guid.NewGuid();
        var futureWorkflowId = Guid.NewGuid();
        var dbName = $"background-service-scheduled-{Guid.NewGuid()}";

        var services = new ServiceCollection();
        services.AddDbContext<OrchestratorDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddSingleton<IWorkflowOrchestratorService>(new FakeOrchestrator((_, _) => Task.CompletedTask));

        await using var provider = services.BuildServiceProvider();
        await using (var scope = provider.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
            dbContext.WorkflowInstances.AddRange(
                new WorkflowInstance
                {
                    Id = dueWorkflowId,
                    StorageLocationId = Guid.NewGuid(),
                    ScanJobId = Guid.NewGuid(),
                    CorrelationId = "corr-due",
                    RootPath = @"A:\\scheduled\\due",
                    Status = WorkflowStatus.Pending.ToString(),
                    CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
                    LastError = string.Empty
                },
                new WorkflowInstance
                {
                    Id = futureWorkflowId,
                    StorageLocationId = Guid.NewGuid(),
                    ScanJobId = Guid.NewGuid(),
                    CorrelationId = "corr-future",
                    RootPath = @"A:\\scheduled\\future",
                    Status = WorkflowStatus.Pending.ToString(),
                    CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(10),
                    LastError = string.Empty
                });

            await dbContext.SaveChangesAsync();
        }

        var queue = new WorkflowQueue();
        var svc = new WorkflowBackgroundService(
            queue,
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new WorkflowProcessingOptions { MaxConcurrentWorkflows = 1 }),
            NullLogger<WorkflowBackgroundService>.Instance);

        var method = typeof(WorkflowBackgroundService)
            .GetMethod("EnqueueDueScheduledWorkflowsAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);

        var invoked = (Task?)method!.Invoke(svc, [CancellationToken.None]);
        Assert.NotNull(invoked);
        await invoked!;

        using var dequeueTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var dequeuedId = await queue.DequeueAsync(dequeueTimeout.Token);
        Assert.Equal(dueWorkflowId, dequeuedId);

        using var secondTimeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await queue.DequeueAsync(secondTimeout.Token);
        });

        await using var verifyScope = provider.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        var dueWorkflow = await verifyDb.WorkflowInstances.FirstAsync(x => x.Id == dueWorkflowId);
        var futureWorkflow = await verifyDb.WorkflowInstances.FirstAsync(x => x.Id == futureWorkflowId);

        Assert.Equal(WorkflowStatus.RetryQueued.ToString(), dueWorkflow.Status);
        Assert.Equal(WorkflowStatus.Pending.ToString(), futureWorkflow.Status);
    }

    private sealed class FakeOrchestrator(Func<Guid, CancellationToken, Task> handler) : IWorkflowOrchestratorService
    {
        public Task<WorkflowStartResultDto> StartScanAsync(Guid locationId, DateTimeOffset? scheduledStartUtc, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<WorkflowInstanceDto?> GetWorkflowAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<WorkflowStartResultDto?> ResumeAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<WorkflowStartResultDto?> RetryAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<bool> CancelByScanJobIdAsync(Guid scanJobId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<WorkflowStartResultDto?> ResumeByScanJobIdAsync(Guid scanJobId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public ValueTask EnqueueAsync(Guid workflowId, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;

        public Task ProcessWorkflowAsync(Guid workflowId, CancellationToken cancellationToken) =>
            handler(workflowId, cancellationToken);
    }
}
