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
            var hasAnyTables = await DatabaseHasAnyTablesAsync(dbContext, cancellationToken);
            if (hasAnyTables == true)
            {
                return;
            }

            var databaseCreator = dbContext.Database.GetService<IDatabaseCreator>();
            await TryCreateTablesAsync(databaseCreator, cancellationToken);
        }
        catch (Exception ex) when (TableAlreadyExists(ex))
        {
            // Another instance may have already created these tables.
            logger.LogDebug(ex, "Database tables for current context already exist.");
        }
    }

    private static async Task<bool?> DatabaseHasAnyTablesAsync(DbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            var databaseCreator = dbContext.Database.GetService<IDatabaseCreator>();
            return await TryHasTablesAsync(databaseCreator, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private static bool IsRetryableStartupError(Exception ex)
    {
        var currentException = ex;
        while (currentException is not null)
        {
            if (currentException.GetType().Name == "SqlException")
            {
                var sqlErrorNumber = GetSqlErrorNumber(currentException);
                if (sqlErrorNumber == 1801 || sqlErrorNumber == -2)
                {
                    return true;
                }

                var message = currentException.Message;
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

            if (currentException is System.Net.Sockets.SocketException)
            {
                return true;
            }

            if (currentException is System.IO.IOException ioEx &&
                ioEx.InnerException is System.Net.Sockets.SocketException)
            {
                return true;
            }

            currentException = currentException.InnerException;
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
        var currentException = ex;
        while (currentException is not null)
        {
            var message = currentException.Message;
            if (message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("already an object named", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            currentException = currentException.InnerException;
        }

        return false;
    }

    private static async Task TryCreateTablesAsync(IDatabaseCreator databaseCreator, CancellationToken cancellationToken)
    {
        var createTablesAsync = databaseCreator.GetType().GetMethod(
            "CreateTablesAsync",
            new[] { typeof(CancellationToken) });

        if (createTablesAsync?.Invoke(databaseCreator, new object[] { cancellationToken }) is Task createTask)
        {
            await createTask;
        }
    }

    private static async Task<bool?> TryHasTablesAsync(IDatabaseCreator databaseCreator, CancellationToken cancellationToken)
    {
        var hasTablesAsync = databaseCreator.GetType().GetMethod(
            "HasTablesAsync",
            new[] { typeof(CancellationToken) });

        if (hasTablesAsync?.Invoke(databaseCreator, new object[] { cancellationToken }) is not Task<bool> hasTablesTask)
        {
            return null;
        }

        return await hasTablesTask;
    }
}