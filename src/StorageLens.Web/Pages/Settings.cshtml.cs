using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StorageLens.Web.Pages;

public class SettingsModel(IWebHostEnvironment environment) : PageModel
{
    public bool IsDevelopmentEnvironment { get; private set; }
    public string EnvironmentName { get; private set; } = string.Empty;
    public string SelectedEditEnvironment { get; private set; } = EnvironmentSettingsModel.Development;

    public void OnGet()
    {
        ViewData["Title"] = "Settings";
        ViewData["Nav"] = "Settings";
        IsDevelopmentEnvironment = environment.IsDevelopment();
        EnvironmentName = environment.EnvironmentName;
        SelectedEditEnvironment = EnvironmentSettingsModel.ResolveEditEnvironment(Request);
    }
}
