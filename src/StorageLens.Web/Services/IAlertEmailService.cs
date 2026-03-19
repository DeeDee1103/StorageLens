namespace StorageLens.Web.Services;

public interface IAlertEmailService
{
    Task<AlertDispatchResult> SendThresholdReachedEmailAsync(
        int currentPercent,
        int thresholdPercent,
        string recipientEmail,
        CancellationToken cancellationToken = default);
}
