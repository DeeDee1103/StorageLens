using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StorageLens.Shared.Contracts;
using StorageLens.Web.Clients;
using StorageLens.Web.Pages;
using StorageLens.Web.Services;
using Xunit;

namespace StorageLens.Web.Tests;

public class ScanJobsModelTests
{
    [Fact]
    public async Task OnGetAsync_WhenBothServicesRespond_PopulatesJobsAndLocations()
    {
        var locationId = Guid.NewGuid();
        var jobs = new List<ScanJobDto>
        {
            new(Guid.NewGuid(), locationId, "NAS01", "Completed",
                DateTimeOffset.UtcNow.AddHours(-2), DateTimeOffset.UtcNow.AddHours(-1),
                1000, 100, "Test scan")
        };
        var locations = new List<StorageLocationDto>
        {
            new(locationId, "NAS01", @"\\nas01\data", "Nas", "Online", null, 0, 0, DateTimeOffset.UtcNow)
        };

        var scanJobsHandler = new StubHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(jobs)
            }));
        var locationsHandler = new StubHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(locations)
            }));
        var orchestratorHandler = new StubHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var scanJobsClient = new ScanJobsApiClient(
            new HttpClient(scanJobsHandler) { BaseAddress = new Uri("http://scanjobs") },
            NullLogger<ScanJobsApiClient>.Instance);
        var locationsClient = new LocationsApiClient(
            new HttpClient(locationsHandler) { BaseAddress = new Uri("http://locations") },
            NullLogger<LocationsApiClient>.Instance);
        var orchestratorClient = new OrchestratorApiClient(
            new HttpClient(orchestratorHandler) { BaseAddress = new Uri("http://orchestrator") },
            NullLogger<OrchestratorApiClient>.Instance);

        var appearanceOptions = Options.Create(new AppearanceOptions());
        var model = new ScanJobsModel(scanJobsClient, locationsClient, orchestratorClient, appearanceOptions);
        IndexModelTests.SetupPageContext(model);

        await model.OnGetAsync(CancellationToken.None);

        Assert.False(model.ServiceUnavailable);
        Assert.Single(model.Jobs);
        Assert.Single(model.Locations);
        Assert.Equal("Completed", model.Jobs[0].Status);
    }

    [Fact]
    public async Task OnGetAsync_WhenScanJobsServiceUnavailable_SetsServiceUnavailable()
    {
        var scanJobsHandler = new StubHttpMessageHandler((req, ct) =>
            Task.FromException<HttpResponseMessage>(new HttpRequestException("Connection refused")));
        var locationsHandler = new StubHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<StorageLocationDto>())
            }));
        var orchestratorHandler = new StubHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var scanJobsClient = new ScanJobsApiClient(
            new HttpClient(scanJobsHandler) { BaseAddress = new Uri("http://scanjobs") },
            NullLogger<ScanJobsApiClient>.Instance);
        var locationsClient = new LocationsApiClient(
            new HttpClient(locationsHandler) { BaseAddress = new Uri("http://locations") },
            NullLogger<LocationsApiClient>.Instance);
        var orchestratorClient = new OrchestratorApiClient(
            new HttpClient(orchestratorHandler) { BaseAddress = new Uri("http://orchestrator") },
            NullLogger<OrchestratorApiClient>.Instance);

        var appearanceOptions = Options.Create(new AppearanceOptions());
        var model = new ScanJobsModel(scanJobsClient, locationsClient, orchestratorClient, appearanceOptions);
        IndexModelTests.SetupPageContext(model);

        await model.OnGetAsync(CancellationToken.None);

        Assert.True(model.ServiceUnavailable);
        Assert.Empty(model.Jobs);
        Assert.Empty(model.Locations);
    }

    [Fact]
    public async Task HasActiveJobs_WithRunningJob_ReturnsTrue()
    {
        var locationId = Guid.NewGuid();
        var jobs = new List<ScanJobDto>
        {
            new(Guid.NewGuid(), locationId, "NAS01", "Running", DateTimeOffset.UtcNow.AddMinutes(-10), null, 500, 50, "Active scan")
        };

        var scanJobsHandler = new StubHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(jobs)
            }));
        var locationsHandler = new StubHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<StorageLocationDto>())
            }));
        var orchestratorHandler = new StubHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var scanJobsClient = new ScanJobsApiClient(
            new HttpClient(scanJobsHandler) { BaseAddress = new Uri("http://scanjobs") },
            NullLogger<ScanJobsApiClient>.Instance);
        var locationsClient = new LocationsApiClient(
            new HttpClient(locationsHandler) { BaseAddress = new Uri("http://locations") },
            NullLogger<LocationsApiClient>.Instance);
        var orchestratorClient = new OrchestratorApiClient(
            new HttpClient(orchestratorHandler) { BaseAddress = new Uri("http://orchestrator") },
            NullLogger<OrchestratorApiClient>.Instance);

        var appearanceOptions = Options.Create(new AppearanceOptions());
        var model = new ScanJobsModel(scanJobsClient, locationsClient, orchestratorClient, appearanceOptions);
        IndexModelTests.SetupPageContext(model);
        await model.OnGetAsync(CancellationToken.None);

        Assert.True(model.HasActiveJobs);
    }
}
