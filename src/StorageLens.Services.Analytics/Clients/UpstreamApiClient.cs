using System.Net.Http.Json;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Analytics.Clients;

public class UpstreamApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<StorageLocationDto>> GetLocationsAsync(CancellationToken cancellationToken)
    {
        return await httpClient.GetFromJsonAsync<List<StorageLocationDto>>("api/locations", cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<ScanJobDto>> GetRecentScanJobsAsync(CancellationToken cancellationToken)
    {
        return await httpClient.GetFromJsonAsync<List<ScanJobDto>>("api/scanjobs/recent", cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<FileRecordDto>> GetFilesAsync(CancellationToken cancellationToken)
    {
        return await httpClient.GetFromJsonAsync<List<FileRecordDto>>("api/files?page=1&pageSize=200", cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<DuplicateGroupDto>> GetDuplicateGroupsAsync(CancellationToken cancellationToken)
    {
        return await httpClient.GetFromJsonAsync<List<DuplicateGroupDto>>("api/duplicates/groups", cancellationToken) ?? [];
    }

    public async Task<DuplicateSummaryDto> GetDuplicateSummaryAsync(CancellationToken cancellationToken)
    {
        return await httpClient.GetFromJsonAsync<DuplicateSummaryDto>("api/duplicates/summary", cancellationToken)
               ?? new DuplicateSummaryDto(0, 0, 0, 0);
    }
}
