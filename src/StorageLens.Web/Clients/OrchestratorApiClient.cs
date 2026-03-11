using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
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
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Orchestrator service unavailable.");
            return null;
        }
    }
}
