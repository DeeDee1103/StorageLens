using Microsoft.AspNetCore.Mvc;
using StorageLens.Services.ScanJobs.Interfaces;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.ScanJobs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScanJobsController(IScanJobsService scanJobsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ScanJobDto>>> GetAll(CancellationToken cancellationToken)
    {
        var items = await scanJobsService.GetAllAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("recent")]
    public async Task<ActionResult<IReadOnlyList<ScanJobDto>>> GetRecent(CancellationToken cancellationToken)
    {
        var items = await scanJobsService.GetRecentAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ScanJobDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await scanJobsService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ScanJobDto>> Create([FromBody] CreateScanJobRequest request, CancellationToken cancellationToken)
    {
        var created = await scanJobsService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
