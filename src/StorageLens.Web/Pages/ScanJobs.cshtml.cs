using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StorageLens.Shared.Contracts;
using StorageLens.Web.Clients;

namespace StorageLens.Web.Pages;

public class ScanJobsModel(ScanJobsApiClient scanJobsApiClient, LocationsApiClient locationsApiClient) : PageModel
{
    [BindProperty]
    public Guid SelectedLocationId { get; set; }

    [BindProperty]
    public string Notes { get; set; } = "Manual scan request";

    public IReadOnlyList<ScanJobDto> Jobs { get; private set; } = [];
    public IReadOnlyList<StorageLocationDto> Locations { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Scan Jobs";
        ViewData["Nav"] = "ScanJobs";

        Jobs = await scanJobsApiClient.GetScanJobsAsync(cancellationToken);
        Locations = await locationsApiClient.GetLocationsAsync(cancellationToken);
        if (SelectedLocationId == Guid.Empty)
        {
            SelectedLocationId = Locations.FirstOrDefault()?.Id ?? Guid.Empty;
        }
    }

    public async Task<IActionResult> OnPostStartAsync(CancellationToken cancellationToken)
    {
        if (SelectedLocationId == Guid.Empty)
        {
            return RedirectToPage();
        }

        await scanJobsApiClient.StartScanAsync(new CreateScanJobRequest(SelectedLocationId, Notes), cancellationToken);
        return RedirectToPage();
    }
}
