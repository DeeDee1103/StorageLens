using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace StorageLens.Web.Services;

public sealed class SmtpAlertEmailService(
    IOptions<NotificationOptions> notificationOptions,
    IConfiguration configuration,
    IMemoryCache cache,
    ILogger<SmtpAlertEmailService> logger) : IAlertEmailService
{
    private static readonly TimeSpan DispatchCooldown = TimeSpan.FromMinutes(30);

    public async Task<AlertDispatchResult> SendThresholdReachedEmailAsync(
        int currentPercent,
        int thresholdPercent,
        string recipientEmail,
        CancellationToken cancellationToken = default)
    {
        var n = notificationOptions.Value;
        if (!n.EmailEnabled)
        {
            return new AlertDispatchResult(false, "Email notifications are disabled in Notification Settings.");
        }

        var recipients = BuildRecipientList(recipientEmail, n.ToAddresses);
        if (recipients.Count == 0)
        {
            return new AlertDispatchResult(false, "No recipient email addresses configured for alerts.");
        }

        if (string.IsNullOrWhiteSpace(n.SmtpHost) || string.IsNullOrWhiteSpace(n.SmtpUsername))
        {
            return new AlertDispatchResult(false, "SMTP host and username must be configured before sending alerts.");
        }

        if (string.IsNullOrWhiteSpace(n.FromAddress))
        {
            return new AlertDispatchResult(false, "From address must be configured before sending alerts.");
        }

        var smtpPassword = configuration["Notifications:SmtpPassword"];
        if (string.IsNullOrWhiteSpace(smtpPassword))
        {
            return new AlertDispatchResult(false, "Notifications:SmtpPassword user-secret is missing.");
        }

        var cacheKey = BuildCacheKey(thresholdPercent, recipients);
        if (cache.TryGetValue(cacheKey, out _))
        {
            return new AlertDispatchResult(false, "Alert already sent recently. Waiting for cooldown before sending again.");
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(n.FromAddress, string.IsNullOrWhiteSpace(n.FromName) ? "StorageLens" : n.FromName),
                Subject = $"StorageLens Alert: Threshold reached ({currentPercent}%)",
                Body = $"StorageLens detected usage at {currentPercent}% which meets or exceeds the configured maximum threshold of {thresholdPercent}%.\n\nTriggered at: {DateTimeOffset.UtcNow:u}",
                IsBodyHtml = false
            };

            foreach (var address in recipients)
            {
                message.To.Add(address);
            }

            using var client = new SmtpClient(n.SmtpHost, n.SmtpPort)
            {
                EnableSsl = n.SmtpUseSsl,
                Credentials = new NetworkCredential(n.SmtpUsername, smtpPassword)
            };

            cancellationToken.ThrowIfCancellationRequested();
            await client.SendMailAsync(message, cancellationToken);

            cache.Set(cacheKey, true, DispatchCooldown);
            logger.LogInformation("Threshold alert email sent to {Recipients} at {CurrentPercent}%.", string.Join(",", recipients), currentPercent);

            return new AlertDispatchResult(true, $"Alert email sent to: {string.Join(", ", recipients)}.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send threshold alert email.");
            return new AlertDispatchResult(false, $"Failed to send email alert: {ex.Message}");
        }
    }

    private static List<string> BuildRecipientList(string explicitRecipient, string configuredRecipients)
    {
        return (explicitRecipient + "," + configuredRecipients)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string BuildCacheKey(int thresholdPercent, IEnumerable<string> recipients)
    {
        return $"alert-threshold:{thresholdPercent}:{string.Join(';', recipients.OrderBy(r => r, StringComparer.OrdinalIgnoreCase))}";
    }
}
