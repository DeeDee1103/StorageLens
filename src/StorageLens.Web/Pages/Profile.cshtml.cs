using Microsoft.AspNetCore.Mvc.RazorPages;
using StorageLens.Web.Services;

namespace StorageLens.Web.Pages;

public class ProfileModel(ITierAccessService tierAccess) : PageModel
{
    public PricingTier? ActiveTier => tierAccess.ActiveTier;

    public void OnGet()
    {
        ViewData["Title"] = "Profile";
        ViewData["Nav"] = "Profile";
    }
}
