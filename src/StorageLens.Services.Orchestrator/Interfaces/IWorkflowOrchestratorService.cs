using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Orchestrator.Interfaces;

public interface IWorkflowOrchestratorService
{
    Task<WorkflowStartResultDto> StartScanAsync(Guid locationId, DateTimeOffset? scheduledStartUtc, CancellationToken cancellationToken);
    Task<WorkflowInstanceDto?> GetWorkflowAsync(Guid id, CancellationToken cancellationToken);
    Task<WorkflowStartResultDto?> ResumeAsync(Guid id, CancellationToken cancellationToken);
    Task<WorkflowStartResultDto?> RetryAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> CancelByScanJobIdAsync(Guid scanJobId, CancellationToken cancellationToken);
    Task<WorkflowStartResultDto?> ResumeByScanJobIdAsync(Guid scanJobId, CancellationToken cancellationToken);
    ValueTask EnqueueAsync(Guid workflowId, CancellationToken cancellationToken);
    Task ProcessWorkflowAsync(Guid workflowId, CancellationToken cancellationToken);
}
