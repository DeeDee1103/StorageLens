using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using StorageLens.Services.Analytics.Services;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Analytics.Tests;

public class AnalyticsServiceResilienceTests
{
    [Fact]
    public async Task GetDashboardAsync_WhenAllUpstreamsFail_ReturnsFallbackDashboard()
    {
        var factory = new StubHttpClientFactory(
            new HttpClient(new RouteHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))));

        var sut = new AnalyticsService(factory, NullLogger<AnalyticsService>.Instance);

        var dashboard = await sut.GetDashboardAsync(CancellationToken.None);

        Assert.NotNull(dashboard);
        Assert.Equal(0, dashboard.Kpis.TotalStorageBytes);
        Assert.Equal(0, dashboard.Kpis.TotalFiles);
        Assert.Equal(0, dashboard.Kpis.DuplicatePercent);
        Assert.Equal(0, dashboard.Kpis.PotentialSavingsBytes);
        Assert.Single(dashboard.StorageGrowth);
        Assert.Equal("N/A", dashboard.StorageGrowth[0].Label);
        Assert.Single(dashboard.FileTypeDistribution);
        Assert.Equal("(none)", dashboard.FileTypeDistribution[0].Label);
        Assert.Empty(dashboard.Locations);
    }

    [Fact]
    public async Task GetReportsAsync_WhenUpstreamsFail_ReturnsEmptyReports()
    {
        var factory = new StubHttpClientFactory(
            new HttpClient(new RouteHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))));

        var sut = new AnalyticsService(factory, NullLogger<AnalyticsService>.Instance);

        var reports = await sut.GetReportsAsync(CancellationToken.None);

        Assert.NotNull(reports);
        Assert.Empty(reports.OldestFiles);
        Assert.Empty(reports.LargestFiles);
        Assert.Empty(reports.MostCommonFileTypes);
        Assert.Empty(reports.BiggestStorageConsumers);
        Assert.Empty(reports.DuplicateHeavyLocations);
    }

    [Fact]
    public async Task GetDashboardAsync_WhenOneUpstreamFails_StillReturnsDataFromHealthyUpstreams()
    {
        var locationId = Guid.NewGuid();
        var locations = new List<StorageLocationDto>
        {
            new(locationId, "HQ", "C:/Data", "Nas", "Online", DateTimeOffset.UtcNow, 1024, 3, DateTimeOffset.UtcNow)
        };

        var jobs = new List<ScanJobDto>
        {
            new(Guid.NewGuid(), locationId, "HQ", "Completed", DateTimeOffset.UtcNow.AddMinutes(-2), DateTimeOffset.UtcNow, 3, 100, "ok")
        };

        var files = new List<FileRecordDto>
        {
            new(Guid.NewGuid(), locationId, "HQ", "a.txt", "C:/Data/a.txt", ".txt", 100, DateTimeOffset.UtcNow, "hash", false, null, "Completed")
        };

        var handler = new RouteHandler(request =>
        {
            var path = request.RequestUri?.PathAndQuery ?? string.Empty;

            if (path.Contains("api/locations", StringComparison.OrdinalIgnoreCase))
                return Json(locations);
            if (path.Contains("api/scanjobs/recent", StringComparison.OrdinalIgnoreCase))
                return Json(jobs);
            if (path.Contains("api/files", StringComparison.OrdinalIgnoreCase))
                return Json(files);
            if (path.Contains("api/duplicates/summary", StringComparison.OrdinalIgnoreCase))
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            if (path.Contains("api/duplicates/groups", StringComparison.OrdinalIgnoreCase))
                return Json(Array.Empty<DuplicateGroupDto>());

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var sut = new AnalyticsService(new StubHttpClientFactory(new HttpClient(handler)), NullLogger<AnalyticsService>.Instance);

        var dashboard = await sut.GetDashboardAsync(CancellationToken.None);

        Assert.Equal(1024, dashboard.Kpis.TotalStorageBytes);
        Assert.Equal(3, dashboard.Kpis.TotalFiles);
        Assert.Equal(0, dashboard.Kpis.DuplicatePercent);
        Assert.Equal(0, dashboard.Kpis.PotentialSavingsBytes);
        Assert.Single(dashboard.Locations);
    }

    private static HttpResponseMessage Json<T>(T payload)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class RouteHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(handler(request));
        }
    }
}
