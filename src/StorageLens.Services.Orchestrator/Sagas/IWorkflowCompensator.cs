using StorageLens.Services.Orchestrator.Models;

namespace StorageLens.Services.Orchestrator.Sagas;

/// <summary>
/// Executes best-effort compensating transactions for a failed workflow,
/// undoing side-effects of steps that already completed before the failure.
/// <para>
/// Compensation is idempotent: calling it multiple times for the same workflow is safe.
/// Each step's compensating action is attempted independently so that a failure in one
/// compensation does not prevent the others from running.
/// </para>
/// </summary>
public interface IWorkflowCompensator
{
    /// <summary>
    /// Compensates the side-effects of a failed workflow by issuing cleanup requests
    /// to downstream services for every step that was already recorded as completed.
    /// </summary>
    Task CompensateAsync(WorkflowInstance workflow, CancellationToken cancellationToken);
}
