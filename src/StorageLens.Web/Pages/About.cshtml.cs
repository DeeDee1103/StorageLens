using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StorageLens.Web.Pages;

public class AboutModel : PageModel
{
    public void OnGet()
    {
        ViewData["Title"] = "About";
        ViewData["Nav"] = "about";
    }
}
