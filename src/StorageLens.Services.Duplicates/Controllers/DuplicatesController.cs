using Microsoft.AspNetCore.Mvc;
using StorageLens.Services.Duplicates.Interfaces;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Duplicates.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DuplicatesController(IDuplicatesService duplicatesService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<DuplicateSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await duplicatesService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    [HttpGet("groups")]
    public async Task<ActionResult<IReadOnlyList<DuplicateGroupDto>>> GetGroups(CancellationToken cancellationToken)
    {
        var groups = await duplicatesService.GetGroupsAsync(cancellationToken);
        return Ok(groups);
    }

    [HttpGet("groups/{id:guid}")]
    public async Task<ActionResult<DuplicateGroupDto>> GetGroupById(Guid id, CancellationToken cancellationToken)
    {
        var item = await duplicatesService.GetGroupByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("recalculate/{scanJobId:guid}")]
    public async Task<ActionResult<DuplicateRecalculationResultDto>> Recalculate(
        Guid scanJobId,
        [FromQuery] string correlationId,
        CancellationToken cancellationToken)
    {
        var result = await duplicatesService.RecalculateAsync(scanJobId, correlationId, cancellationToken);
        return Ok(result);
    }
}
