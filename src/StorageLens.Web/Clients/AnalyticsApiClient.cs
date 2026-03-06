using System.Net.Http.Json;
using StorageLens.Shared.Contracts;

namespace StorageLens.Web.Clients;

public class AnalyticsApiClient(HttpClient httpClient)
{
    public async Task<DashboardDto?> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<DashboardDto>("api/analytics/dashboard", cancellationToken);
    }

    public async Task<ReportsDto?> GetReportsAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<ReportsDto>("api/analytics/reports", cancellationToken);
    }
}
