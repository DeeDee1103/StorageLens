using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StorageLens.Services.Locations.Interfaces;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Locations.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LocationsController(ILocationService locationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StorageLocationDto>>> GetAll(CancellationToken cancellationToken)
    {
        var items = await locationService.GetAllAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StorageLocationDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await locationService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<StorageLocationDto>> Create([FromBody] CreateStorageLocationRequest request, CancellationToken cancellationToken)
    {
        // Validation handled by FluentValidation (CreateStorageLocationRequestValidator).
        // Invalid requests are rejected with 400 + ProblemDetails before reaching here.
        var created = await locationService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStorageLocationRequest request, CancellationToken cancellationToken)
    {
        var updated = await locationService.UpdateAsync(id, request, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await locationService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
