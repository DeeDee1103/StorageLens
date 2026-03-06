using Microsoft.AspNetCore.Mvc.RazorPages;
using StorageLens.Shared.Contracts;
using StorageLens.Web.Clients;

namespace StorageLens.Web.Pages;

public class DuplicatesModel(DuplicatesApiClient duplicatesApiClient) : PageModel
{
    public DuplicateSummaryDto Summary { get; private set; } = new(0, 0, 0, 0);
    public IReadOnlyList<DuplicateGroupDto> Groups { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Duplicate Files";
        ViewData["Nav"] = "Duplicates";
        Summary = await duplicatesApiClient.GetSummaryAsync(cancellationToken);
        Groups = await duplicatesApiClient.GetGroupsAsync(cancellationToken);
    }
}
