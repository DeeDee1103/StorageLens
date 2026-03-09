using Microsoft.Extensions.Options;
using StorageLens.Services.Orchestrator.Clients;
using StorageLens.Services.Orchestrator.Interfaces;

namespace StorageLens.Services.Orchestrator.Services;

public class WorkflowBackgroundService(
    WorkflowQueue workflowQueue,
    IServiceScopeFactory scopeFactory,
    IOptions<WorkflowProcessingOptions> processingOptions,
    ILogger<WorkflowBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var maxConcurrent = Math.Max(1, processingOptions.Value.MaxConcurrentWorkflows);
        var semaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
        var activeTasks = new System.Collections.Concurrent.ConcurrentDictionary<Guid, Task>();

        logger.LogInformation("Workflow background service started with MaxConcurrentWorkflows={Max}", maxConcurrent);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Guid workflowId;
                try
                {
                    workflowId = await workflowQueue.DequeueAsync(stoppingToken);
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
}
