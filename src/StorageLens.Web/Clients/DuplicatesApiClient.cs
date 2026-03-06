using System.Net.Http.Json;
using StorageLens.Shared.Contracts;

namespace StorageLens.Web.Clients;

public class DuplicatesApiClient(HttpClient httpClient)
{
    public async Task<DuplicateSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<DuplicateSummaryDto>("api/duplicates/summary", cancellationToken)
               ?? new DuplicateSummaryDto(0, 0, 0, 0);
    }

    public async Task<IReadOnlyList<DuplicateGroupDto>> GetGroupsAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<List<DuplicateGroupDto>>("api/duplicates/groups", cancellationToken) ?? [];
    }
}
