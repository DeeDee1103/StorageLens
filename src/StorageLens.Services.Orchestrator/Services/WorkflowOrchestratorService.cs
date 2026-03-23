using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using StorageLens.Services.Orchestrator.Data;
using StorageLens.Services.Orchestrator.Interfaces;
using StorageLens.Services.Orchestrator.Models;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Orchestrator.Services;

public class WorkflowOrchestratorService(
    OrchestratorDbContext dbContext,
    WorkflowQueue workflowQueue,
    IHttpClientFactory httpClientFactory,
    ILogger<WorkflowOrchestratorService> logger) : IWorkflowOrchestratorService
{
    private static readonly IReadOnlyDictionary<WorkflowStatus, HashSet<WorkflowStatus>> AllowedTransitions =
        new Dictionary<WorkflowStatus, HashSet<WorkflowStatus>>
        {
            [WorkflowStatus.Pending] = [WorkflowStatus.Running, WorkflowStatus.RetryQueued, WorkflowStatus.Failed, WorkflowStatus.Cancelled],
            [WorkflowStatus.Running] = [WorkflowStatus.Completed, WorkflowStatus.Failed, WorkflowStatus.RetryQueued, WorkflowStatus.Cancelled],
            [WorkflowStatus.Completed] = [WorkflowStatus.RetryQueued],
            [WorkflowStatus.Failed] = [WorkflowStatus.RetryQueued],
            [WorkflowStatus.RetryQueued] = [WorkflowStatus.Running, WorkflowStatus.Failed, WorkflowStatus.RetryQueued, WorkflowStatus.Cancelled],
            [WorkflowStatus.Cancelled] = [WorkflowStatus.RetryQueued]
        };

    public async Task<WorkflowStartResultDto> StartScanAsync(Guid locationId, CancellationToken cancellationToken)
    {
        var locationsClient = httpClientFactory.CreateClient("locations");
        var scanJobsClient = httpClientFactory.CreateClient("scanjobs");

        var location = await locationsClient.GetFromJsonAsync<StorageLocationDto>($"api/locations/{locationId}", cancellationToken)
                       ?? throw new InvalidOperationException("Location not found.");

        var correlationId = Guid.NewGuid().ToString("N");
        var scanJob = await scanJobsClient.PostAsJsonAsync(
            "api/scanjobs",
            new CreateScanJobRequest(locationId, "Workflow scan request", correlationId, location.Name),
            cancellationToken);
        scanJob.EnsureSuccessStatusCode();

        var createdJob = await scanJob.Content.ReadFromJsonAsync<ScanJobDto>(cancellationToken)
                         ?? throw new InvalidOperationException("Scan job creation failed.");

        var workflow = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            StorageLocationId = locationId,
            ScanJobId = createdJob.Id,
            CorrelationId = correlationId,
            RootPath = location.Path,
            Status = WorkflowStatus.Pending.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastError = string.Empty
        };

        dbContext.WorkflowInstances.Add(workflow);
        await dbContext.SaveChangesAsync(cancellationToken);

        await EnqueueAsync(workflow.Id, cancellationToken);

        return new WorkflowStartResultDto(workflow.Id, createdJob.Id, correlationId, workflow.Status);
    }

    public async Task<WorkflowInstanceDto?> GetWorkflowAsync(Guid id, CancellationToken cancellationToken)
    {
        var workflow = await dbContext.WorkflowInstances
            .Include(x => x.Steps)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return workflow is null ? null : Map(workflow);
    }

    public async Task<WorkflowStartResultDto?> ResumeAsync(Guid id, CancellationToken cancellationToken)
    {
        var workflow = await dbContext.WorkflowInstances.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (workflow is null)
        {
            return null;
        }

        SetWorkflowStatus(workflow, WorkflowStatus.RetryQueued);
        workflow.LastError = string.Empty;
        await dbContext.SaveChangesAsync(cancellationToken);

        await EnqueueAsync(workflow.Id, cancellationToken);
        return new WorkflowStartResultDto(workflow.Id, workflow.ScanJobId, workflow.CorrelationId, workflow.Status);
    }

    public async Task<WorkflowStartResultDto?> RetryAsync(Guid id, CancellationToken cancellationToken)
    {
        var workflow = await dbContext.WorkflowInstances.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (workflow is null)
        {
            return null;
        }

        workflow.RetryCount += 1;
        SetWorkflowStatus(workflow, WorkflowStatus.RetryQueued);
        await dbContext.SaveChangesAsync(cancellationToken);

        await EnqueueAsync(workflow.Id, cancellationToken);
        return new WorkflowStartResultDto(workflow.Id, workflow.ScanJobId, workflow.CorrelationId, workflow.Status);
    }

    public async Task<bool> CancelByScanJobIdAsync(Guid scanJobId, CancellationToken cancellationToken)
    {
        var workflow = await dbContext.WorkflowInstances
            .FirstOrDefaultAsync(x => x.ScanJobId == scanJobId, cancellationToken);
        if (workflow is null)
        {
            return false;
        }

        SetWorkflowStatus(workflow, WorkflowStatus.Cancelled);
        workflow.LastError = "Cancelled by user.";
        workflow.CompletedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var scanJobsClient = httpClientFactory.CreateClient("scanjobs");
        await scanJobsClient.PutAsJsonAsync(
            $"api/scanjobs/{scanJobId}/status",
            new UpdateScanJobStatusRequest(ScanJobStatus.Cancelled, "Cancelled", "Cancelled by user.", IsTerminal: true),
            cancellationToken);

        logger.LogInformation("Workflow for scan job {ScanJobId} cancelled by user.", scanJobId);
        return true;
    }

    public async Task<WorkflowStartResultDto?> ResumeByScanJobIdAsync(Guid scanJobId, CancellationToken cancellationToken)
    {
        var workflow = await dbContext.WorkflowInstances
            .FirstOrDefaultAsync(x => x.ScanJobId == scanJobId, cancellationToken);
        if (workflow is null)
        {
            return null;
        }

        SetWorkflowStatus(workflow, WorkflowStatus.RetryQueued);
        workflow.LastError = string.Empty;
        await dbContext.SaveChangesAsync(cancellationToken);

        await EnqueueAsync(workflow.Id, cancellationToken);
        return new WorkflowStartResultDto(workflow.Id, workflow.ScanJobId, workflow.CorrelationId, workflow.Status);
    }

    public ValueTask EnqueueAsync(Guid workflowId, CancellationToken cancellationToken) =>
        workflowQueue.EnqueueAsync(workflowId, cancellationToken);

    public async Task ProcessWorkflowAsync(Guid workflowId, CancellationToken cancellationToken)
    {
        var workflow = await dbContext.WorkflowInstances
            .Include(x => x.Steps)
            .FirstOrDefaultAsync(x => x.Id == workflowId, cancellationToken);

        if (workflow is null)
        {
            return;
        }

        if (!Enum.TryParse<WorkflowStatus>(workflow.Status, true, out var currentStatus))
        {
            logger.LogWarning("Skipping workflow {WorkflowId} due to unknown status '{Status}'", workflowId, workflow.Status);
            return;
        }

        if (currentStatus is not WorkflowStatus.Pending and not WorkflowStatus.RetryQueued)
        {
            logger.LogInformation(
                "Skipping workflow {WorkflowId} processing because status '{Status}' is not runnable.",
                workflowId,
                workflow.Status);
            return;
        }

        var scanJobsClient = httpClientFactory.CreateClient("scanjobs");

        try
        {
            SetWorkflowStatus(workflow, WorkflowStatus.Running);
            await dbContext.SaveChangesAsync(cancellationToken);

            await RunStepAsync(workflow, "Scanning", async () =>
            {
                var scannerClient = httpClientFactory.CreateClient("scanner");
                var locationsClient = httpClientFactory.CreateClient("locations");
                var location = await locationsClient.GetFromJsonAsync<StorageLocationDto>($"api/locations/{workflow.StorageLocationId}", cancellationToken)
                               ?? throw new InvalidOperationException("Location lookup failed.");

                await scanJobsClient.PutAsJsonAsync(
                    $"api/scanjobs/{workflow.ScanJobId}/status",
                    new UpdateScanJobStatusRequest(ScanJobStatus.Scanning, "Scanning", "Scanner execution started"),
                    cancellationToken);

                var executeRequest = new ScannerExecuteRequest(
                    workflow.StorageLocationId,
                    workflow.ScanJobId,
                    workflow.CorrelationId,
                    location.Name,
                    workflow.RootPath);

                var response = await scannerClient.PostAsJsonAsync("api/scanner/execute", executeRequest, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var rawBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    string errorMessage;
                    try
                    {
                        var failedExecution = System.Text.Json.JsonSerializer.Deserialize<ScannerExecutionDto>(
                            rawBody, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        errorMessage = !string.IsNullOrWhiteSpace(failedExecution?.Notes)
                            ? failedExecution.Notes
                            : string.IsNullOrWhiteSpace(rawBody)
                                ? $"HTTP {(int)response.StatusCode} ({response.ReasonPhrase})"
                                : rawBody.Length > 500 ? rawBody[..500] : rawBody;
                    }
                    catch
                    {
                        errorMessage = string.IsNullOrWhiteSpace(rawBody)
                            ? $"HTTP {(int)response.StatusCode} ({response.ReasonPhrase})"
                            : rawBody.Length > 500 ? rawBody[..500] : rawBody;
                    }

                    throw new InvalidOperationException(errorMessage);
                }
            }, cancellationToken);

            await RunStepAsync(workflow, "Hashing", async () =>
            {
                var hashingClient = httpClientFactory.CreateClient("hashing");

                await scanJobsClient.PutAsJsonAsync(
                    $"api/scanjobs/{workflow.ScanJobId}/status",
                    new UpdateScanJobStatusRequest(ScanJobStatus.Hashing, "Hashing", "Hashing execution started"),
                    cancellationToken);

                var response = await hashingClient.PostAsync(
                    $"api/hashing/run/{workflow.ScanJobId}?correlationId={workflow.CorrelationId}",
                    null,
                    cancellationToken);
                await EnsureSuccessOrThrowAsync(response, cancellationToken);
            }, cancellationToken);

            DuplicateRecalculationResultDto? duplicateResult = null;
            await RunStepAsync(workflow, "DetectingDuplicates", async () =>
            {
                var duplicatesClient = httpClientFactory.CreateClient("duplicates");

                await scanJobsClient.PutAsJsonAsync(
                    $"api/scanjobs/{workflow.ScanJobId}/status",
                    new UpdateScanJobStatusRequest(ScanJobStatus.DetectingDuplicates, "DetectingDuplicates", "Duplicate recalculation started"),
                    cancellationToken);

                var response = await duplicatesClient.PostAsync(
                    $"api/duplicates/recalculate/{workflow.ScanJobId}?correlationId={workflow.CorrelationId}",
                    null,
                    cancellationToken);
                await EnsureSuccessOrThrowAsync(response, cancellationToken);
                duplicateResult = await response.Content.ReadFromJsonAsync<DuplicateRecalculationResultDto>(cancellationToken);
            }, cancellationToken);

            await RunStepAsync(workflow, "RefreshingAnalytics", async () =>
            {
                var analyticsClient = httpClientFactory.CreateClient("analytics");

                await scanJobsClient.PutAsJsonAsync(
                    $"api/scanjobs/{workflow.ScanJobId}/status",
                    new UpdateScanJobStatusRequest(ScanJobStatus.RefreshingAnalytics, "RefreshingAnalytics", "Refreshing analytics projections"),
                    cancellationToken);

                using var response = await analyticsClient.GetAsync("api/analytics/dashboard", cancellationToken);
                await EnsureSuccessOrThrowAsync(response, cancellationToken);
            }, cancellationToken);

            SetWorkflowStatus(workflow, WorkflowStatus.Completed);
            workflow.CompletedAtUtc = DateTimeOffset.UtcNow;
            workflow.LastError = string.Empty;

            var currentJob = await scanJobsClient.GetFromJsonAsync<ScanJobDto>(
                $"api/scanjobs/{workflow.ScanJobId}",
                cancellationToken);

            var duplicateGroups = Math.Max(currentJob?.DuplicateGroupsFound ?? 0, duplicateResult?.DuplicateGroups ?? 0);

            await scanJobsClient.PutAsJsonAsync(
                $"api/scanjobs/{workflow.ScanJobId}/progress",
                new UpdateScanJobProgressRequest(
                    FilesScanned: currentJob?.FilesScanned ?? 0,
                    FilesHashed: currentJob?.FilesHashed ?? 0,
                    ProgressPercent: 100,
                    WarningCount: currentJob?.WarningCount ?? 0,
                    ErrorCount: currentJob?.ErrorCount ?? 0,
                    DuplicateGroupsFound: duplicateGroups,
                    Notes: "Workflow completed"),
                cancellationToken);

            await scanJobsClient.PutAsJsonAsync(
                $"api/scanjobs/{workflow.ScanJobId}/status",
                new UpdateScanJobStatusRequest(ScanJobStatus.Completed, "Completed", "Workflow completed", true),
                cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Workflow {WorkflowId} failed", workflowId);

            SetWorkflowStatus(workflow, WorkflowStatus.Failed);
            workflow.CompletedAtUtc = DateTimeOffset.UtcNow;
            workflow.LastError = ex.Message;

            dbContext.WorkflowErrors.Add(new WorkflowError
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = workflow.Id,
                StepName = "Workflow",
                Message = ex.Message,
                OccurredAtUtc = DateTimeOffset.UtcNow
            });

            await scanJobsClient.PutAsJsonAsync(
                $"api/scanjobs/{workflow.ScanJobId}/status",
                new UpdateScanJobStatusRequest(ScanJobStatus.Failed, "Failed", ex.Message, true),
                cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw new InvalidOperationException(await ReadFailureMessageAsync(response, cancellationToken));
    }

    private static async Task<string> ReadFailureMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var trimmedBody = body.Trim();

        if (!string.IsNullOrWhiteSpace(trimmedBody))
        {
            return trimmedBody.Length > 500 ? trimmedBody[..500] : trimmedBody;
        }

        return $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}).";
    }

    private static void SetWorkflowStatus(WorkflowInstance workflow, WorkflowStatus nextStatus)
    {
        if (!Enum.TryParse<WorkflowStatus>(workflow.Status, true, out var currentStatus))
        {
            throw new InvalidOperationException($"Unknown workflow status '{workflow.Status}'.");
        }

        if (currentStatus == nextStatus)
        {
            workflow.Status = nextStatus.ToString();
            return;
        }

        if (!AllowedTransitions.TryGetValue(currentStatus, out var allowed) || !allowed.Contains(nextStatus))
        {
            throw new InvalidOperationException(
                $"Invalid workflow status transition from '{currentStatus}' to '{nextStatus}'.");
        }

        workflow.Status = nextStatus.ToString();
    }

    private async Task RunStepAsync(
        WorkflowInstance workflow,
        string stepName,
        Func<Task> action,
        CancellationToken cancellationToken)
    {
        WorkflowStepStateContract.EnsureKnownStepName(stepName);

        var step = new WorkflowStepExecution
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = workflow.Id,
            StepName = stepName,
            Status = WorkflowStepStatus.Running.ToString(),
            StartedAtUtc = DateTimeOffset.UtcNow,
            Notes = "Started"
        };

        dbContext.WorkflowStepExecutions.Add(step);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await action();
            SetStepStatus(step, WorkflowStepStatus.Completed);
            step.Notes = "Completed";
            step.CompletedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            SetStepStatus(step, WorkflowStepStatus.Failed);
            step.Notes = ex.Message;
            step.CompletedAtUtc = DateTimeOffset.UtcNow;

            dbContext.WorkflowErrors.Add(new WorkflowError
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = workflow.Id,
                StepName = stepName,
                Message = ex.Message,
                OccurredAtUtc = DateTimeOffset.UtcNow
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private static void SetStepStatus(WorkflowStepExecution step, WorkflowStepStatus nextStatus)
    {
        WorkflowStepStateContract.EnsureKnownStepName(step.StepName);
        var currentStatus = WorkflowStepStateContract.ParseStatus(step.Status);
        WorkflowStepStateContract.EnsureTransitionAllowed(currentStatus, nextStatus);
        step.Status = nextStatus.ToString();
    }

    private static WorkflowInstanceDto Map(WorkflowInstance workflow)
    {
        var steps = workflow.Steps
            .OrderBy(x => x.StartedAtUtc)
            .Select(x => new WorkflowStepExecutionDto(x.StepName, x.Status, x.StartedAtUtc, x.CompletedAtUtc, x.Notes))
            .ToList();

        return new WorkflowInstanceDto(
            workflow.Id,
            workflow.StorageLocationId,
            workflow.ScanJobId,
            workflow.CorrelationId,
            workflow.RootPath,
            workflow.Status,
            workflow.CreatedAtUtc,
            workflow.CompletedAtUtc,
            workflow.RetryCount,
            workflow.LastError,
            steps);
    }
}
