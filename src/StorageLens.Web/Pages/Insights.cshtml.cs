using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StorageLens.Web.Pages;

public class InsightsModel : PageModel
{
    public void OnGet()
    {
        ViewData["Title"] = "Storage Lens Insights";
        ViewData["Nav"] = "Insights";
    }
}
