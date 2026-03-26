using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using StorageLens.Shared.Contracts;
using StorageLens.Web.Clients;
using StorageLens.Web.Services;

namespace StorageLens.Web.Pages;

public class FileInventoryModel(
    FileInventoryApiClient fileInventoryApiClient,
    AnalyticsApiClient analyticsApiClient,
    DuplicatesApiClient duplicatesApiClient,
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
    public int GlobalTotalFiles { get; private set; }
    public long GlobalTotalBytes { get; private set; }
    public int GlobalDuplicateFiles { get; private set; }

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
        var localTotals = BuildLocalTotals(result);
        var globalTotals = await GetGlobalTotalsAsync(localTotals, cancellationToken);
        GlobalTotalFiles = globalTotals.TotalFiles;
        GlobalTotalBytes = globalTotals.TotalBytes;
        GlobalDuplicateFiles = globalTotals.DuplicateFiles;
    }

    public async Task<IActionResult> OnGetSnapshotAsync(CancellationToken cancellationToken)
    {
        var result = await fileInventoryApiClient.GetFilesAsync(Search, Extension, PageNumber, PageSize, cancellationToken);
        if (result is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "File Inventory is currently unavailable." });
        }

        var localTotals = BuildLocalTotals(result);
        var globalTotals = await GetGlobalTotalsAsync(localTotals, cancellationToken);
        var snapshot = BuildSnapshot(result, globalTotals);
        return new JsonResult(snapshot);
    }

    private async Task<GlobalInventoryTotals> GetGlobalTotalsAsync(LocalInventoryTotals localTotals, CancellationToken cancellationToken)
    {
        var dashboardTask = analyticsApiClient.GetDashboardAsync(cancellationToken);
        var duplicateSummaryTask = duplicatesApiClient.GetSummaryAsync(cancellationToken);

        await Task.WhenAll(dashboardTask, duplicateSummaryTask);

        var dashboard = dashboardTask.Result;
        var duplicateSummary = duplicateSummaryTask.Result;
        var totalFiles = dashboard is null
            ? localTotals.TotalFiles
            : (int)Math.Min(int.MaxValue, dashboard.Kpis.TotalFiles);
        var totalBytes = dashboard?.Kpis.TotalStorageBytes ?? localTotals.TotalBytes;

        return new GlobalInventoryTotals(
            totalFiles,
            totalBytes,
            duplicateSummary?.DuplicateFiles ?? localTotals.DuplicateFiles);
    }

    private static LocalInventoryTotals BuildLocalTotals(IReadOnlyList<FileRecordDto> files)
    {
        var totalFiles = files.Count;
        var totalBytes = files.Sum(file => file.SizeBytes);
        var duplicateFiles = files.Count(file => file.HashStatus?.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true || file.IsDuplicate);
        return new LocalInventoryTotals(totalFiles, totalBytes, duplicateFiles);
    }

    private static FileInventorySnapshotDto BuildSnapshot(IReadOnlyList<FileRecordDto> files, GlobalInventoryTotals globalTotals)
    {
        var uniqueExtensions = files
            .Select(file => file.Extension)
            .Where(extension => !string.IsNullOrWhiteSpace(extension))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
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
            globalTotals.TotalFiles,
            globalTotals.TotalBytes,
            uniqueExtensions,
            globalTotals.DuplicateFiles,
            latestModified,
            pendingHashCount,
            pendingHashCount > 0,
            locationSnapshots,
            DateTimeOffset.UtcNow);
    }

    private sealed record LocalInventoryTotals(int TotalFiles, long TotalBytes, int DuplicateFiles);

    private sealed record GlobalInventoryTotals(int TotalFiles, long TotalBytes, int DuplicateFiles);

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
