using Microsoft.AspNetCore.Mvc.RazorPages;
using StorageLens.Web.Clients;
using StorageLens.Shared.Contracts;

namespace StorageLens.Web.Pages;

public class GlobalViewModel : PageModel
{
    private readonly AnalyticsApiClient _analyticsApiClient;
    private readonly ILogger<GlobalViewModel> _logger;

    public GlobalViewModel(AnalyticsApiClient analyticsApiClient, ILogger<GlobalViewModel> logger)
    {
        _analyticsApiClient = analyticsApiClient;
        _logger = logger;
    }

    public DashboardDto Dashboard { get; private set; } = new(
        new DashboardKpiDto(0, 0, 0, 0),
        [],
        [],
        [],
        [],
        [],
        []);

    public bool ServiceUnavailable { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Dashboard";
        ViewData["Nav"] = "dashboard";

        var dashboard = await _analyticsApiClient.GetDashboardAsync(cancellationToken);
        if (dashboard is not null)
        {
            Dashboard = dashboard;
            return;
        }

        ServiceUnavailable = true;
        _logger.LogWarning("Analytics service returned no dashboard payload for Global View.");
    }
}
