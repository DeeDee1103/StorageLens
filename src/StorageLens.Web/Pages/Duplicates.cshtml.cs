using Microsoft.AspNetCore.Mvc.RazorPages;
using StorageLens.Shared.Contracts;
using StorageLens.Web.Clients;
using StorageLens.Web.Services;

namespace StorageLens.Web.Pages;

public class DuplicatesModel(DuplicatesApiClient duplicatesApiClient, ITierAccessService tierAccess) : PageModel
{
    public bool IsFeatureAvailable { get; private set; } = true;
    public DuplicateSummaryDto Summary { get; private set; } = new(0, 0, 0, 0);
    public IReadOnlyList<DuplicateGroupDto> Groups { get; private set; } = [];

    public bool ServiceUnavailable { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Duplicate Files";
        ViewData["Nav"] = "Duplicates";

        IsFeatureAvailable = tierAccess.IsServiceAvailable(ServiceIds.DuplicateDetection);
        if (!IsFeatureAvailable) return;

        var summary = await duplicatesApiClient.GetSummaryAsync(cancellationToken);
        var groups = await duplicatesApiClient.GetGroupsAsync(cancellationToken);

        if (summary is null || groups is null)
        {
            ServiceUnavailable = true;
            return;
        }

        Summary = summary;
        Groups = groups;
    }
}
