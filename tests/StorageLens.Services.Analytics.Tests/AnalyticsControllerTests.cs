using Microsoft.AspNetCore.Mvc;
using StorageLens.Services.Analytics.Controllers;
using StorageLens.Services.Analytics.Interfaces;
using StorageLens.Shared.Contracts;
using Xunit;

namespace StorageLens.Services.Analytics.Tests;

public class AnalyticsControllerTests
{
    // -----------------------------------------------------------------------
    // GetDashboard
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetDashboard_ReturnsOk_WithDashboardDto()
    {
        var expected = BuildDashboard();
        var stub = new StubAnalyticsService { Dashboard = expected };
        var controller = new AnalyticsController(stub);

        var result = await controller.GetDashboard(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<DashboardDto>(ok.Value);
        Assert.Equal(expected.Kpis.TotalStorageBytes, dto.Kpis.TotalStorageBytes);
        Assert.Equal(expected.Kpis.TotalFiles, dto.Kpis.TotalFiles);
        Assert.Equal(expected.Kpis.DuplicatePercent, dto.Kpis.DuplicatePercent);
        Assert.Equal(expected.Kpis.PotentialSavingsBytes, dto.Kpis.PotentialSavingsBytes);
        Assert.Equal(expected.StorageGrowth.Count, dto.StorageGrowth.Count);
        Assert.Equal(expected.RecentJobs.Count, dto.RecentJobs.Count);
        Assert.Equal(expected.Locations.Count, dto.Locations.Count);
    }

    [Fact]
    public async Task GetDashboard_PassesCancellationToken_ToService()
    {
        var stub = new StubAnalyticsService { Dashboard = BuildDashboard() };
        var controller = new AnalyticsController(stub);
        using var cts = new CancellationTokenSource();

        await controller.GetDashboard(cts.Token);

        Assert.Equal(cts.Token, stub.LastCancellationToken);
    }

    // -----------------------------------------------------------------------
    // GetReports
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetReports_ReturnsOk_WithReportsDto()
    {
        var expected = BuildReports();
        var stub = new StubAnalyticsService { Reports = expected };
        var controller = new AnalyticsController(stub);

        var result = await controller.GetReports(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ReportsDto>(ok.Value);
        Assert.Equal(expected.OldestFiles.Count, dto.OldestFiles.Count);
        Assert.Equal(expected.LargestFiles.Count, dto.LargestFiles.Count);
        Assert.Equal(expected.MostCommonFileTypes.Count, dto.MostCommonFileTypes.Count);
        Assert.Equal(expected.BiggestStorageConsumers.Count, dto.BiggestStorageConsumers.Count);
        Assert.Equal(expected.DuplicateHeavyLocations.Count, dto.DuplicateHeavyLocations.Count);
    }

    [Fact]
    public async Task GetReports_PassesCancellationToken_ToService()
    {
        var stub = new StubAnalyticsService { Reports = BuildReports() };
        var controller = new AnalyticsController(stub);
        using var cts = new CancellationTokenSource();

        await controller.GetReports(cts.Token);

        Assert.Equal(cts.Token, stub.LastCancellationToken);
    }

    // -----------------------------------------------------------------------
    // GetCharts
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetCharts_ReturnsOk_WithChartPointList()
    {
        var expected = new List<ChartPointDto>
        {
            new("01-01", 1.5),
            new("01-02", 2.3),
            new("01-03", 0.8)
        };
        var stub = new StubAnalyticsService { Charts = expected };
        var controller = new AnalyticsController(stub);

        var result = await controller.GetCharts(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IReadOnlyList<ChartPointDto>>(ok.Value);
        Assert.Equal(3, list.Count);
        Assert.Equal("01-01", list[0].Label);
        Assert.Equal(1.5, list[0].Value);
    }

    [Fact]
    public async Task GetCharts_ReturnsOk_WithEmptyList_WhenNoData()
    {
        var stub = new StubAnalyticsService { Charts = new List<ChartPointDto>() };
        var controller = new AnalyticsController(stub);

        var result = await controller.GetCharts(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IReadOnlyList<ChartPointDto>>(ok.Value);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetCharts_PassesCancellationToken_ToService()
    {
        var stub = new StubAnalyticsService { Charts = new List<ChartPointDto>() };
        var controller = new AnalyticsController(stub);
        using var cts = new CancellationTokenSource();

        await controller.GetCharts(cts.Token);

        Assert.Equal(cts.Token, stub.LastCancellationToken);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static DashboardDto BuildDashboard()
    {
        var kpis = new DashboardKpiDto(
            TotalStorageBytes: 10_000_000_000L,
            TotalFiles: 50_000L,
            DuplicatePercent: 12,
            PotentialSavingsBytes: 1_200_000_000L);

        var location = new StorageLocationDto(
            Guid.NewGuid(),
            "Primary NAS",
            @"\\nas\share",
            StorageLocationType.Nas.ToString(),
            LocationStatus.Online.ToString(),
            DateTimeOffset.UtcNow.AddDays(-1),
            10_000_000_000L,
            50_000L,
            DateTimeOffset.UtcNow.AddMonths(-6));

        var job = new ScanJobDto(
            Guid.NewGuid(),
            location.Id,
            location.Name,
            ScanJobStatus.Completed.ToString(),
            DateTimeOffset.UtcNow.AddHours(-2),
            DateTimeOffset.UtcNow.AddHours(-1),
            50_000L,
            100,
            "OK");

        var duplicateGroup = new DuplicateGroupDto(
            Guid.NewGuid(),
            "abc123",
            3,
            3_000_000L,
            2_000_000L,
            "report.pdf",
            new List<string> { "Primary NAS" });

        return new DashboardDto(
            kpis,
            StorageGrowth: new List<ChartPointDto> { new("03-01", 9.2), new("03-02", 9.5) },
            FileTypeDistribution: new List<ChartPointDto> { new(".pdf", 12000), new(".docx", 8000) },
            LargestLocations: new List<ChartPointDto> { new("Primary NAS", 9.3) },
            RecentJobs: new List<ScanJobDto> { job },
            TopDuplicateGroups: new List<DuplicateGroupDto> { duplicateGroup },
            Locations: new List<StorageLocationDto> { location });
    }

    private static ReportsDto BuildReports()
    {
        var locationId = Guid.NewGuid();
        var file = new FileRecordDto(
            Guid.NewGuid(),
            locationId,
            "Primary NAS",
            "old_report.pdf",
            @"\\nas\share\old_report.pdf",
            ".pdf",
            512_000L,
            DateTimeOffset.UtcNow.AddYears(-3),
            string.Empty,
            false,
            null,
            HashStatus.Completed.ToString());

        return new ReportsDto(
            OldestFiles: new List<FileRecordDto> { file },
            LargestFiles: new List<FileRecordDto> { file },
            MostCommonFileTypes: new List<ChartPointDto> { new(".pdf", 12000) },
            BiggestStorageConsumers: new List<ChartPointDto> { new("Primary NAS", 9.3) },
            DuplicateHeavyLocations: new List<ChartPointDto> { new("Primary NAS", 5) });
    }

    // -----------------------------------------------------------------------
    // Stub
    // -----------------------------------------------------------------------

    private sealed class StubAnalyticsService : IAnalyticsService
    {
        public DashboardDto Dashboard { get; set; } = null!;
        public ReportsDto Reports { get; set; } = null!;
        public IReadOnlyList<ChartPointDto> Charts { get; set; } = null!;
        public CancellationToken LastCancellationToken { get; private set; }

        public Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken)
        {
            LastCancellationToken = cancellationToken;
            return Task.FromResult(Dashboard);
        }

        public Task<ReportsDto> GetReportsAsync(CancellationToken cancellationToken)
        {
            LastCancellationToken = cancellationToken;
            return Task.FromResult(Reports);
        }

        public Task<IReadOnlyList<ChartPointDto>> GetChartsAsync(CancellationToken cancellationToken)
        {
            LastCancellationToken = cancellationToken;
            return Task.FromResult(Charts);
        }
    }
}
