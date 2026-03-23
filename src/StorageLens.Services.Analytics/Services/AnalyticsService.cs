using StorageLens.Services.Analytics.Clients;
using StorageLens.Services.Analytics.Interfaces;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Analytics.Services;

public class AnalyticsService(
    IHttpClientFactory httpClientFactory,
    ILogger<AnalyticsService> logger) : IAnalyticsService
{
    public async Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var locationsClient = new UpstreamApiClient(httpClientFactory.CreateClient("locations"));
        var scanJobsClient = new UpstreamApiClient(httpClientFactory.CreateClient("scanjobs"));
        var filesClient = new UpstreamApiClient(httpClientFactory.CreateClient("fileinventory"));
        var duplicatesClient = new UpstreamApiClient(httpClientFactory.CreateClient("duplicates"));

        var locationsTask = SafeFetchAsync(
            "locations",
            locationsClient.GetLocationsAsync,
            Array.Empty<StorageLocationDto>(),
            cancellationToken);

        var jobsTask = SafeFetchAsync(
            "scanjobs",
            scanJobsClient.GetRecentScanJobsAsync,
            Array.Empty<ScanJobDto>(),
            cancellationToken);

        var filesTask = SafeFetchAsync(
            "fileinventory",
            filesClient.GetFilesAsync,
            Array.Empty<FileRecordDto>(),
            cancellationToken);

        var duplicateGroupsTask = SafeFetchAsync(
            "duplicates-groups",
            duplicatesClient.GetDuplicateGroupsAsync,
            Array.Empty<DuplicateGroupDto>(),
            cancellationToken);

        var duplicateSummaryTask = SafeFetchAsync(
            "duplicates-summary",
            duplicatesClient.GetDuplicateSummaryAsync,
            new DuplicateSummaryDto(0, 0, 0, 0),
            cancellationToken);

        await Task.WhenAll(locationsTask, jobsTask, filesTask, duplicateGroupsTask, duplicateSummaryTask);

        var locations = await locationsTask;
        var jobs = await jobsTask;
        var files = await filesTask;
        var duplicateGroups = await duplicateGroupsTask;
        var duplicateSummary = await duplicateSummaryTask;

        var totalStorage = locations.Sum(x => x.TotalSizeBytes);
        var totalFiles = locations.Sum(x => x.FileCount);

        var kpis = new DashboardKpiDto(
            totalStorage,
            totalFiles,
            Math.Clamp((int)Math.Round(duplicateSummary.DuplicatePercent), 0, 100),
            duplicateSummary.WastedBytes);

        var locationSizeById = locations.ToDictionary(l => l.Id, l => l.TotalSizeBytes);

        var storageGrowth = jobs
            .GroupBy(x => x.StartTime.UtcDateTime.ToString("yyyy-MM-dd"))
            .OrderBy(x => x.Key)
            .TakeLast(8)
            .Select(x => new ChartPointDto(
                x.Key[5..],
                Math.Round(
                    x.Select(j => j.StorageLocationId)
                     .Distinct()
                     .Sum(id => locationSizeById.TryGetValue(id, out var size) ? size : 0L)
                     / (double)(1024L * 1024 * 1024 * 1024),
                    2)))
            .ToList();

        var fileTypeDistribution = files
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Extension) ? "(none)" : x.Extension.ToLowerInvariant())
            .Select(g => new ChartPointDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Value)
            .Take(6)
            .ToList();

        if (fileTypeDistribution.Count == 0)
        {
            fileTypeDistribution.Add(new ChartPointDto("(none)", 0));
        }

        var largestLocations = locations
            .OrderByDescending(x => x.TotalSizeBytes)
            .Take(5)
            .Select(x => new ChartPointDto(x.Name, Math.Round(x.TotalSizeBytes / (double)(1024L * 1024 * 1024 * 1024), 1)))
            .ToList();

        if (storageGrowth.Count == 0)
        {
            storageGrowth.Add(new ChartPointDto("N/A", 0));
        }

        logger.LogInformation("Dashboard aggregation complete with {LocationCount} locations", locations.Count);

        return new DashboardDto(
            kpis,
            storageGrowth,
            fileTypeDistribution,
            largestLocations,
            jobs,
            duplicateGroups.OrderByDescending(x => x.WastedBytes).Take(6).ToList(),
            locations);
    }

    public async Task<ReportsDto> GetReportsAsync(CancellationToken cancellationToken)
    {
        var filesClient = new UpstreamApiClient(httpClientFactory.CreateClient("fileinventory"));
        var duplicatesClient = new UpstreamApiClient(httpClientFactory.CreateClient("duplicates"));

        var filesTask = SafeFetchAsync(
            "fileinventory",
            filesClient.GetFilesAsync,
            Array.Empty<FileRecordDto>(),
            cancellationToken);

        var duplicatesTask = SafeFetchAsync(
            "duplicates-groups",
            duplicatesClient.GetDuplicateGroupsAsync,
            Array.Empty<DuplicateGroupDto>(),
            cancellationToken);

        await Task.WhenAll(filesTask, duplicatesTask);

        var files = await filesTask;
        var duplicates = await duplicatesTask;

        var oldest = files.OrderBy(x => x.LastModifiedDate).Take(10).ToList();
        var largest = files.OrderByDescending(x => x.SizeBytes).Take(10).ToList();

        var commonTypes = files.GroupBy(x => x.Extension)
            .Select(g => new ChartPointDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Value)
            .Take(8)
            .ToList();

        var storageConsumers = files.GroupBy(x => x.LocationName)
            .Select(g => new ChartPointDto(g.Key, Math.Round(g.Sum(v => v.SizeBytes) / (double)(1024L * 1024 * 1024), 1)))
            .OrderByDescending(x => x.Value)
            .Take(6)
            .ToList();

        var duplicateHeavy = duplicates
            .SelectMany(g => g.Locations)
            .GroupBy(x => x)
            .Select(g => new ChartPointDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Value)
            .Take(6)
            .ToList();

        return new ReportsDto(oldest, largest, commonTypes, storageConsumers, duplicateHeavy);
    }

    public async Task<IReadOnlyList<ChartPointDto>> GetChartsAsync(CancellationToken cancellationToken)
    {
        var dashboard = await GetDashboardAsync(cancellationToken);
        return dashboard.StorageGrowth;
    }

    private async Task<T> SafeFetchAsync<T>(
        string upstream,
        Func<CancellationToken, Task<T>> fetch,
        T fallback,
        CancellationToken cancellationToken)
    {
        try
        {
            return await fetch(cancellationToken);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Analytics upstream {Upstream} timed out. Using fallback payload.", upstream);
            return fallback;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Analytics upstream {Upstream} failed. Using fallback payload.", upstream);
            return fallback;
        }
    }
}
