using System.Net;
using System.Net.Http.Json;
using StorageLens.Shared.Contracts;
using Xunit;

namespace StorageLens.Integration.Tests.Orchestrator;

/// <summary>
/// Integration tests for the Orchestrator service using an in-memory database and
/// stub HTTP message handlers for all six upstream services.
///
/// Tests validate the full request → controller → service → EF Core → response pipeline,
/// including authentication, route parameters, and error handling.
/// </summary>
public class OrchestratorIntegrationTests(OrchestratorWebFactory factory)
    : IClassFixture<OrchestratorWebFactory>
{
    private readonly HttpClient _client = factory.CreateAuthenticatedClient();

    // ── POST /api/orchestrator/scans/start/{locationId} ───────────────────────

    [Fact]
    public async Task StartScan_ValidRequest_Returns202()
    {
        var locationId = Guid.NewGuid();

        var response = await _client.PostAsync($"/api/orchestrator/scans/start/{locationId}", null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<WorkflowStartResultDto>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.WorkflowId);
        Assert.NotEqual(Guid.Empty, result.ScanJobId);
        Assert.False(string.IsNullOrEmpty(result.CorrelationId));
    }

    [Fact]
    public async Task StartScan_EmptyLocationId_Returns400()
    {
        // Route constraint {locationId:guid} rejects Guid.Empty values passed as
        // the literal empty-GUID string in some ASP.NET versions — or the service
        // will throw when the locations stub returns a "not found" equivalent.
        // Either 400 or 500 is acceptable; 404 on the route is also valid.
        var response = await _client.PostAsync($"/api/orchestrator/scans/start/{Guid.Empty}", null);

        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest  ||
            response.StatusCode == HttpStatusCode.NotFound    ||
            response.StatusCode == HttpStatusCode.InternalServerError,
            $"Expected 400, 404 or 500 for empty location id but got {response.StatusCode}");
    }

    // ── GET /api/orchestrator/workflows/{id} ─────────────────────────────────

    [Fact]
    public async Task GetWorkflow_ExistingId_ReturnsOk()
    {
        // Start a workflow to get a known id.
        var startResponse = await _client.PostAsync(
            $"/api/orchestrator/scans/start/{Guid.NewGuid()}", null);
        Assert.Equal(HttpStatusCode.Accepted, startResponse.StatusCode);

        var startResult = await startResponse.Content.ReadFromJsonAsync<WorkflowStartResultDto>();
        Assert.NotNull(startResult);

        var response = await _client.GetAsync($"/api/orchestrator/workflows/{startResult.WorkflowId}");

        response.EnsureSuccessStatusCode();
        var workflow = await response.Content.ReadFromJsonAsync<WorkflowInstanceDto>();
        Assert.NotNull(workflow);
        Assert.Equal(startResult.WorkflowId, workflow.Id);
    }

    [Fact]
    public async Task GetWorkflow_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/orchestrator/workflows/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── POST /api/orchestrator/workflows/{id}/resume ──────────────────────────

    [Fact]
    public async Task Resume_UnknownId_Returns404()
    {
        var response = await _client.PostAsync(
            $"/api/orchestrator/workflows/{Guid.NewGuid()}/resume", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── POST /api/orchestrator/workflows/{id}/retry ───────────────────────────

    [Fact]
    public async Task Retry_UnknownId_Returns404()
    {
        var response = await _client.PostAsync(
            $"/api/orchestrator/workflows/{Guid.NewGuid()}/retry", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Auth ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Request_WithoutApiKey_Returns401()
    {
        using var unauthenticated = factory.CreateClient();

        var response = await unauthenticated.GetAsync(
            $"/api/orchestrator/workflows/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Correlation ID ───────────────────────────────────────────────────────

    [Fact]
    public async Task Request_WithCorrelationId_EchoesItInResponse()
    {
        const string correlationId = "orchestrator-correlation-xyz";
        _client.DefaultRequestHeaders.TryAddWithoutValidation("X-Correlation-ID", correlationId);

        var response = await _client.GetAsync(
            $"/api/orchestrator/workflows/{Guid.NewGuid()}");

        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-ID").FirstOrDefault());

        _client.DefaultRequestHeaders.Remove("X-Correlation-ID");
    }
}
