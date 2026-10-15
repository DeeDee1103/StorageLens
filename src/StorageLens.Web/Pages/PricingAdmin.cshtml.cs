using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StorageLens.Web.Services;

namespace StorageLens.Web.Pages;

public class PricingAdminModel(IConfiguration configuration, IWebHostEnvironment environment, OwnerSession ownerSession) : PageModel
{
    // ── Cookie constants ─────────────────────────────────────────────────────


    // ── Page state ───────────────────────────────────────────────────────────

    public bool IsOwnerAuthenticated { get; private set; }
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public List<PricingTier> Tiers { get; private set; } = [];
    public List<PricingService> AvailableServices { get; private set; } = [];
    public string SelectedEditEnvironment { get; private set; } = EnvironmentSettingsModel.Development;
    public string ActiveTierId { get; private set; } = string.Empty;

    // ── Bind properties (owner PIN form) ─────────────────────────────────────

    [BindProperty]
    public string OwnerPin { get; set; } = string.Empty;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    public void OnGet()
    {
        SetPageDefaults();
        LoadCurrentPricing();
    }

    // ── Owner authentication ─────────────────────────────────────────────────

    public IActionResult OnPostUnlock()
    {
        SetPageDefaults();
        LoadCurrentPricing();

        var storedPin = configuration[$"{PricingOptions.SectionName}:OwnerPin"] ?? string.Empty;

        if (!OwnerSession.PinMatches(storedPin, OwnerPin))
        {
            ErrorMessage = "Incorrect owner PIN. Please try again.";
            return Page();
        }

        ownerSession.SignIn(Response);

        IsOwnerAuthenticated = true;
        SuccessMessage = "Owner mode unlocked. You can now edit pricing tiers and services.";
        return Page();
    }

    public IActionResult OnPostLock()
    {
        OwnerSession.SignOut(Response);
        return RedirectToPage();
    }

    // ── Tier management ──────────────────────────────────────────────────────

    public IActionResult OnPostSaveTier(
        string tierId, string tierName, string tierDescription, string tierHighlight,
        bool tierIsPopular, bool tierIsContactUs, bool tierUseCalculatedPrice,
        decimal tierManualMonthlyPrice, string[] includedServiceIds)
    {
        SetPageDefaults();
        LoadCurrentPricing();

        if (!IsOwnerAuthenticated)
        {
            ErrorMessage = "Owner authentication required.";
            return Page();
        }

        var tier = Tiers.FirstOrDefault(t => t.Id == tierId);
        if (tier is null)
        {
            ErrorMessage = $"Tier '{tierId}' not found.";
            return Page();
        }

        tier.Name = tierName.Trim();
        tier.Description = tierDescription.Trim();
        tier.Highlight = tierHighlight.Trim();
        tier.IsPopular = tierIsPopular;
        tier.IsContactUs = tierIsContactUs;
        tier.UseCalculatedPrice = tierUseCalculatedPrice;
        tier.ManualMonthlyPrice = tierManualMonthlyPrice;
        tier.IncludedServiceIds = [.. includedServiceIds];

        PersistPricing();
        SuccessMessage = $"Tier \"{tier.Name}\" saved successfully.";
        return Page();
    }

    // ── Active tier management ───────────────────────────────────────────────

