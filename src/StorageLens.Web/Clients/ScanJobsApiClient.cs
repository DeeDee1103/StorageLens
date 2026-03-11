using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using StorageLens.Shared.Contracts;

namespace StorageLens.Web.Clients;

public class ScanJobsApiClient(HttpClient httpClient, ILogger<ScanJobsApiClient> logger)
{
    public async Task<IReadOnlyList<ScanJobDto>?> GetScanJobsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<List<ScanJobDto>>("api/scanjobs", cancellationToken) ?? [];
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "ScanJobs service unavailable.");
            return null;
        }
    }

    public async Task<bool> StartScanAsync(CreateScanJobRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("api/scanjobs", request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "ScanJobs service unavailable.");
            return false;
        }
    }
}
