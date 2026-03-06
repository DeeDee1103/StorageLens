using Microsoft.AspNetCore.Mvc;
using StorageLens.Services.FileInventory.Interfaces;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.FileInventory.Controllers;

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
}
