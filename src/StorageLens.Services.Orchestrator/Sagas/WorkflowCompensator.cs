using StorageLens.Services.Orchestrator.Models;

namespace StorageLens.Services.Orchestrator.Sagas;

/// <summary>
/// Issues best-effort compensating HTTP calls to downstream services to undo
/// side-effects of completed workflow steps when the overall workflow fails.
/// </summary>
/// <remarks>
/// Compensation strategy per step:
/// <list type="bullet">
///   <item><term>Scanning</term><description>
///     Deletes all file records ingested for the scan job from FileInventory, giving the
///     next retry a clean slate to re-scan without stale or duplicate file records.
///   </description></item>
///   <item><term>DetectingDuplicates</term><description>
///     Deletes all duplicate groups calculated for the scan job from the Duplicates service.
///     (The Duplicates service already cleans up on recalculate, but explicit compensation
///     ensures orphaned groups are not left behind when no retry follows.)
///   </description></item>
///   <item><term>Hashing / RefreshingAnalytics</term><description>
///     No explicit compensation. Hash results are stored as attributes of FileInventory
///     records (removed by Scanning compensation). Analytics is stateless and recomputed
///     on the next successful run.
///   </description></item>
/// </list>
/// All compensation calls are fire-and-log: a failure in one step's compensation is
/// logged but does not prevent compensation for subsequent steps from being attempted.
/// </remarks>
public class WorkflowCompensator(
    IHttpClientFactory httpClientFactory,
    ILogger<WorkflowCompensator> logger) : IWorkflowCompensator
{
    public async Task CompensateAsync(WorkflowInstance workflow, CancellationToken cancellationToken)
    {
        var completedStepNames = workflow.Steps
            .Where(s => s.Status == WorkflowStepStatus.Completed.ToString())
            .Select(s => s.StepName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (completedStepNames.Count == 0)
        {
            logger.LogDebug(
                "Workflow {WorkflowId}: no completed steps to compensate.",
                workflow.Id);
            return;
        }

        logger.LogInformation(
            "Workflow {WorkflowId}: beginning compensation for completed steps: {Steps}.",
            workflow.Id,
            string.Join(", ", completedStepNames));

        // Scanning compensation: remove file records for this scan job from FileInventory.
        if (completedStepNames.Contains("Scanning"))
        {
            await CompensateScanningAsync(workflow, cancellationToken);
        }

        // DetectingDuplicates compensation: remove duplicate groups for this scan job.
        if (completedStepNames.Contains("DetectingDuplicates"))
        {
            await CompensateDuplicatesAsync(workflow, cancellationToken);
        }
    }

    private async Task CompensateScanningAsync(WorkflowInstance workflow, CancellationToken cancellationToken)
    {
        try
        {
            var client = httpClientFactory.CreateClient("fileinventory");
            var url = $"api/files/by-scan-job/{workflow.ScanJobId}?correlationId={Uri.EscapeDataString(workflow.CorrelationId)}";
            using var response = await client.DeleteAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "Workflow {WorkflowId}: compensated Scanning step — deleted file records for scan job {ScanJobId}.",
                    workflow.Id, workflow.ScanJobId);
            }
            else
            {
                logger.LogWarning(
                    "Workflow {WorkflowId}: Scanning compensation returned HTTP {Status} for scan job {ScanJobId}.",
                    workflow.Id, (int)response.StatusCode, workflow.ScanJobId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Workflow {WorkflowId}: Scanning compensation failed for scan job {ScanJobId}. " +
                "File records may need manual cleanup.",
                workflow.Id, workflow.ScanJobId);
        }
    }

    private async Task CompensateDuplicatesAsync(WorkflowInstance workflow, CancellationToken cancellationToken)
    {
        try
        {
            var client = httpClientFactory.CreateClient("duplicates");
            var url = $"api/duplicates/by-scan-job/{workflow.ScanJobId}?correlationId={Uri.EscapeDataString(workflow.CorrelationId)}";
            using var response = await client.DeleteAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "Workflow {WorkflowId}: compensated DetectingDuplicates step — deleted duplicate groups for scan job {ScanJobId}.",
                    workflow.Id, workflow.ScanJobId);
            }
            else
            {
                logger.LogWarning(
                    "Workflow {WorkflowId}: DetectingDuplicates compensation returned HTTP {Status} for scan job {ScanJobId}.",
                    workflow.Id, (int)response.StatusCode, workflow.ScanJobId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Workflow {WorkflowId}: DetectingDuplicates compensation failed for scan job {ScanJobId}. " +
                "Duplicate groups may need manual cleanup.",
                workflow.Id, workflow.ScanJobId);
        }
    }
}
