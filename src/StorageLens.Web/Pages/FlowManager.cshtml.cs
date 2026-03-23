using Microsoft.AspNetCore.Mvc.RazorPages;
using StorageLens.Web.Clients;
using StorageLens.Shared.Contracts;

namespace StorageLens.Web.Pages;

public class FlowManagerModel : PageModel
{
    private readonly AnalyticsApiClient _analyticsApiClient;
    private readonly ILogger<FlowManagerModel> _logger;

    public FlowManagerModel(AnalyticsApiClient analyticsApiClient, ILogger<FlowManagerModel> logger)
    {
        _analyticsApiClient = analyticsApiClient;
        _logger = logger;
    }

    public DashboardDto Dashboard { get; private set; } = new(
        new DashboardKpiDto(0, 0, 0, 0),
        [], [], [], [], [], []);

    public bool ServiceUnavailable { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Flow Manager";
        ViewData["Nav"] = "FlowManager";

        var dashboard = await _analyticsApiClient.GetDashboardAsync(cancellationToken);
        if (dashboard is not null)
        {
            Dashboard = dashboard;
            return;
        }

        ServiceUnavailable = true;
        _logger.LogWarning("Analytics service unavailable for Flow Manager.");
    }
}
