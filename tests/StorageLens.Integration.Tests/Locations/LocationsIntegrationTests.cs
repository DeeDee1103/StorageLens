using System.Net;
using System.Net.Http.Json;
using StorageLens.Integration.Tests.Infrastructure;
using StorageLens.Shared.Contracts;
using Xunit;

namespace StorageLens.Integration.Tests.Locations;

/// <summary>
/// Integration tests for the Locations service using an in-memory database.
/// Validates the full request → controller → service → EF Core → response pipeline,
/// including authentication, validation, and error handling.
/// </summary>
public class LocationsIntegrationTests(LocationsWebFactory factory)
    : IClassFixture<LocationsWebFactory>
{
    private readonly HttpClient _client = factory.CreateAuthenticatedClient();

    // ── GET /api/locations ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_EmptyStore_ReturnsOkWithEmptyList()
    {
        var response = await _client.GetAsync("/api/locations");

        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<List<StorageLocationDto>>();
        Assert.NotNull(items);
    }

    // ── POST /api/locations ──────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_Returns201WithLocation()
    {
        var request = new CreateLocationBuilder()
            .WithName("Integration NAS")
            .WithPath(@"\\nas\share")
            .WithType(StorageLocationType.Nas)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/locations", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<StorageLocationDto>();
        Assert.NotNull(created);
        Assert.Equal("Integration NAS", created.Name);
        Assert.NotEqual(Guid.Empty, created.Id);
    }

    [Fact]
    public async Task Create_EmptyName_Returns400WithProblemDetails()
    {
        var request = new CreateLocationBuilder().WithName("").Build();

        var response = await _client.PostAsJsonAsync("/api/locations", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        // FluentValidation returns ValidationProblemDetails
        Assert.Contains("Name", body);
    }

    [Fact]
    public async Task Create_EmptyPath_Returns400()
    {
        var request = new CreateLocationBuilder().WithPath("").Build();

        var response = await _client.PostAsJsonAsync("/api/locations", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── GET /api/locations/{id} ──────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingLocation_ReturnsOk()
    {
        // Arrange — create first
        var created = await CreateLocationAsync("GetById Target");

        var response = await _client.GetAsync($"/api/locations/{created.Id}");

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<StorageLocationDto>();
        Assert.Equal(created.Id, dto!.Id);
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/locations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── PUT /api/locations/{id} ──────────────────────────────────────────────

    [Fact]
    public async Task Update_ExistingLocation_Returns204()
    {
        var created = await CreateLocationAsync("Before Update");

        var update = new UpdateLocationBuilder().WithName("After Update").Build();
        var response = await _client.PutAsJsonAsync($"/api/locations/{created.Id}", update);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Update_UnknownId_Returns404()
    {
        var update = new UpdateLocationBuilder().Build();
        var response = await _client.PutAsJsonAsync($"/api/locations/{Guid.NewGuid()}", update);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── DELETE /api/locations/{id} ───────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingLocation_Returns204()
    {
        var created = await CreateLocationAsync("To Delete");

        var response = await _client.DeleteAsync($"/api/locations/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/locations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Auth ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Request_WithoutApiKey_Returns401()
    {
        using var unauthenticated = factory.CreateClient();
        var response = await unauthenticated.GetAsync("/api/locations");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Correlation ID ───────────────────────────────────────────────────────

    [Fact]
    public async Task Request_WithCorrelationId_EchoesItInResponse()
    {
        const string correlationId = "test-correlation-abc123";
        _client.DefaultRequestHeaders.TryAddWithoutValidation("X-Correlation-ID", correlationId);

        var response = await _client.GetAsync("/api/locations");

        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-ID").FirstOrDefault());

        _client.DefaultRequestHeaders.Remove("X-Correlation-ID");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<StorageLocationDto> CreateLocationAsync(string name)
    {
        var request = new CreateLocationBuilder().WithName(name).Build();
        var response = await _client.PostAsJsonAsync("/api/locations", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StorageLocationDto>())!;
    }
}
