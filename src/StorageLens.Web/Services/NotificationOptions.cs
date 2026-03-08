namespace StorageLens.Web.Services;

public class NotificationOptions
{
    public const string SectionName = "Notifications";

    // ── Email / SMTP ─────────────────────────────────────────────────────────

    public bool EmailEnabled { get; set; } = false;
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool SmtpUseSsl { get; set; } = true;
    public string SmtpUsername { get; set; } = string.Empty;

    /// <summary>From address shown to recipients.</summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>Display name shown to recipients (e.g. "StorageLens Alerts").</summary>
    public string FromName { get; set; } = "StorageLens";

    /// <summary>Comma-separated list of recipient email addresses.</summary>
    public string ToAddresses { get; set; } = string.Empty;

    // ── Webhook ──────────────────────────────────────────────────────────────

    public bool WebhookEnabled { get; set; } = false;
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>Shared secret appended as an HMAC-SHA256 signature header.</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    // ── Notification events ──────────────────────────────────────────────────

    public bool NotifyOnScanComplete { get; set; } = true;
    public bool NotifyOnScanFailed { get; set; } = true;
    public bool NotifyOnDuplicatesFound { get; set; } = false;
    public bool NotifyOnStorageThreshold { get; set; } = false;

    /// <summary>
    /// Storage usage percentage (0–100) that triggers the threshold alert.
    /// </summary>
    public int StorageThresholdPercent { get; set; } = 90;
}
