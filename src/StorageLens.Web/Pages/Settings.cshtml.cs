using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StorageLens.Web.Pages;

public class SettingsModel : PageModel
{
    public void OnGet()
    {
        ViewData["Title"] = "Settings";
        ViewData["Nav"] = "Settings";
    }
}
