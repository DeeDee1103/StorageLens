using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using StorageLens.Services.FileInventory.Interfaces;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.FileInventory.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FilesController(IFileInventoryService fileInventoryService) : ControllerBase
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

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
        [FromQuery] bool includeFailed = true,
        CancellationToken cancellationToken = default)
    {
        var items = await fileInventoryService.GetPendingHashFilesAsync(scanJobId, take, includeFailed, cancellationToken);
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

    [HttpDelete("by-scan-job/{scanJobId:guid}")]
    public async Task<ActionResult<int>> DeleteByScanJob(
        Guid scanJobId,
        [FromQuery] string correlationId,
        CancellationToken cancellationToken)
    {
        var deleted = await fileInventoryService.DeleteByScanJobAsync(scanJobId, correlationId, cancellationToken);
        return Ok(deleted);
    }

    [HttpGet("{id:guid}/content")]
    public async Task<IActionResult> GetContent(Guid id, [FromQuery] bool download = false, CancellationToken cancellationToken = default)
    {
        var item = await fileInventoryService.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        var resolvedPath = ResolveMountedPath(item.FilePath);
        if (string.IsNullOrWhiteSpace(resolvedPath) || !System.IO.File.Exists(resolvedPath))
        {
            return NotFound(new { message = "File not reachable from service container.", file = item.FileName });
        }

        var fileName = string.IsNullOrWhiteSpace(item.FileName) ? Path.GetFileName(resolvedPath) : item.FileName;
        var contentType = ResolveContentType(fileName);
        var stream = new FileStream(
            resolvedPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            bufferSize: 64 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        if (download)
        {
            return File(stream, contentType, fileName, enableRangeProcessing: true);
        }

        return File(stream, contentType, enableRangeProcessing: true);
    }

    private static string ResolveContentType(string fileName)
    {
        if (ContentTypeProvider.TryGetContentType(fileName, out var contentType))
        {
            return contentType;
        }

        return "application/octet-stream";
    }

    private static string ResolveMountedPath(string rawPath)
    {
        var path = (rawPath ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        if (path.StartsWith("/host/drives/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/host/unc/", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        if (path.StartsWith("\\\\", StringComparison.Ordinal))
        {
            var uncPath = path.TrimStart('\\').Replace('\\', '/');
            return $"/host/unc/{uncPath}";
        }

        if (path.Length >= 3 && char.IsLetter(path[0]) && path[1] == ':' && (path[2] == '\\' || path[2] == '/'))
        {
            var drive = char.ToLowerInvariant(path[0]);
            var remainder = path[3..].Replace('\\', '/');
            return string.IsNullOrWhiteSpace(remainder)
                ? $"/host/drives/{drive}"
                : $"/host/drives/{drive}/{remainder}";
        }

        if (path.StartsWith("/", StringComparison.Ordinal))
        {
            return path;
        }

        return path;
    }
}
