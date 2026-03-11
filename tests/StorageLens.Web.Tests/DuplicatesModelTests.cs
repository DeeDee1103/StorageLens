using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging.Abstractions;
using StorageLens.Shared.Contracts;
using StorageLens.Web.Clients;
using StorageLens.Web.Pages;
using StorageLens.Web.Services;
using Xunit;

namespace StorageLens.Web.Tests;

public class DuplicatesModelTests
{
    [Fact]
    public async Task OnGetAsync_WhenFeatureUnavailable_DoesNotCallApi()
    {
        var apiCallCount = 0;
        var duplicatesHandler = new StubHttpMessageHandler((req, ct) =>
        {
            apiCallCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var duplicatesClient = new DuplicatesApiClient(
            new HttpClient(duplicatesHandler) { BaseAddress = new Uri("http://duplicates") },
            NullLogger<DuplicatesApiClient>.Instance);

        var tierAccess = new LockedTierAccessService();
        var model = new DuplicatesModel(duplicatesClient, tierAccess);
        IndexModelTests.SetupPageContext(model);

        await model.OnGetAsync(CancellationToken.None);

        Assert.False(model.IsFeatureAvailable);
        Assert.Equal(0, apiCallCount);
        Assert.False(model.ServiceUnavailable);
    }

    [Fact]
    public async Task OnGetAsync_WhenFeatureAvailableAndServiceResponds_PopulatesData()
    {
        var summary = new DuplicateSummaryDto(5, 12, 1024 * 1024 * 500L, 3);
        var groups = new List<DuplicateGroupDto>
        {
            new(Guid.NewGuid(), "abc123def456abc1", 3, 1024 * 1024 * 200L, 1024 * 1024 * 100L, "report.pdf", ["NAS01", "NAS02"])
        };

        var summaryHandler = new StubHttpMessageHandler((req, ct) =>
        {
            if (req.RequestUri!.AbsolutePath.Contains("summary"))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(summary)
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(groups)
            });
        });

        var duplicatesClient = new DuplicatesApiClient(
            new HttpClient(summaryHandler) { BaseAddress = new Uri("http://duplicates") },
            NullLogger<DuplicatesApiClient>.Instance);

        var tierAccess = new AvailableTierAccessService();
        var model = new DuplicatesModel(duplicatesClient, tierAccess);
        IndexModelTests.SetupPageContext(model);

        await model.OnGetAsync(CancellationToken.None);

        Assert.True(model.IsFeatureAvailable);
        Assert.False(model.ServiceUnavailable);
        Assert.Equal(5, model.Summary.DuplicateGroups);
        Assert.Single(model.Groups);
    }

    [Fact]
    public async Task OnGetAsync_WhenFeatureAvailableButServiceUnavailable_SetsServiceUnavailable()
    {
        var duplicatesHandler = new StubHttpMessageHandler((req, ct) =>
            Task.FromException<HttpResponseMessage>(new HttpRequestException("Connection refused")));

        var duplicatesClient = new DuplicatesApiClient(
            new HttpClient(duplicatesHandler) { BaseAddress = new Uri("http://duplicates") },
            NullLogger<DuplicatesApiClient>.Instance);

        var tierAccess = new AvailableTierAccessService();
        var model = new DuplicatesModel(duplicatesClient, tierAccess);
        IndexModelTests.SetupPageContext(model);

        await model.OnGetAsync(CancellationToken.None);

        Assert.True(model.IsFeatureAvailable);
        Assert.True(model.ServiceUnavailable);
        Assert.Equal(0, model.Summary.DuplicateGroups);
        Assert.Empty(model.Groups);
    }

    /// <summary>Simulates a tier where duplicate detection is NOT available.</summary>
    private sealed class LockedTierAccessService : ITierAccessService
    {
        public PricingTier? ActiveTier => null;
        public IReadOnlyList<PricingTier> AllTiers => [];
        public bool IsServiceAvailable(string serviceId) => false;
    }

    /// <summary>Simulates a tier where all features are available.</summary>
    private sealed class AvailableTierAccessService : ITierAccessService
    {
        public PricingTier? ActiveTier => null;
        public IReadOnlyList<PricingTier> AllTiers => [];
        public bool IsServiceAvailable(string serviceId) => true;
    }
}
