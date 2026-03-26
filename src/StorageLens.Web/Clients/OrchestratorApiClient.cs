using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using StorageLens.Shared.Contracts;

namespace StorageLens.Web.Clients;

public class OrchestratorApiClient(HttpClient httpClient, ILogger<OrchestratorApiClient> logger)
{
    public async Task<WorkflowStartResultDto?> StartScanAsync(
        Guid locationId,
        DateTimeOffset? scheduledStartUtc = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new StartScanRequest(scheduledStartUtc);
            var response = await httpClient.PostAsJsonAsync($"api/orchestrator/scans/start/{locationId}", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<WorkflowStartResultDto>(cancellationToken);
        }
        catch (Exception ex) when (IsServiceUnavailable(ex, cancellationToken))
        {
            logger.LogWarning(ex, "Orchestrator service unavailable.");
            return null;
        }
    }

    public async Task<bool> CancelJobAsync(Guid scanJobId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsync($"api/orchestrator/scans/{scanJobId}/cancel", null, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (IsServiceUnavailable(ex, cancellationToken))
        {
            logger.LogWarning(ex, "Orchestrator service unavailable when cancelling job {ScanJobId}.", scanJobId);
            return false;
        }
    }

    public async Task<bool> ResumeJobAsync(Guid scanJobId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsync($"api/orchestrator/scans/{scanJobId}/resume", null, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (IsServiceUnavailable(ex, cancellationToken))
        {
            logger.LogWarning(ex, "Orchestrator service unavailable when resuming job {ScanJobId}.", scanJobId);
            return false;
        }
    }

    private static bool IsServiceUnavailable(Exception ex, CancellationToken cancellationToken) =>
        ex is HttpRequestException or BrokenCircuitException ||
        ex is TaskCanceledException && !cancellationToken.IsCancellationRequested;
}
