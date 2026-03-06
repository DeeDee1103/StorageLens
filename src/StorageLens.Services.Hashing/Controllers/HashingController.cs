using Microsoft.AspNetCore.Mvc;
using StorageLens.Services.Hashing.Interfaces;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Hashing.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HashingController(IHashingService hashingService) : ControllerBase
{
    [HttpPost("run/{scanJobId:guid}")]
    public async Task<ActionResult<HashingRunResultDto>> Run(
        Guid scanJobId,
        [FromQuery] string correlationId,
        CancellationToken cancellationToken)
    {
        var result = await hashingService.RunAsync(scanJobId, correlationId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("executions/{id:guid}")]
    public async Task<ActionResult<HashingRunResultDto>> GetExecution(Guid id, CancellationToken cancellationToken)
    {
        var execution = await hashingService.GetExecutionAsync(id, cancellationToken);
        return execution is null ? NotFound() : Ok(execution);
    }
}
