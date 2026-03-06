using System.Net.Http.Json;
using StorageLens.Shared.Contracts;

namespace StorageLens.Web.Clients;

public class LocationsApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<StorageLocationDto>> GetLocationsAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<List<StorageLocationDto>>("api/locations", cancellationToken) ?? [];
    }

    public async Task<bool> CreateAsync(CreateStorageLocationRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/locations", request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"api/locations/{id}", cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
