using StorageLens.Services.Orchestrator.Interfaces;

namespace StorageLens.Services.Orchestrator.Services;

public class WorkflowBackgroundService(
    WorkflowQueue workflowQueue,
    IServiceScopeFactory scopeFactory,
    ILogger<WorkflowBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workflowId = await workflowQueue.DequeueAsync(stoppingToken);

                using var scope = scopeFactory.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<IWorkflowOrchestratorService>();
                await orchestrator.ProcessWorkflowAsync(workflowId, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Workflow background processor failed");
            }
        }
    }
}
