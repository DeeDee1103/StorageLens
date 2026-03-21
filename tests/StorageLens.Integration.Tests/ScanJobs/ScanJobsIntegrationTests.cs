using System.Net;
using System.Net.Http.Json;
using StorageLens.Integration.Tests.Infrastructure;
using StorageLens.Shared.Contracts;
using Xunit;

namespace StorageLens.Integration.Tests.ScanJobs;

/// <summary>
/// Integration tests for the ScanJobs service using an in-memory database.
/// Validates the full request → controller → service → EF Core → response pipeline,
/// including authentication, validation, status/progress transitions, and error handling.
/// </summary>
public class ScanJobsIntegrationTests(ScanJobsWebFactory factory)
    : IClassFixture<ScanJobsWebFactory>
{
    private readonly HttpClient _client = factory.CreateAuthenticatedClient();

    // ── GET /api/scanjobs ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_EmptyStore_ReturnsOkWithEmptyList()
    {
        var response = await _client.GetAsync("/api/scanjobs");

        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<List<ScanJobDto>>();
        Assert.NotNull(items);
    }

    // ── POST /api/scanjobs ───────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_Returns201()
    {
        var request = new CreateScanJobBuilder()
            .ForLocation(Guid.NewGuid())
            .WithNotes("Integration test job")
            .Build();

        var response = await _client.PostAsJsonAsync("/api/scanjobs", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ScanJobDto>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(request.StorageLocationId, created.StorageLocationId);
    }

    [Fact]
    public async Task Create_EmptyLocationId_Returns400()
    {
        // StorageLocationId of Guid.Empty should fail model binding / validation.
        var request = new CreateScanJobRequest(Guid.Empty, "notes", null);

        var response = await _client.PostAsJsonAsync("/api/scanjobs", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── GET /api/scanjobs/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingJob_ReturnsOk()
    {
        var created = await CreateJobAsync("GetById target");

        var response = await _client.GetAsync($"/api/scanjobs/{created.Id}");

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<ScanJobDto>();
        Assert.NotNull(dto);
        Assert.Equal(created.Id, dto.Id);
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/scanjobs/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── PUT /api/scanjobs/{id}/status ────────────────────────────────────────

    [Fact]
    public async Task UpdateStatus_ValidTransition_ReturnsOkWithUpdatedJob()
    {
        var created = await CreateJobAsync("Status update target");

        var statusRequest = new UpdateScanJobStatusRequest(
            ScanJobStatus.Scanning, "Scanning", "Scanner started");

        var response = await _client.PutAsJsonAsync(
            $"/api/scanjobs/{created.Id}/status", statusRequest);

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<ScanJobDto>();
        Assert.NotNull(updated);
        Assert.Equal("Scanning", updated.Status);
    }

    [Fact]
    public async Task UpdateStatus_UnknownId_Returns404()
    {
        var statusRequest = new UpdateScanJobStatusRequest(
            ScanJobStatus.Scanning, "Scanning");

        var response = await _client.PutAsJsonAsync(
            $"/api/scanjobs/{Guid.NewGuid()}/status", statusRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── PUT /api/scanjobs/{id}/progress ──────────────────────────────────────

    [Fact]
    public async Task UpdateProgress_Valid_ReturnsOkWithUpdatedJob()
    {
        var created = await CreateJobAsync("Progress update target");

        var progressRequest = new UpdateScanJobProgressRequest(
            FilesScanned: 500,
            FilesHashed: 100,
            ProgressPercent: 50,
            WarningCount: 0,
            ErrorCount: 0,
            DuplicateGroupsFound: 0,
            Notes: "Halfway done");

        var response = await _client.PutAsJsonAsync(
            $"/api/scanjobs/{created.Id}/progress", progressRequest);

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<ScanJobDto>();
        Assert.NotNull(updated);
        Assert.Equal(500, updated.FilesScanned);
        Assert.Equal(50, updated.ProgressPercent);
    }

    [Fact]
    public async Task UpdateProgress_Over100_ClampedTo100()
    {
        var created = await CreateJobAsync("Progress clamping target");

        // Send ProgressPercent beyond the valid range (clamping is handled in service)
        var progressRequest = new UpdateScanJobProgressRequest(
            FilesScanned: 9999,
            FilesHashed: 9999,
            ProgressPercent: 150,
            WarningCount: 0,
            ErrorCount: 0,
            DuplicateGroupsFound: 0);

        var response = await _client.PutAsJsonAsync(
            $"/api/scanjobs/{created.Id}/progress", progressRequest);

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<ScanJobDto>();
        Assert.NotNull(updated);
        // ScanJobsService clamps ProgressPercent to Math.Clamp(value, 0, 100)
        Assert.Equal(100, updated.ProgressPercent);
    }

    [Fact]
    public async Task UpdateProgress_UnknownId_Returns404()
    {
        var progressRequest = new UpdateScanJobProgressRequest(
            FilesScanned: 1,
            FilesHashed: 0,
            ProgressPercent: 10,
            WarningCount: 0,
            ErrorCount: 0,
            DuplicateGroupsFound: 0);

        var response = await _client.PutAsJsonAsync(
            $"/api/scanjobs/{Guid.NewGuid()}/progress", progressRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── GET /api/scanjobs/recent ─────────────────────────────────────────────

    [Fact]
    public async Task GetRecentJobs_ReturnsOk()
    {
        // Seed a few jobs so the endpoint has something to return.
        await CreateJobAsync("Recent 1");
        await CreateJobAsync("Recent 2");

        var response = await _client.GetAsync("/api/scanjobs/recent");

        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<List<ScanJobDto>>();
        Assert.NotNull(items);
        Assert.True(items.Count >= 2);
    }

    // ── Auth ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Request_WithoutApiKey_Returns401()
    {
        using var unauthenticated = factory.CreateClient();

        var response = await unauthenticated.GetAsync("/api/scanjobs");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Correlation ID ───────────────────────────────────────────────────────

    [Fact]
    public async Task Request_WithCorrelationId_EchoesItInResponse()
    {
        const string correlationId = "scanjobs-correlation-xyz987";
        _client.DefaultRequestHeaders.TryAddWithoutValidation("X-Correlation-ID", correlationId);

        var response = await _client.GetAsync("/api/scanjobs");

        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-ID").FirstOrDefault());

        _client.DefaultRequestHeaders.Remove("X-Correlation-ID");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<ScanJobDto> CreateJobAsync(string notes)
    {
        var request = new CreateScanJobBuilder().WithNotes(notes).Build();
        var response = await _client.PostAsJsonAsync("/api/scanjobs", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ScanJobDto>())!;
    }
}
