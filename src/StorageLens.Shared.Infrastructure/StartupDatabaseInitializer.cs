using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace StorageLens.Shared.Infrastructure;

public static class StartupDatabaseInitializer
{
    public static async Task EnsureCreatedWithRetryAsync(
        DbContext dbContext,
        ILogger logger,
        int maxAttempts = 8,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await dbContext.Database.EnsureCreatedAsync(cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && IsRetryableStartupError(ex))
            {
                var delay = TimeSpan.FromSeconds(Math.Min(10, attempt * 2));
                logger.LogWarning(
                    ex,
                    "Database initialization attempt {Attempt}/{MaxAttempts} failed. Retrying in {DelaySeconds}s.",
                    attempt,
                    maxAttempts,
                    delay.TotalSeconds);

                await Task.Delay(delay, cancellationToken);
            }
        }

        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }

    private static bool IsRetryableStartupError(Exception ex)
    {
        var current = ex;
        while (current is not null)
        {
            if (current.GetType().Name == "SqlException")
            {
                var number = GetSqlErrorNumber(current);
                if (number == 1801 || number == -2)
                {
                    return true;
                }

                var message = current.Message;
                if (message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
                    message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            current = current.InnerException;
        }

        return false;
    }

    private static int? GetSqlErrorNumber(Exception ex)
    {
        var numberProperty = ex.GetType().GetProperty("Number");
        if (numberProperty?.GetValue(ex) is int number)
        {
            return number;
        }

        return null;
    }
}