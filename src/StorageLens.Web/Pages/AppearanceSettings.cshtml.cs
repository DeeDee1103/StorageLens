using Microsoft.AspNetCore.Mvc.RazorPages;
using StorageLens.Web.Services;

namespace StorageLens.Web.Pages;

public class AppearanceSettingsModel(IConfiguration configuration) : PageModel
{
    // ── Server-side defaults (read from appsettings) ─────────────────────────

    public string DefaultTheme { get; private set; } = "system";
    public string DefaultLandingPage { get; private set; } = "/Index";
    public int TablePageSize { get; private set; } = 25;
    public int ScanAutoRefreshSeconds { get; private set; } = 10;
    public int DashboardDaysRange { get; private set; } = 30;
    public bool EnableKpiCountAnimation { get; private set; } = true;
    public bool SidebarCollapsedByDefault { get; private set; } = false;

    // ── Page static metadata ─────────────────────────────────────────────────

    public static readonly string[] LandingPages =
    [
        "/Index",
        "/StorageLocations",
        "/ScanJobs",
        "/FileInventory",
        "/Duplicates",
        "/Reports",
    ];

    public static readonly (int Value, string Label)[] TablePageSizes =
    [
        (10,  "10 rows"),
        (25,  "25 rows"),
        (50,  "50 rows"),
        (100, "100 rows"),
    ];

    public static readonly (int Value, string Label)[] RefreshIntervals =
    [
        (0,   "Disabled"),
        (5,   "Every 5 seconds"),
        (10,  "Every 10 seconds"),
        (30,  "Every 30 seconds"),
        (60,  "Every 60 seconds"),
    ];

    public static readonly (int Value, string Label)[] DashboardRanges =
    [
        (7,  "Last 7 days"),
        (14, "Last 14 days"),
        (30, "Last 30 days"),
    ];

    // ── Lifecycle ────────────────────────────────────────────────────────────

    public void OnGet()
    {
        ViewData["Title"] = "Appearance & Display";
        ViewData["Nav"] = "Settings";

        var section = configuration.GetSection(AppearanceOptions.SectionName);
        DefaultTheme = section["DefaultTheme"] ?? "system";
        DefaultLandingPage = section["DefaultLandingPage"] ?? "/Index";
        TablePageSize = section.GetValue("TablePageSize", 25);
        ScanAutoRefreshSeconds = section.GetValue("ScanAutoRefreshSeconds", 10);
        DashboardDaysRange = section.GetValue("DashboardDaysRange", 30);
        EnableKpiCountAnimation = section.GetValue("EnableKpiCountAnimation", true);
        SidebarCollapsedByDefault = section.GetValue<bool>("SidebarCollapsedByDefault");
    }
}
