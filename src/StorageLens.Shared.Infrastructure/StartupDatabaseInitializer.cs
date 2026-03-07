using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
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
                var created = await dbContext.Database.EnsureCreatedAsync(cancellationToken);

                if (!created)
                {
                    await EnsureTablesCreatedForCurrentContextAsync(dbContext, logger, cancellationToken);
                }

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

        var finalCreated = await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        if (!finalCreated)
        {
            await EnsureTablesCreatedForCurrentContextAsync(dbContext, logger, cancellationToken);
        }
    }

    private static async Task EnsureTablesCreatedForCurrentContextAsync(
        DbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var creator = dbContext.Database.GetService<IDatabaseCreator>();
            var createTablesAsync = creator.GetType().GetMethod("CreateTablesAsync", [typeof(CancellationToken)]);

            if (createTablesAsync is null)
            {
                return;
            }

            var task = createTablesAsync.Invoke(creator, [cancellationToken]) as Task;
            if (task is not null)
            {
                await task;
            }
        }
        catch (Exception ex) when (TableAlreadyExists(ex))
        {
            // Another instance may have already created these tables.
            logger.LogDebug(ex, "Database tables for current context already exist.");
        }
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
                    message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                    message.Contains("pre-login handshake", StringComparison.OrdinalIgnoreCase) ||
                    message.Contains("login failed", StringComparison.OrdinalIgnoreCase) ||
                    message.Contains("connection refused", StringComparison.OrdinalIgnoreCase) ||
                    message.Contains("connection reset", StringComparison.OrdinalIgnoreCase) ||
                    message.Contains("connection failed", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            if (current is System.Net.Sockets.SocketException)
            {
                return true;
            }

            if (current is System.IO.IOException ioEx &&
                ioEx.InnerException is System.Net.Sockets.SocketException)
            {
                return true;
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

    private static bool TableAlreadyExists(Exception ex)
    {
        var current = ex;
        while (current is not null)
        {
            var message = current.Message;
            if (message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("already an object named", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }
}