using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using StorageLens.Shared.Contracts;

namespace StorageLens.Web.Clients;

public class DuplicatesApiClient(HttpClient httpClient, ILogger<DuplicatesApiClient> logger)
{
    public async Task<DuplicateSummaryDto?> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<DuplicateSummaryDto>("api/duplicates/summary", cancellationToken);
        }
        catch (Exception ex) when (IsServiceUnavailable(ex, cancellationToken))
        {
            logger.LogWarning(ex, "Duplicates service unavailable.");
            return null;
        }
    }

    public async Task<IReadOnlyList<DuplicateGroupDto>?> GetGroupsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<List<DuplicateGroupDto>>("api/duplicates/groups", cancellationToken) ?? [];
        }
        catch (Exception ex) when (IsServiceUnavailable(ex, cancellationToken))
        {
            logger.LogWarning(ex, "Duplicates service unavailable.");
            return null;
        }
    }

    private static bool IsServiceUnavailable(Exception ex, CancellationToken cancellationToken) =>
        ex is HttpRequestException or BrokenCircuitException ||
        ex is TaskCanceledException && !cancellationToken.IsCancellationRequested;
}
