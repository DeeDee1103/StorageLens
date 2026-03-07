using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StorageLens.Web.Pages;

public class ProfileModel : PageModel
{
    public void OnGet()
    {
        ViewData["Title"] = "Profile";
        ViewData["Nav"] = "Profile";
    }
}
