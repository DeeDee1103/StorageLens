using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StorageLens.Shared.Contracts;
using StorageLens.Web.Clients;

namespace StorageLens.Web.Pages;

public class StorageLocationsModel(
    LocationsApiClient locationsApiClient,
    OrchestratorApiClient orchestratorApiClient) : PageModel
{
    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public string Path { get; set; } = string.Empty;

    [BindProperty]
    public StorageLocationType Type { get; set; } = StorageLocationType.Nas;

    public IReadOnlyList<StorageLocationDto> Locations { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Storage Locations";
        ViewData["Nav"] = "Locations";
        Locations = await locationsApiClient.GetLocationsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAddAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Path))
        {
            ModelState.AddModelError(string.Empty, "Name and path are required.");
            await OnGetAsync(cancellationToken);
            return Page();
        }

        await locationsApiClient.CreateAsync(new CreateStorageLocationRequest(Name.Trim(), Path.Trim(), Type), cancellationToken);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostStartScanAsync(Guid locationId, CancellationToken cancellationToken)
    {
        if (locationId == Guid.Empty)
        {
            return RedirectToPage();
        }

        await orchestratorApiClient.StartScanAsync(locationId, cancellationToken);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid locationId, CancellationToken cancellationToken)
    {
        if (locationId == Guid.Empty)
        {
            return RedirectToPage();
        }

        await locationsApiClient.DeleteAsync(locationId, cancellationToken);
        return RedirectToPage();
    }
}
