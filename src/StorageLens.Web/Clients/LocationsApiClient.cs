using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using StorageLens.Shared.Contracts;

namespace StorageLens.Web.Clients;

public class LocationsApiClient(HttpClient httpClient, ILogger<LocationsApiClient> logger)
{
    public async Task<IReadOnlyList<StorageLocationDto>?> GetLocationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<List<StorageLocationDto>>("api/locations", cancellationToken) ?? [];
        }
        catch (Exception ex) when (IsServiceUnavailable(ex, cancellationToken))
        {
            logger.LogWarning(ex, "Locations service unavailable.");
            return null;
        }
    }

    public async Task<bool> CreateAsync(CreateStorageLocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("api/locations", request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (IsServiceUnavailable(ex, cancellationToken))
        {
            logger.LogWarning(ex, "Locations service unavailable.");
            return false;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"api/locations/{id}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (IsServiceUnavailable(ex, cancellationToken))
        {
            logger.LogWarning(ex, "Locations service unavailable.");
            return false;
        }
    }

    private static bool IsServiceUnavailable(Exception ex, CancellationToken cancellationToken) =>
        ex is HttpRequestException or BrokenCircuitException ||
        ex is TaskCanceledException && !cancellationToken.IsCancellationRequested;
}
