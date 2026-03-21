using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using StorageLens.Shared.Contracts;

namespace StorageLens.Web.Clients;

public class OrchestratorApiClient(HttpClient httpClient, ILogger<OrchestratorApiClient> logger)
{
    public async Task<WorkflowStartResultDto?> StartScanAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsync($"api/orchestrator/scans/start/{locationId}", null, cancellationToken);
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

    private static bool IsServiceUnavailable(Exception ex, CancellationToken cancellationToken) =>
        ex is HttpRequestException or BrokenCircuitException ||
        ex is TaskCanceledException && !cancellationToken.IsCancellationRequested;
}
