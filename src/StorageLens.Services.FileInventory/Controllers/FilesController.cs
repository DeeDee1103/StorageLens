using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StorageLens.Services.FileInventory.Interfaces;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.FileInventory.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FilesController(IFileInventoryService fileInventoryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FileRecordDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? extension,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var items = await fileInventoryService.GetAllAsync(search, extension, page, pageSize, cancellationToken);
        return Ok(items);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<FileRecordDto>>> Search(
        [FromQuery] string? query,
        [FromQuery] string? extension,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var items = await fileInventoryService.GetAllAsync(query, extension, page, pageSize, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FileRecordDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await fileInventoryService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("by-location/{locationId:guid}")]
    public async Task<ActionResult<IReadOnlyList<FileRecordDto>>> GetByLocation(Guid locationId, CancellationToken cancellationToken)
    {
        var items = await fileInventoryService.GetByLocationAsync(locationId, cancellationToken);
        return Ok(items);
    }

    [HttpGet("stats/by-location")]
    public async Task<ActionResult<IReadOnlyList<LocationStorageStatsDto>>> GetStorageStatsByLocation(CancellationToken cancellationToken)
    {
        var items = await fileInventoryService.GetLocationStorageStatsAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost("batch")]
    public async Task<ActionResult<FileBatchIngestResultDto>> IngestBatch(
        [FromBody] FileMetadataBatchIngestRequest request,
        CancellationToken cancellationToken)
    {
        var result = await fileInventoryService.UpsertBatchAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("pending-hash")]
    public async Task<ActionResult<IReadOnlyList<PendingHashFileDto>>> GetPendingHash(
        [FromQuery] Guid? scanJobId,
        [FromQuery] int take = 500,
        CancellationToken cancellationToken = default)
    {
        var items = await fileInventoryService.GetPendingHashFilesAsync(scanJobId, take, cancellationToken);
        return Ok(items);
    }

    [HttpPost("{id:guid}/hash-result")]
    public async Task<IActionResult> SetHashResult(
        Guid id,
        [FromBody] FileHashResultRequest request,
        CancellationToken cancellationToken)
    {
        var ok = await fileInventoryService.UpdateHashResultAsync(id, request, cancellationToken);
        return ok ? Ok() : NotFound();
    }

    [HttpPost("mark-duplicates/{scanJobId:guid}")]
    public async Task<ActionResult<int>> MarkDuplicates(
        Guid scanJobId,
        [FromBody] MarkDuplicateHashesRequest request,
        CancellationToken cancellationToken)
    {
        var changed = await fileInventoryService.MarkDuplicateFlagsByHashAsync(
            scanJobId,
            request.CorrelationId,
            request.Hashes.ToHashSet(StringComparer.OrdinalIgnoreCase),
            cancellationToken);

        return Ok(changed);
    }
}
