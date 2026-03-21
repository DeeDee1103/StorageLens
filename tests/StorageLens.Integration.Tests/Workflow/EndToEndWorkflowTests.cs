using System.Net;
using System.Net.Http.Json;
using StorageLens.Integration.Tests.Orchestrator;
using StorageLens.Shared.Contracts;
using Xunit;

namespace StorageLens.Integration.Tests.Workflow;

/// <summary>
/// End-to-end workflow tests that exercise the full Orchestrator pipeline —
/// from starting a scan through Scanning → Hashing → DetectingDuplicates →
/// RefreshingAnalytics → Completed — using stub upstream HTTP handlers.
///
/// These tests rely on the WorkflowBackgroundService picking up enqueued
/// workflow IDs and calling ProcessWorkflowAsync asynchronously.  A polling
/// helper waits for the workflow to reach a terminal state.
/// </summary>
public class EndToEndWorkflowTests(OrchestratorWebFactory factory)
    : IClassFixture<OrchestratorWebFactory>
{
    private readonly HttpClient _client = factory.CreateAuthenticatedClient();

    // Polling parameters for async workflow completion.
    private const int PollingIntervalMs  = 100;
    private const int MaxPollingAttempts = 100;  // 10 s total budget

    // ── Full pipeline ────────────────────────────────────────────────────────

    [Fact]
    public async Task FullWorkflow_ScanToAnalytics_CompletesAllSteps()
    {
        // ── 1. Start ─────────────────────────────────────────────────────────
        var startResponse = await _client.PostAsync(
            $"/api/orchestrator/scans/start/{Guid.NewGuid()}", null);

        Assert.Equal(HttpStatusCode.Accepted, startResponse.StatusCode);

        var startResult = await startResponse.Content.ReadFromJsonAsync<WorkflowStartResultDto>();
        Assert.NotNull(startResult);
        Assert.NotEqual(Guid.Empty, startResult.WorkflowId);

        // ── 2. Poll until Completed or Failed ────────────────────────────────
        var workflow = await PollUntilTerminalAsync(startResult.WorkflowId);

        Assert.NotNull(workflow);
        Assert.Equal("Completed", workflow.Status);

        // ── 3. Verify all four pipeline steps ran ────────────────────────────
        var stepNames = workflow.Steps.Select(s => s.StepName).ToHashSet();
        Assert.Contains("Scanning",            stepNames);
        Assert.Contains("Hashing",             stepNames);
        Assert.Contains("DetectingDuplicates", stepNames);
        Assert.Contains("RefreshingAnalytics", stepNames);

        foreach (var step in workflow.Steps)
        {
            Assert.Equal("Completed", step.Status);
        }

        // ── 4. Verify upstream services were called ───────────────────────────
        // locations is called once in StartScanAsync and once more per scan step
        Assert.True(factory.StubClients.LocationsHandler.CallCount >= 1,
            "Locations service should have been called at least once");

        // scanjobs is called multiple times (create + status updates per step)
        Assert.True(factory.StubClients.ScanJobsHandler.CallCount >= 5,
            "ScanJobs service should have been called for creation and status/progress updates");

        Assert.True(factory.StubClients.ScannerHandler.CallCount >= 1,
            "Scanner service should have been called for the Scanning step");

        Assert.True(factory.StubClients.HashingHandler.CallCount >= 1,
            "Hashing service should have been called for the Hashing step");

        Assert.True(factory.StubClients.DuplicatesHandler.CallCount >= 1,
            "Duplicates service should have been called for the DetectingDuplicates step");

        Assert.True(factory.StubClients.AnalyticsHandler.CallCount >= 1,
            "Analytics service should have been called for the RefreshingAnalytics step");
    }

    [Fact]
    public async Task StartScan_Accepted_WorkflowEntersRunningOrCompleted()
    {
        var startResponse = await _client.PostAsync(
            $"/api/orchestrator/scans/start/{Guid.NewGuid()}", null);

        Assert.Equal(HttpStatusCode.Accepted, startResponse.StatusCode);

        var startResult = await startResponse.Content.ReadFromJsonAsync<WorkflowStartResultDto>();
        Assert.NotNull(startResult);

        // After acceptance the background service will pick it up; status moves
        // from Pending → Running → Completed (or Failed).  At minimum it should
        // no longer be Pending after a short wait.
        var workflow = await PollUntilNotPendingAsync(startResult.WorkflowId);
        Assert.NotNull(workflow);
        Assert.NotEqual("Pending", workflow.Status);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Polls GET /api/orchestrator/workflows/{id} until the workflow is in a
    /// terminal state (Completed or Failed), or the budget is exhausted.
    /// </summary>
    private async Task<WorkflowInstanceDto?> PollUntilTerminalAsync(Guid workflowId)
    {
        for (var attempt = 0; attempt < MaxPollingAttempts; attempt++)
        {
            var response = await _client.GetAsync($"/api/orchestrator/workflows/{workflowId}");
            if (!response.IsSuccessStatusCode)
            {
                await Task.Delay(PollingIntervalMs);
                continue;
            }

            var workflow = await response.Content.ReadFromJsonAsync<WorkflowInstanceDto>();
            if (workflow is null)
            {
                await Task.Delay(PollingIntervalMs);
                continue;
            }

            if (workflow.Status is "Completed" or "Failed")
            {
                return workflow;
            }

            await Task.Delay(PollingIntervalMs);
        }

        // Return the last known state (may still be Running — test will fail on assertion).
        var finalResponse = await _client.GetAsync($"/api/orchestrator/workflows/{workflowId}");
        return finalResponse.IsSuccessStatusCode
            ? await finalResponse.Content.ReadFromJsonAsync<WorkflowInstanceDto>()
            : null;
    }

    private async Task<WorkflowInstanceDto?> PollUntilNotPendingAsync(Guid workflowId)
    {
        for (var attempt = 0; attempt < MaxPollingAttempts; attempt++)
        {
            var response = await _client.GetAsync($"/api/orchestrator/workflows/{workflowId}");
            if (!response.IsSuccessStatusCode)
            {
                await Task.Delay(PollingIntervalMs);
                continue;
            }

            var workflow = await response.Content.ReadFromJsonAsync<WorkflowInstanceDto>();
            if (workflow is not null && workflow.Status != "Pending")
            {
                return workflow;
            }

            await Task.Delay(PollingIntervalMs);
        }

        var finalResponse = await _client.GetAsync($"/api/orchestrator/workflows/{workflowId}");
        return finalResponse.IsSuccessStatusCode
            ? await finalResponse.Content.ReadFromJsonAsync<WorkflowInstanceDto>()
            : null;
    }
}
