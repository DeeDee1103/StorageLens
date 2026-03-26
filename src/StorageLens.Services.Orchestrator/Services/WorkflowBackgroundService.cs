using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StorageLens.Services.Orchestrator.Clients;
using StorageLens.Services.Orchestrator.Data;
using StorageLens.Services.Orchestrator.Interfaces;

namespace StorageLens.Services.Orchestrator.Services;

public class WorkflowBackgroundService(
    WorkflowQueue workflowQueue,
    IServiceScopeFactory scopeFactory,
    IOptions<WorkflowProcessingOptions> processingOptions,
    ILogger<WorkflowBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan ScheduledDispatchPollInterval = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var maxConcurrent = Math.Max(1, processingOptions.Value.MaxConcurrentWorkflows);
        var semaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
        var activeTasks = new System.Collections.Concurrent.ConcurrentDictionary<Guid, Task>();

        logger.LogInformation("Workflow background service started with MaxConcurrentWorkflows={Max}", maxConcurrent);
        await TryEnqueueRecoverableWorkflowsWithRetryAsync(stoppingToken);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Guid workflowId;
                try
                {
                    using var dequeueTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    dequeueTimeoutCts.CancelAfter(ScheduledDispatchPollInterval);
                    workflowId = await workflowQueue.DequeueAsync(dequeueTimeoutCts.Token);
                }
                catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                {
                    await TryEnqueueDueScheduledWorkflowsWithRetryAsync(stoppingToken);
                    continue;
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                await semaphore.WaitAsync(stoppingToken);

                // Use CancellationToken.None so that the already-dequeued workflow task is
                // always scheduled and started, even during shutdown. The stoppingToken is
                // passed into ProcessWorkflowAsync itself so the work can cooperatively
                // cancel; using stoppingToken here would prevent the task from ever starting
                // if the token is already cancelled, leaving the semaphore unreleased.
                var task = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = scopeFactory.CreateScope();
                        var orchestrator = scope.ServiceProvider.GetRequiredService<IWorkflowOrchestratorService>();
                        await orchestrator.ProcessWorkflowAsync(workflowId, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // Graceful shutdown — not an error.
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Workflow {WorkflowId} background processor failed", workflowId);
                    }
                    finally
                    {
                        semaphore.Release();
                        activeTasks.TryRemove(workflowId, out _);
                    }
                }, CancellationToken.None);

                activeTasks[workflowId] = task;

                await TryEnqueueDueScheduledWorkflowsWithRetryAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown path.
        }

        // Drain: wait for all in-flight workflows to finish before the host tears down.
        if (!activeTasks.IsEmpty)
        {
            logger.LogInformation("Waiting for {Count} in-flight workflow(s) to complete...", activeTasks.Count);
            await Task.WhenAll(activeTasks.Values);
        }
    }

    private async Task EnqueueRecoverableWorkflowsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        var workflowIds = await dbContext.WorkflowInstances
            .AsNoTracking()
            .Where(x => x.Status == "RetryQueued")
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var workflowId in workflowIds)
        {
            await workflowQueue.EnqueueAsync(workflowId, cancellationToken);
        }

        if (workflowIds.Count > 0)
        {
            logger.LogInformation("Recovered {Count} retry-queued workflow(s) into the in-memory queue.", workflowIds.Count);
        }
    }

    private async Task TryEnqueueRecoverableWorkflowsWithRetryAsync(CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(5);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await EnqueueRecoverableWorkflowsAsync(cancellationToken);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Unable to recover startup workflows yet. Retrying in {DelaySeconds}s.",
                    delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private async Task EnqueueDueScheduledWorkflowsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        var nowUtc = DateTimeOffset.UtcNow;
        var workflows = await dbContext.WorkflowInstances
            .Where(x => x.Status == "Pending" && x.CreatedAtUtc <= nowUtc)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (workflows.Count == 0)
        {
            return;
        }

        foreach (var workflow in workflows)
        {
            workflow.Status = "RetryQueued";
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var workflow in workflows)
        {
            await workflowQueue.EnqueueAsync(workflow.Id, cancellationToken);
        }

        logger.LogInformation("Enqueued {Count} scheduled workflow(s) that are now due.", workflows.Count);
    }

    private async Task TryEnqueueDueScheduledWorkflowsWithRetryAsync(CancellationToken cancellationToken)
    {
        try
        {
            await EnqueueDueScheduledWorkflowsAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to enqueue due scheduled workflows on this cycle.");
        }
    }
}
