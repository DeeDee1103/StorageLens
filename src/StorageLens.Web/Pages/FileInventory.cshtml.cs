using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using StorageLens.Shared.Contracts;
using StorageLens.Web.Clients;
using StorageLens.Web.Services;

namespace StorageLens.Web.Pages;

public class FileInventoryModel(
    FileInventoryApiClient fileInventoryApiClient,
    IOptions<AppearanceOptions> appearanceOptions) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Extension { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; } = 25;
    public IReadOnlyList<FileRecordDto> Files { get; private set; } = [];
    public int LiveRefreshSeconds { get; } = Math.Max(0, appearanceOptions.Value.ScanAutoRefreshSeconds);

    public bool ServiceUnavailable { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "File Inventory";
        ViewData["Nav"] = "FileInventory";
        var result = await fileInventoryApiClient.GetFilesAsync(Search, Extension, PageNumber, PageSize, cancellationToken);
        if (result is null)
        {
            ServiceUnavailable = true;
            return;
        }

        Files = result;
    }

    public async Task<IActionResult> OnGetSnapshotAsync(CancellationToken cancellationToken)
    {
        var result = await fileInventoryApiClient.GetFilesAsync(Search, Extension, PageNumber, PageSize, cancellationToken);
        if (result is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "File Inventory is currently unavailable." });
        }

        var snapshot = BuildSnapshot(result);
        return new JsonResult(snapshot);
    }

    private static FileInventorySnapshotDto BuildSnapshot(IReadOnlyList<FileRecordDto> files)
    {
        var totalFiles = files.Count;
        var totalBytes = files.Sum(file => file.SizeBytes);
        var uniqueExtensions = files
            .Select(file => file.Extension)
            .Where(extension => !string.IsNullOrWhiteSpace(extension))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var duplicateCount = files.Count(file => file.HashStatus?.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true || file.IsDuplicate);
        var latestModified = files.Any() ? files.Max(file => file.LastModifiedDate) : (DateTimeOffset?)null;
        var pendingHashCount = files.Count(file => string.Equals(file.HashStatus, "Pending", StringComparison.OrdinalIgnoreCase) || string.Equals(file.HashStatus, "InProgress", StringComparison.OrdinalIgnoreCase));
        var locationSnapshots = files
            .GroupBy(file => string.IsNullOrWhiteSpace(file.LocationName) ? "Unassigned" : file.LocationName)
            .Select(group => new FileInventoryLocationSnapshotDto(
                group.Key,
                group.Count(),
                group.Sum(file => file.SizeBytes),
                group.Max(file => file.LastModifiedDate),
                group.Count(file => file.HashStatus?.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true || file.IsDuplicate),
                group.Count(file => string.Equals(file.HashStatus, "Pending", StringComparison.OrdinalIgnoreCase) || string.Equals(file.HashStatus, "InProgress", StringComparison.OrdinalIgnoreCase))))
            .OrderByDescending(location => location.FileCount)
            .ThenBy(location => location.Name)
            .ToList();

        return new FileInventorySnapshotDto(
            files,
            totalFiles,
            totalBytes,
            uniqueExtensions,
            duplicateCount,
            latestModified,
            pendingHashCount,
            pendingHashCount > 0,
            locationSnapshots,
            DateTimeOffset.UtcNow);
    }

    public sealed record FileInventoryLocationSnapshotDto(
        string Name,
        int FileCount,
        long TotalBytes,
        DateTimeOffset LatestModified,
        int DuplicateCount,
        int HashPendingCount);

    public sealed record FileInventorySnapshotDto(
        IReadOnlyList<FileRecordDto> Files,
        int TotalFiles,
        long TotalBytes,
        int UniqueExtensions,
        int DuplicateCount,
        DateTimeOffset? LatestModified,
        int PendingHashCount,
        bool HasActiveWork,
        IReadOnlyList<FileInventoryLocationSnapshotDto> LocationSnapshots,
        DateTimeOffset GeneratedAtUtc);
}
