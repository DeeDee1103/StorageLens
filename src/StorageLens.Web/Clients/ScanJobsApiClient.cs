using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
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
        catch (Exception ex) when (IsServiceUnavailable(ex, cancellationToken))
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
        catch (Exception ex) when (IsServiceUnavailable(ex, cancellationToken))
        {
            logger.LogWarning(ex, "ScanJobs service unavailable.");
            return false;
        }
    }

    private static bool IsServiceUnavailable(Exception ex, CancellationToken cancellationToken) =>
        ex is HttpRequestException or BrokenCircuitException ||
        ex is TaskCanceledException && !cancellationToken.IsCancellationRequested;
}
