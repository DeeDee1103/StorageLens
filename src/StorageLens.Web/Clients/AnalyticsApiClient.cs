using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using StorageLens.Shared.Contracts;

namespace StorageLens.Web.Clients;

public class AnalyticsApiClient(HttpClient httpClient, ILogger<AnalyticsApiClient> logger)
{
    public async Task<DashboardDto?> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<DashboardDto>("api/analytics/dashboard", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Analytics service unavailable.");
            return null;
        }
    }

    public async Task<ReportsDto?> GetReportsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<ReportsDto>("api/analytics/reports", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Analytics service unavailable.");
            return null;
        }
    }
}
