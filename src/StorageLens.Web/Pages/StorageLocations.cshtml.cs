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
    public string SiteLocation { get; set; } = string.Empty;

    [BindProperty]
    public LocationStatus Status { get; set; } = LocationStatus.Online;

    [BindProperty]
    public double? Latitude { get; set; }

    [BindProperty]
    public double? Longitude { get; set; }

    [BindProperty]
    public StorageLocationType Type { get; set; } = StorageLocationType.Nas;

    public IReadOnlyList<StorageLocationDto> Locations { get; private set; } = [];

    public bool ServiceUnavailable { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Storage Locations";
        ViewData["Nav"] = "Locations";
        var result = await locationsApiClient.GetLocationsAsync(cancellationToken);
        if (result is null)
        {
            ServiceUnavailable = true;
            return;
        }

        Locations = result;
    }

    public async Task<IActionResult> OnPostAddAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Path) || string.IsNullOrWhiteSpace(SiteLocation))
        {
            ModelState.AddModelError(string.Empty, "Name, path, and site location are required.");
            await OnGetAsync(cancellationToken);
            return Page();
        }

        if (Latitude.HasValue ^ Longitude.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Provide both latitude and longitude, or leave both empty.");
            await OnGetAsync(cancellationToken);
            return Page();
        }

        if (Latitude is < -90 or > 90)
        {
            ModelState.AddModelError(string.Empty, "Latitude must be between -90 and 90.");
            await OnGetAsync(cancellationToken);
            return Page();
        }

        if (Longitude is < -180 or > 180)
        {
            ModelState.AddModelError(string.Empty, "Longitude must be between -180 and 180.");
            await OnGetAsync(cancellationToken);
            return Page();
        }

        await locationsApiClient.CreateAsync(
            new CreateStorageLocationRequest(
                Name.Trim(),
                Path.Trim(),
                Type,
                SiteLocation.Trim(),
                Status,
                Latitude,
                Longitude),
            cancellationToken);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostStartScanAsync(Guid locationId, CancellationToken cancellationToken)
    {
        if (locationId == Guid.Empty)
        {
            return RedirectToPage();
        }

        await orchestratorApiClient.StartScanAsync(locationId, cancellationToken: cancellationToken);
        return RedirectToPage("/ScanJobs");
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
