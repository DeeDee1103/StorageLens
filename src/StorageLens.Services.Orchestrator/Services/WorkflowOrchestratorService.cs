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

        workflow.Status = WorkflowStatus.RetryQueued.ToString();
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
        workflow.Status = WorkflowStatus.RetryQueued.ToString();
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

        var scanJobsClient = httpClientFactory.CreateClient("scanjobs");

        try
        {
            workflow.Status = WorkflowStatus.Running.ToString();
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
                response.EnsureSuccessStatusCode();
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
                response.EnsureSuccessStatusCode();
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
                response.EnsureSuccessStatusCode();
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
                response.EnsureSuccessStatusCode();
            }, cancellationToken);

            workflow.Status = WorkflowStatus.Completed.ToString();
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

            workflow.Status = WorkflowStatus.Failed.ToString();
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

    private async Task RunStepAsync(
        WorkflowInstance workflow,
        string stepName,
        Func<Task> action,
        CancellationToken cancellationToken)
    {
        var step = new WorkflowStepExecution
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = workflow.Id,
            StepName = stepName,
            Status = "Running",
            StartedAtUtc = DateTimeOffset.UtcNow,
            Notes = "Started"
        };

        dbContext.WorkflowStepExecutions.Add(step);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await action();
            step.Status = "Completed";
            step.Notes = "Completed";
            step.CompletedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            step.Status = "Failed";
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
