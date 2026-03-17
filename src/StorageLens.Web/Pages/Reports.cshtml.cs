using Microsoft.AspNetCore.Mvc.RazorPages;
using StorageLens.Shared.Contracts;
using StorageLens.Web.Clients;

namespace StorageLens.Web.Pages;

public class ReportsModel(AnalyticsApiClient analyticsApiClient) : PageModel
{
    public ReportsDto Reports { get; private set; } = new([], [], [], [], []);

    public bool ServiceUnavailable { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Reports";
        ViewData["Nav"] = "Reports";

        var payload = await analyticsApiClient.GetReportsAsync(cancellationToken);
        if (payload is not null)
        {
            Reports = payload;
            return;
        }

        ServiceUnavailable = true;
    }
}
