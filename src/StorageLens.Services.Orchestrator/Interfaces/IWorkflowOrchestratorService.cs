using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Orchestrator.Interfaces;

public interface IWorkflowOrchestratorService
{
    Task<WorkflowStartResultDto> StartScanAsync(Guid locationId, CancellationToken cancellationToken);
    Task<WorkflowInstanceDto?> GetWorkflowAsync(Guid id, CancellationToken cancellationToken);
    Task<WorkflowStartResultDto?> ResumeAsync(Guid id, CancellationToken cancellationToken);
    Task<WorkflowStartResultDto?> RetryAsync(Guid id, CancellationToken cancellationToken);
    ValueTask EnqueueAsync(Guid workflowId, CancellationToken cancellationToken);
    Task ProcessWorkflowAsync(Guid workflowId, CancellationToken cancellationToken);
}
