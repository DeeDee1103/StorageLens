using Microsoft.AspNetCore.Mvc.RazorPages;
using StorageLens.Web.Services;

namespace StorageLens.Web.Pages;

public class LandingModel(IConfiguration configuration) : PageModel
{
    public List<PricingTier> Tiers { get; private set; } = [];
    public List<PricingService> AvailableServices { get; private set; } = [];

    public void OnGet()
    {
        ViewData["Title"] = "Landing";

        var section = configuration.GetSection(PricingOptions.SectionName);
        Tiers = section.GetSection("Tiers").Get<List<PricingTier>>() ?? [];
        AvailableServices = section.GetSection("AvailableServices").Get<List<PricingService>>() ?? [];
    }
}