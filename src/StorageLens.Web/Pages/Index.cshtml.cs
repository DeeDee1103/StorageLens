using Microsoft.AspNetCore.Mvc.RazorPages;
using StorageLens.Web.Clients;
using StorageLens.Shared.Contracts;

namespace StorageLens.Web.Pages;

public class IndexModel : PageModel
{
    private readonly AnalyticsApiClient _analyticsApiClient;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(AnalyticsApiClient analyticsApiClient, ILogger<IndexModel> logger)
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

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Dashboard";
        ViewData["Nav"] = "Dashboard";

        var dashboard = await _analyticsApiClient.GetDashboardAsync(cancellationToken);
        if (dashboard is not null)
        {
            Dashboard = dashboard;
            return;
        }

        _logger.LogWarning("Analytics service returned no dashboard payload.");
    }
}