    public IActionResult OnPostSetActiveTier(string activeTierId)
    {
        SetPageDefaults();
        LoadCurrentPricing();

        if (!IsOwnerAuthenticated)
        {
            ErrorMessage = "Owner authentication required.";
            return Page();
        }

        var id = (activeTierId ?? string.Empty).Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(id) && !Tiers.Any(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase)))
        {
            ErrorMessage = $"Tier '{id}' not found.";
            return Page();
        }

        ActiveTierId = id;
        PersistPricing();
        var label = string.IsNullOrEmpty(id)
            ? "Subscription tier cleared (open access mode)."
            : $"Active subscription tier set to \"{Tiers.FirstOrDefault(t => t.Id == id)?.Name ?? id}\".";
        SuccessMessage = label;
        return Page();
    }

    // ── Service management ───────────────────────────────────────────────────

    public IActionResult OnPostSaveService(
        string serviceId, string serviceName, string serviceDescription,
        string serviceIcon, decimal serviceMonthlyPrice)
    {
        SetPageDefaults();
        LoadCurrentPricing();

        if (!IsOwnerAuthenticated)
        {
            ErrorMessage = "Owner authentication required.";
            return Page();
        }

        var svc = AvailableServices.FirstOrDefault(s => s.Id == serviceId);
        if (svc is null)
        {
            ErrorMessage = $"Service '{serviceId}' not found.";
            return Page();
        }

        svc.Name = serviceName.Trim();
        svc.Description = serviceDescription.Trim();
        svc.Icon = serviceIcon.Trim();
        svc.MonthlyPrice = serviceMonthlyPrice;

        PersistPricing();
        SuccessMessage = $"Service \"{svc.Name}\" saved successfully.";
        return Page();
    }

    public IActionResult OnPostAddService(
        string newServiceId, string newServiceName, string newServiceDescription,
        string newServiceIcon, decimal newServiceMonthlyPrice)
    {
        SetPageDefaults();
        LoadCurrentPricing();

        if (!IsOwnerAuthenticated)
        {
            ErrorMessage = "Owner authentication required.";
            return Page();
        }

        var id = newServiceId.Trim().ToLowerInvariant().Replace(' ', '-');
        if (string.IsNullOrWhiteSpace(id) || AvailableServices.Any(s => s.Id == id))
        {
            ErrorMessage = string.IsNullOrWhiteSpace(id)
                ? "Service ID cannot be empty."
                : $"A service with ID '{id}' already exists.";
            return Page();
        }

        AvailableServices.Add(new PricingService
        {
            Id = id,
            Name = newServiceName.Trim(),
            Description = newServiceDescription.Trim(),
            Icon = string.IsNullOrWhiteSpace(newServiceIcon) ? "bi-check-circle" : newServiceIcon.Trim(),
            MonthlyPrice = newServiceMonthlyPrice,
        });

        PersistPricing();
        SuccessMessage = $"Service \"{newServiceName.Trim()}\" added successfully.";
        return Page();
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private void SetPageDefaults()
    {
        ViewData["Title"] = "Pricing Admin";
        ViewData["Nav"] = "Settings";
        SelectedEditEnvironment = EnvironmentSettingsModel.ResolveEditEnvironment(Request);
        IsOwnerAuthenticated = ownerSession.IsAuthenticated(Request);
    }

    private void LoadCurrentPricing()
    {
        var section = configuration.GetSection(PricingOptions.SectionName);
        Tiers = section.GetSection("Tiers").Get<List<PricingTier>>() ?? [];
        AvailableServices = section.GetSection("AvailableServices").Get<List<PricingService>>() ?? [];
        ActiveTierId = section["ActiveTierId"] ?? string.Empty;
    }

    private void PersistPricing()
    {
        var fileName = $"appsettings.{SelectedEditEnvironment}.json";
        var path = Path.Combine(environment.ContentRootPath, fileName);
        var json = System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path) : "{}";
        var doc = JsonNode.Parse(json) as JsonObject ?? [];

        var writeOpts = new JsonSerializerOptions { WriteIndented = true };
        var pricingNode = JsonSerializer.SerializeToNode(new
        {
            ActiveTierId,
            Tiers,
            AvailableServices,
        }, writeOpts);

        doc[PricingOptions.SectionName] = pricingNode;

        var tempPath = path + ".tmp";
        try
        {
            System.IO.File.WriteAllText(tempPath, doc.ToJsonString(writeOpts));
            System.IO.File.Move(tempPath, path, overwrite: true);
        }
        catch
        {
            if (System.IO.File.Exists(tempPath))
                System.IO.File.Delete(tempPath);
            throw;
        }
    }
}
