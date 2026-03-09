namespace StorageLens.Web.Services;

public class PricingOptions
{
    public const string SectionName = "Pricing";

    /// <summary>
    /// PIN that the application owner must enter to access the pricing admin page.
    /// Store a strong value here; never commit production secrets to source control.
    /// </summary>
    public string OwnerPin { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the tier this installation is currently subscribed to
    /// (e.g. "starter", "professional", "business", "enterprise").
    /// Leave empty to disable tier enforcement and allow all features.
    /// </summary>
    public string ActiveTierId { get; set; } = string.Empty;

    /// <summary>The four (or more) pricing tiers shown on the public Pricing page.</summary>
    public List<PricingTier> Tiers { get; set; } = [];

    /// <summary>
    /// Catalogue of every service the owner can assign to pricing tiers.
    /// Adding a service here makes it available to select from the admin UI.
    /// </summary>
    public List<PricingService> AvailableServices { get; set; } = [];
}

/// <summary>
/// Well-known service IDs that map to features in the application.
/// These match the <see cref="PricingService.Id"/> values configured in appsettings.json.
/// </summary>
public static class ServiceIds
{
    public const string FileInventory = "file-inventory";
    public const string ScanJobs = "scan-jobs";
    public const string ReportsAnalytics = "reports-analytics";
    public const string EmailSupport = "email-support";
    public const string DuplicateDetection = "duplicate-detection";
    public const string NotificationAlerts = "notification-alerts";
    public const string ApiAccess = "api-access";
    public const string ScheduledScans = "scheduled-scans";
    public const string RetentionPolicies = "retention-policies";
    public const string PrioritySupport = "priority-support";
    public const string AiInsights = "ai-insights";
    public const string CustomIntegrations = "custom-integrations";
}

public class PricingTier
{
    /// <summary>Stable, URL-safe identifier (e.g. "starter", "professional").</summary>
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// When <c>true</c> the price shown is the sum of all included service prices.
    /// When <c>false</c> the <see cref="ManualMonthlyPrice"/> value is used instead.
    /// </summary>
    public bool UseCalculatedPrice { get; set; } = true;

    /// <summary>
    /// Override price (USD/month). Used only when <see cref="UseCalculatedPrice"/> is
    /// <c>false</c>, or for tiers marked <see cref="IsContactUs"/>.
    /// </summary>
    public decimal ManualMonthlyPrice { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>Short marketing headline shown on the pricing card (e.g. "Up to 5 locations").</summary>
    public string Highlight { get; set; } = string.Empty;

    /// <summary>Marks this tier as the recommended / most-popular option.</summary>
    public bool IsPopular { get; set; }

    /// <summary>
    /// When <c>true</c> no price is displayed; a "Contact Us" CTA is shown instead.
    /// </summary>
    public bool IsContactUs { get; set; }

    /// <summary>IDs from <see cref="PricingOptions.AvailableServices"/> included in this tier.</summary>
    public List<string> IncludedServiceIds { get; set; } = [];

    // ── Computed helpers (not persisted) ────────────────────────────────────

    /// <summary>
    /// Returns the effective monthly price: calculated from services or the manual override.
    /// Always returns 0 for "Contact Us" tiers.
    /// </summary>
    public decimal ComputedMonthlyPrice(IEnumerable<PricingService> allServices)
    {
        if (IsContactUs) return 0m;
        if (!UseCalculatedPrice) return ManualMonthlyPrice;

        return allServices
            .Where(s => IncludedServiceIds.Contains(s.Id))
            .Sum(s => s.MonthlyPrice);
    }
}

public class PricingService
{
    /// <summary>Stable, URL-safe identifier (e.g. "file-inventory").</summary>
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Bootstrap-Icons class for this service (e.g. "bi-folder2-open").</summary>
    public string Icon { get; set; } = "bi-check-circle";

    /// <summary>Per-service monthly price (USD) added to the calculated tier total.</summary>
    public decimal MonthlyPrice { get; set; }
}
