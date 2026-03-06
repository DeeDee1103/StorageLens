using System.Net.Http.Json;
using StorageLens.Shared.Contracts;

namespace StorageLens.Web.Clients;

public class ScanJobsApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<ScanJobDto>> GetScanJobsAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<List<ScanJobDto>>("api/scanjobs", cancellationToken) ?? [];
    }

    public async Task<bool> StartScanAsync(CreateScanJobRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/scanjobs", request, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
