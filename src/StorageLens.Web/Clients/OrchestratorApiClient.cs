using System.Net.Http.Json;
using StorageLens.Shared.Contracts;

namespace StorageLens.Web.Clients;

public class OrchestratorApiClient(HttpClient httpClient)
{
    public async Task<WorkflowStartResultDto?> StartScanAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync($"api/orchestrator/scans/start/{locationId}", null, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<WorkflowStartResultDto>(cancellationToken);
    }
}
