namespace StorageLens.Web.Data;

/// <summary>
/// Represents a client (customer company) that has been granted access to StorageLens services.
/// </summary>
public class ClientRecord
{
    // ── Identity ─────────────────────────────────────────────────────────────

    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Auto-generated short account number for easy reference (e.g. "SL-00001").</summary>
    public string AccountNumber { get; set; } = string.Empty;

    // ── Company information ───────────────────────────────────────────────────

    public string CompanyName { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;

    // ── Primary contact ───────────────────────────────────────────────────────

    public string ContactFirstName { get; set; } = string.Empty;
    public string ContactLastName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string ContactTitle { get; set; } = string.Empty;

    // ── Billing address ───────────────────────────────────────────────────────

    public string BillingAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;

    // ── Subscription & service access ─────────────────────────────────────────

    /// <summary>
    /// The pricing tier ID assigned to this client (e.g. "starter", "professional").
    /// Maps to <see cref="StorageLens.Web.Services.PricingOptions"/> tier IDs.
    /// </summary>
    public string AssignedTierId { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialised list of service IDs explicitly granted to this client in addition
    /// to (or instead of) the tier defaults. Stored as a JSON string for portability.
    /// </summary>
    public string AssignedServiceIdsJson { get; set; } = "[]";

    // ── Notification preferences ──────────────────────────────────────────────

    /// <summary>Comma-separated list of email addresses to notify for this client's events.</summary>
    public string NotificationEmails { get; set; } = string.Empty;

    public bool NotifyOnScanComplete { get; set; } = true;
    public bool NotifyOnScanFailed { get; set; } = true;
    public bool NotifyOnDuplicatesFound { get; set; } = false;
    public bool NotifyOnStorageThreshold { get; set; } = false;
    public int StorageThresholdPercent { get; set; } = 90;

    // ── Contract dates ────────────────────────────────────────────────────────

    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }

    // ── Status & notes ────────────────────────────────────────────────────────

    /// <summary>Client account status: Active, Inactive, or Suspended.</summary>
    public string Status { get; set; } = ClientStatus.Active;

    public string Notes { get; set; } = string.Empty;

    // ── Audit timestamps ──────────────────────────────────────────────────────

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public static class ClientStatus
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public const string Suspended = "Suspended";

    public static readonly string[] All = [Active, Inactive, Suspended];
}
