using StorageLens.Services.Orchestrator.Models;

namespace StorageLens.Services.Orchestrator.Repositories;

/// <summary>
/// Abstracts all EF Core data-access operations for the Orchestrator service,
/// allowing <see cref="Services.WorkflowOrchestratorService"/> to be tested without a direct EF Core dependency.
/// </summary>
public interface IWorkflowRepository
{
    Task<WorkflowInstance?> GetByIdAsync(Guid id, bool includeSteps, CancellationToken cancellationToken);
    Task<WorkflowInstance?> GetByScanJobIdAsync(Guid scanJobId, CancellationToken cancellationToken);
    Task AddWorkflowAsync(WorkflowInstance workflow, CancellationToken cancellationToken);
    void AddStepExecution(WorkflowStepExecution step);
    void AddError(WorkflowError error);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
