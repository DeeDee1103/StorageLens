using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StorageLens.Services.Orchestrator.Interfaces;
using StorageLens.Services.Orchestrator.Sagas;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Orchestrator.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrchestratorController(
    IWorkflowOrchestratorService workflowService,
    IWorkflowCompensator compensator) : ControllerBase
{
    [HttpPost("scans/start/{locationId:guid}")]
    public async Task<ActionResult<WorkflowStartResultDto>> Start(
        Guid locationId,
        [FromBody] StartScanRequest? request,
        CancellationToken cancellationToken)
    {
        if (locationId == Guid.Empty)
        {
            return BadRequest("Location id cannot be empty.");
        }

        var result = await workflowService.StartScanAsync(locationId, request?.ScheduledStartUtc, cancellationToken);
        return Accepted(result);
    }

    [HttpGet("workflows/{id:guid}")]
    public async Task<ActionResult<WorkflowInstanceDto>> GetWorkflow(Guid id, CancellationToken cancellationToken)
    {
        var workflow = await workflowService.GetWorkflowAsync(id, cancellationToken);
        return workflow is null ? NotFound() : Ok(workflow);
    }

    [HttpPost("workflows/{id:guid}/resume")]
    public async Task<ActionResult<WorkflowStartResultDto>> Resume(Guid id, CancellationToken cancellationToken)
    {
        var result = await workflowService.ResumeAsync(id, cancellationToken);
        return result is null ? NotFound() : Accepted(result);
    }

    [HttpPost("workflows/{id:guid}/retry")]
    public async Task<ActionResult<WorkflowStartResultDto>> Retry(Guid id, CancellationToken cancellationToken)
    {
        var result = await workflowService.RetryAsync(id, cancellationToken);
        return result is null ? NotFound() : Accepted(result);
    }

    [HttpPost("workflows/{id:guid}/compensate")]
    public async Task<IActionResult> Compensate(Guid id, CancellationToken cancellationToken)
    {
        var workflow = await workflowService.GetWorkflowAsync(id, cancellationToken);
        if (workflow is null)
        {
            return NotFound();
        }

        var instance = new Models.WorkflowInstance
        {
            Id = workflow.Id,
            ScanJobId = workflow.ScanJobId,
            CorrelationId = workflow.CorrelationId,
            StorageLocationId = workflow.StorageLocationId,
            RootPath = workflow.RootPath,
            Status = workflow.Status,
            CreatedAtUtc = workflow.CreatedAtUtc,
            Steps = workflow.Steps
                .Select(s => new Models.WorkflowStepExecution
                {
                    Id = Guid.NewGuid(),
                    WorkflowInstanceId = workflow.Id,
                    StepName = s.StepName,
                    Status = s.Status,
                    StartedAtUtc = s.StartedAtUtc,
                    CompletedAtUtc = s.CompletedAtUtc,
                    Notes = s.Notes
                })
                .ToList()
        };

        await compensator.CompensateAsync(instance, cancellationToken);
        return Accepted();
    }

    [HttpPost("scans/{scanJobId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid scanJobId, CancellationToken cancellationToken)
    {
        var cancelled = await workflowService.CancelByScanJobIdAsync(scanJobId, cancellationToken);
        return cancelled ? Accepted() : NotFound();
    }

    [HttpPost("scans/{scanJobId:guid}/resume")]
    public async Task<ActionResult<WorkflowStartResultDto>> ResumeByScanJob(Guid scanJobId, CancellationToken cancellationToken)
    {
        var result = await workflowService.ResumeByScanJobIdAsync(scanJobId, cancellationToken);
        return result is null ? NotFound() : Accepted(result);
    }
}
