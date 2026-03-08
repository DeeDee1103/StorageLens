using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StorageLens.Web.Pages;

public class EnvironmentSettingsModel(IWebHostEnvironment environment) : PageModel
{
    // ── Cookie / constants ───────────────────────────────────────────────────

    public const string EditEnvCookieName = "sl-edit-env";
    public const string Development = "Development";
    public const string Production = "Production";

    // ── Page state ───────────────────────────────────────────────────────────

    public bool IsDevelopmentEnvironment { get; private set; }
    public string CurrentEnvironment { get; private set; } = string.Empty;
    public string SelectedEditEnvironment { get; private set; } = Development;
    public string? SuccessMessage { get; set; }

    // ── Lifecycle ────────────────────────────────────────────────────────────

    public void OnGet() => SetPageDefaults();

    public IActionResult OnPostSwitch(string target)
    {
        SetPageDefaults();

        if (!IsDevelopmentEnvironment)
            return Page();

        var env = target == Production ? Production : Development;
        Response.Cookies.Append(EditEnvCookieName, env, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
        });
        SelectedEditEnvironment = env;
        SuccessMessage = $"Active edit environment switched to {env}. " +
                         $"Settings will now be saved to appsettings.{env}.json.";
        return Page();
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private void SetPageDefaults()
    {
        ViewData["Title"] = "Environment Settings";
        ViewData["Nav"] = "Settings";
        IsDevelopmentEnvironment = environment.IsDevelopment();
        CurrentEnvironment = environment.EnvironmentName;
        SelectedEditEnvironment = ResolveEditEnvironment(Request);
    }

    /// <summary>Reads the edit-environment cookie; defaults to Development.</summary>
    public static string ResolveEditEnvironment(HttpRequest request) =>
        request.Cookies.TryGetValue(EditEnvCookieName, out var val) && val == Production
            ? Production
            : Development;
}
