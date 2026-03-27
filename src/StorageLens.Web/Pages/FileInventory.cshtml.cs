using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Net;
using StorageLens.Shared.Contracts;
using StorageLens.Web.Clients;
using StorageLens.Web.Services;

namespace StorageLens.Web.Pages;

public class FileInventoryModel(
    FileInventoryApiClient fileInventoryApiClient,
    LocationsApiClient locationsApiClient,
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
    public IReadOnlyList<FileInventoryLocationSnapshotDto> LocationSnapshots { get; private set; } = [];

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
        var locations = await locationsApiClient.GetLocationsAsync(cancellationToken) ?? [];
        var localTotals = BuildLocalTotals(result);
        var globalTotals = await GetGlobalTotalsAsync(localTotals, cancellationToken);
        GlobalTotalFiles = globalTotals.TotalFiles;
        GlobalTotalBytes = globalTotals.TotalBytes;
        GlobalDuplicateFiles = globalTotals.DuplicateFiles;
        LocationSnapshots = BuildLocationSnapshots(result, locations);
    }

    public async Task<IActionResult> OnGetSnapshotAsync(CancellationToken cancellationToken)
    {
        var result = await fileInventoryApiClient.GetFilesAsync(Search, Extension, PageNumber, PageSize, cancellationToken);
        if (result is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "File Inventory is currently unavailable." });
        }

        var locations = await locationsApiClient.GetLocationsAsync(cancellationToken) ?? [];

        var localTotals = BuildLocalTotals(result);
        var globalTotals = await GetGlobalTotalsAsync(localTotals, cancellationToken);
        var snapshot = BuildSnapshot(result, globalTotals, locations);
        return new JsonResult(snapshot);
    }

    public async Task<IActionResult> OnGetFileContentAsync(Guid id, bool download = false, CancellationToken cancellationToken = default)
    {
        var response = await fileInventoryApiClient.GetFileContentAsync(id, download, cancellationToken);
        if (response is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "File content service unavailable." });
        }

        HttpContext.Response.RegisterForDispose(response);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return NotFound();
        }

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var rawFileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName;
        var fileName = rawFileName?.Trim('"');

        Response.Headers["Cache-Control"] = "no-store";
        var result = new FileStreamResult(stream, contentType)
        {
            EnableRangeProcessing = true
        };

        if (download && !string.IsNullOrWhiteSpace(fileName))
        {
            result.FileDownloadName = fileName;
        }

        return result;
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

    private static FileInventorySnapshotDto BuildSnapshot(
        IReadOnlyList<FileRecordDto> files,
        GlobalInventoryTotals globalTotals,
        IReadOnlyList<StorageLocationDto> locations)
    {
        var uniqueExtensions = files
            .Select(file => file.Extension)
            .Where(extension => !string.IsNullOrWhiteSpace(extension))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var latestModified = files.Any() ? files.Max(file => file.LastModifiedDate) : (DateTimeOffset?)null;
        var pendingHashCount = files.Count(file => string.Equals(file.HashStatus, "Pending", StringComparison.OrdinalIgnoreCase) || string.Equals(file.HashStatus, "InProgress", StringComparison.OrdinalIgnoreCase));
        var locationSnapshots = BuildLocationSnapshots(files, locations);

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

    private static IReadOnlyList<FileInventoryLocationSnapshotDto> BuildLocationSnapshots(
        IReadOnlyList<FileRecordDto> files,
        IReadOnlyList<StorageLocationDto> locations)
    {
        var groupedByLocation = files
            .GroupBy(file => string.IsNullOrWhiteSpace(file.LocationName) ? "Unassigned" : file.LocationName)
            .ToDictionary(group => group.Key, StringComparer.OrdinalIgnoreCase);

        var locationNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var location in locations)
        {
            if (!string.IsNullOrWhiteSpace(location.Name))
            {
                locationNames.Add(location.Name);
            }
        }

        foreach (var locationName in groupedByLocation.Keys)
        {
            locationNames.Add(locationName);
        }

        return locationNames
            .Select(locationName =>
            {
                groupedByLocation.TryGetValue(locationName, out var filesAtLocation);
                var filesForLocation = filesAtLocation?.ToList() ?? [];

                return new FileInventoryLocationSnapshotDto(
                    locationName,
                    filesForLocation.Count,
                    filesForLocation.Sum(file => file.SizeBytes),
                    filesForLocation.Count > 0 ? filesForLocation.Max(file => file.LastModifiedDate) : null,
                    filesForLocation.Count(file => file.HashStatus?.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true || file.IsDuplicate),
                    filesForLocation.Count(file => string.Equals(file.HashStatus, "Pending", StringComparison.OrdinalIgnoreCase) || string.Equals(file.HashStatus, "InProgress", StringComparison.OrdinalIgnoreCase)));
            })
            .OrderByDescending(location => location.FileCount)
            .ThenBy(location => location.Name)
            .ToList();
    }

    private sealed record LocalInventoryTotals(int TotalFiles, long TotalBytes, int DuplicateFiles);

    private sealed record GlobalInventoryTotals(int TotalFiles, long TotalBytes, int DuplicateFiles);

    public sealed record FileInventoryLocationSnapshotDto(
        string Name,
        int FileCount,
        long TotalBytes,
        DateTimeOffset? LatestModified,
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
