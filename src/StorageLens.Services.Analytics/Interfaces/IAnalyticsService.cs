using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Analytics.Interfaces;

public interface IAnalyticsService
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken);
    Task<ReportsDto> GetReportsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ChartPointDto>> GetChartsAsync(CancellationToken cancellationToken);
}
