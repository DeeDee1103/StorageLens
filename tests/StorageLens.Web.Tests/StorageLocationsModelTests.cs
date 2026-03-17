using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using StorageLens.Shared.Contracts;
using StorageLens.Web.Clients;
using StorageLens.Web.Pages;
using Xunit;

namespace StorageLens.Web.Tests;

public class StorageLocationsModelTests
{
    [Fact]
    public async Task OnGetAsync_WhenLocationsReturned_PopulatesLocations()
    {
        var locationId = Guid.NewGuid();
        var locations = new List<StorageLocationDto>
        {
            new(locationId, "NAS01", @"\\nas01\data", "Nas", "Online", null, 0, 0, DateTimeOffset.UtcNow)
        };

        var locationsHandler = new StubHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(locations)
            }));

        var orchestratorHandler = new StubHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var locationsClient = new LocationsApiClient(
            new HttpClient(locationsHandler) { BaseAddress = new Uri("http://locations") },
            NullLogger<LocationsApiClient>.Instance);
        var orchestratorClient = new OrchestratorApiClient(
            new HttpClient(orchestratorHandler) { BaseAddress = new Uri("http://orchestrator") },
            NullLogger<OrchestratorApiClient>.Instance);

        var model = new StorageLocationsModel(locationsClient, orchestratorClient);
        IndexModelTests.SetupPageContext(model);

        await model.OnGetAsync(CancellationToken.None);

        Assert.False(model.ServiceUnavailable);
        Assert.Single(model.Locations);
        Assert.Equal("NAS01", model.Locations[0].Name);
    }

    [Fact]
    public async Task OnGetAsync_WhenLocationsServiceUnavailable_SetsServiceUnavailable()
    {
        var locationsHandler = new StubHttpMessageHandler((req, ct) =>
            Task.FromException<HttpResponseMessage>(new HttpRequestException("Connection refused")));

        var orchestratorHandler = new StubHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var locationsClient = new LocationsApiClient(
            new HttpClient(locationsHandler) { BaseAddress = new Uri("http://locations") },
            NullLogger<LocationsApiClient>.Instance);
        var orchestratorClient = new OrchestratorApiClient(
            new HttpClient(orchestratorHandler) { BaseAddress = new Uri("http://orchestrator") },
            NullLogger<OrchestratorApiClient>.Instance);

        var model = new StorageLocationsModel(locationsClient, orchestratorClient);
        IndexModelTests.SetupPageContext(model);

        await model.OnGetAsync(CancellationToken.None);

        Assert.True(model.ServiceUnavailable);
        Assert.Empty(model.Locations);
    }

    [Fact]
    public async Task OnPostAddAsync_WhenNameOrPathEmpty_ReturnsPageWithModelError()
    {
        var locationsHandler = new StubHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<StorageLocationDto>())
            }));

        var orchestratorHandler = new StubHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var locationsClient = new LocationsApiClient(
            new HttpClient(locationsHandler) { BaseAddress = new Uri("http://locations") },
            NullLogger<LocationsApiClient>.Instance);
        var orchestratorClient = new OrchestratorApiClient(
            new HttpClient(orchestratorHandler) { BaseAddress = new Uri("http://orchestrator") },
            NullLogger<OrchestratorApiClient>.Instance);

        var model = new StorageLocationsModel(locationsClient, orchestratorClient);
        IndexModelTests.SetupPageContext(model);
        model.Name = string.Empty;
        model.Path = string.Empty;

        var result = await model.OnPostAddAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }
}
