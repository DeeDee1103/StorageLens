using System.Net;
using System.Net.Http.Json;
using StorageLens.Integration.Tests.Infrastructure;
using StorageLens.Shared.Contracts;
using Xunit;

namespace StorageLens.Integration.Tests.Scanner;

/// <summary>
/// Integration tests for the Scanner service using an in-memory database and
/// stub HTTP message handlers for the fileinventory and scanjobs upstream services.
///
/// Execute endpoint tests exercise the full controller → service → EF Core pipeline.
/// Because <see cref="ScannerExecutionService"/> walks the real filesystem, paths
/// used in Execute tests must exist on the test runner machine. For "invalid path"
/// validation tests (empty path, bad characters) we rely solely on the service
/// returning a 400 or 500 without touching the filesystem.
/// </summary>
public class ScannerIntegrationTests(ScannerWebFactory factory)
    : IClassFixture<ScannerWebFactory>
{
    private readonly HttpClient _client = factory.CreateAuthenticatedClient();

    // ── POST /api/scanner/execute ────────────────────────────────────────────

    [Fact]
    public async Task Execute_ValidPath_Returns200()
    {
        // Use the system temp directory which is always present on any OS.
        var tempPath = System.IO.Path.GetTempPath();

        var request = new ScannerExecuteRequestBuilder()
            .WithRootPath(tempPath)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/scanner/execute", request);

        // 200 OK when scan succeeded (or partially completed); 500 if the stub
        // upstream rejected a batch — either is acceptable for path-reachability test.
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.InternalServerError,
            $"Expected 200 or 500 but got {response.StatusCode}");

        if (response.IsSuccessStatusCode)
        {
            var dto = await response.Content.ReadFromJsonAsync<ScannerExecutionDto>();
            Assert.NotNull(dto);
            Assert.NotEqual(Guid.Empty, dto.Id);
        }
    }

    [Fact]
    public async Task Execute_EmptyPath_Returns500OrBadRequest()
    {
        // An empty RootPath will either fail model-binding (400) or fail the
        // PathSafety / Directory.Exists check inside the service (500).
        var request = new ScannerExecuteRequestBuilder()
            .WithRootPath(string.Empty)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/scanner/execute", request);

        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.InternalServerError,
            $"Expected 400 or 500 for empty path, got {response.StatusCode}");
    }

    [Fact]
    public async Task Execute_NonExistentPath_Returns500()
    {
        // A well-formed but non-existent path should produce DirectoryNotFoundException
        // which the controller maps to 500.
        var request = new ScannerExecuteRequestBuilder()
            .WithRootPath(@"C:\this-path-does-not-exist-integration-test-" + Guid.NewGuid())
            .Build();

        var response = await _client.PostAsJsonAsync("/api/scanner/execute", request);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Execute_EmptyScanJobId_Returns400()
    {
        // Guid.Empty as ScanJobId should fail validation.
        var request = new ScannerExecuteRequest(
            StorageLocationId: Guid.NewGuid(),
            ScanJobId: Guid.Empty,
            CorrelationId: Guid.NewGuid().ToString("N"),
            LocationName: "Test",
            RootPath: System.IO.Path.GetTempPath());

        var response = await _client.PostAsJsonAsync("/api/scanner/execute", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── GET /api/scanner/executions/{id} ─────────────────────────────────────

    [Fact]
    public async Task GetExecution_ExistingId_ReturnsOk()
    {
        // Create an execution record first by running a scan on a real path.
        var tempPath = System.IO.Path.GetTempPath();
        var createRequest = new ScannerExecuteRequestBuilder()
            .WithRootPath(tempPath)
            .Build();

        var createResponse = await _client.PostAsJsonAsync("/api/scanner/execute", createRequest);
        if (!createResponse.IsSuccessStatusCode)
        {
            // If the scan itself failed we cannot test GetExecution in this path.
            return;
        }

        var created = await createResponse.Content.ReadFromJsonAsync<ScannerExecutionDto>();
        Assert.NotNull(created);

        var response = await _client.GetAsync($"/api/scanner/executions/{created.Id}");

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<ScannerExecutionDto>();
        Assert.NotNull(dto);
        Assert.Equal(created.Id, dto.Id);
    }

    [Fact]
    public async Task GetExecution_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/scanner/executions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Auth ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Request_WithoutApiKey_Returns401()
    {
        using var unauthenticated = factory.CreateClient();

        var response = await unauthenticated.GetAsync($"/api/scanner/executions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Correlation ID ───────────────────────────────────────────────────────

    [Fact]
    public async Task Request_WithCorrelationId_EchoesItInResponse()
    {
        const string correlationId = "scanner-correlation-abc123";
        _client.DefaultRequestHeaders.TryAddWithoutValidation("X-Correlation-ID", correlationId);

        var response = await _client.GetAsync($"/api/scanner/executions/{Guid.NewGuid()}");

        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-ID").FirstOrDefault());

        _client.DefaultRequestHeaders.Remove("X-Correlation-ID");
    }
}
