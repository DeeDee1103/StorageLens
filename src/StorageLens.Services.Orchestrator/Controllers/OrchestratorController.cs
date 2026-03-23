using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StorageLens.Services.Orchestrator.Interfaces;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Orchestrator.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrchestratorController(IWorkflowOrchestratorService workflowService) : ControllerBase
{
    [HttpPost("scans/start/{locationId:guid}")]
    public async Task<ActionResult<WorkflowStartResultDto>> Start(Guid locationId, CancellationToken cancellationToken)
    {
        var result = await workflowService.StartScanAsync(locationId, cancellationToken);
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
