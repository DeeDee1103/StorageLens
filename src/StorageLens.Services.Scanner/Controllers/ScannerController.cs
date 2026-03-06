using Microsoft.AspNetCore.Mvc;
using StorageLens.Services.Scanner.Interfaces;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Scanner.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScannerController(IScannerExecutionService scannerExecutionService) : ControllerBase
{
    [HttpPost("execute")]
    public async Task<ActionResult<ScannerExecutionDto>> Execute(
        [FromBody] ScannerExecuteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await scannerExecutionService.ExecuteAsync(request, cancellationToken);

        if (string.Equals(result.Status, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, result);
        }

        return Ok(result);
    }

    [HttpGet("executions/{id:guid}")]
    public async Task<ActionResult<ScannerExecutionDto>> GetExecution(Guid id, CancellationToken cancellationToken)
    {
        var item = await scannerExecutionService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }
}
