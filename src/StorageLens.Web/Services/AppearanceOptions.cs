namespace StorageLens.Web.Services;

public class AppearanceOptions
{
    public const string SectionName = "Appearance";

    /// <summary>
    /// Default color theme: "system", "light", or "dark".
    /// The runtime value is always taken from localStorage; this is the server-side default.
    /// </summary>
    public string DefaultTheme { get; set; } = "system";

    /// <summary>
    /// Razor page path the user is sent to after signing in (e.g. "/Index", "/ScanJobs").
    /// </summary>
    public string DefaultLandingPage { get; set; } = "/Index";

    /// <summary>
    /// Default number of rows shown in data tables (10, 25, 50, or 100).
    /// </summary>
    public int TablePageSize { get; set; } = 25;

    /// <summary>
    /// Interval in seconds used by live-refresh pages such as Scan Jobs and File Inventory. 0 = disabled.
    /// </summary>
    public int ScanAutoRefreshSeconds { get; set; } = 10;

    /// <summary>
    /// Number of days of history shown on the Dashboard Storage Growth chart (7, 14, or 30).
    /// </summary>
    public int DashboardDaysRange { get; set; } = 30;

    /// <summary>
    /// When true the Dashboard KPI cards animate counting up to their final value on load.
    /// </summary>
    public bool EnableKpiCountAnimation { get; set; } = true;

    /// <summary>
    /// When true, the sidebar is collapsed to icon-only mode by default on desktop.
    /// </summary>
    public bool SidebarCollapsedByDefault { get; set; } = false;
}
