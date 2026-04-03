using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System.Data.Common;

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
                if (dbContext.Database.IsRelational())
                {
                    await MigrateRelationalWithFallbackAsync(dbContext, logger, cancellationToken);
                }
                else
                {
                    var created = await dbContext.Database.EnsureCreatedAsync(cancellationToken);
                    if (!created)
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

        if (dbContext.Database.IsRelational())
        {
            await MigrateRelationalWithFallbackAsync(dbContext, logger, cancellationToken);
        }
        else
        {
            var finalCreated = await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            if (!finalCreated)
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
        if (databaseCreator is RelationalDatabaseCreator relationalDatabaseCreator)
        {
            await relationalDatabaseCreator.CreateTablesAsync(cancellationToken);
        }
    }

    private static async Task<bool?> TryHasTablesAsync(IDatabaseCreator databaseCreator, CancellationToken cancellationToken)
    {
        if (databaseCreator is not RelationalDatabaseCreator relationalDatabaseCreator)
        {
            return null;
        }

        return await relationalDatabaseCreator.HasTablesAsync(cancellationToken);
    }

    private static async Task MigrateRelationalWithFallbackAsync(
        DbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception ex) when (LooksLikeLegacyEnsureCreatedDatabase(ex))
        {
            logger.LogWarning(
                ex,
                "Detected legacy relational schema state. Reconciling missing model columns before continuing.");
        }

        await EnsureRelationalColumnsForCurrentModelAsync(dbContext, logger, cancellationToken);
    }

    private static bool LooksLikeLegacyEnsureCreatedDatabase(Exception ex)
    {
        var currentException = ex;
        while (currentException is not null)
        {
            if (currentException.GetType().Name == "SqlException")
            {
                var sqlErrorNumber = GetSqlErrorNumber(currentException);
                if (sqlErrorNumber == 2714)
                {
                    return true;
                }

                var message = currentException.Message;
                if (message.Contains("already an object named", StringComparison.OrdinalIgnoreCase) ||
                    message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            currentException = currentException.InnerException;
        }

        return false;
    }

    private static async Task EnsureRelationalColumnsForCurrentModelAsync(
        DbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!dbContext.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
        {
            return;
        }

        var connection = dbContext.Database.GetDbConnection();
        var openedHere = false;

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
            openedHere = true;
        }

        try
        {
            var seenColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var entityType in dbContext.Model.GetEntityTypes())
            {
                var tableName = entityType.GetTableName();
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    continue;
                }

                var schemaName = entityType.GetSchema() ?? "dbo";
                var storeObject = StoreObjectIdentifier.Table(tableName, schemaName);

                await EnsureSchemaExistsAsync(dbContext, schemaName, cancellationToken);

                foreach (var property in entityType.GetProperties())
                {
                    var columnName = property.GetColumnName(storeObject);
                    if (string.IsNullOrWhiteSpace(columnName))
                    {
                        continue;
                    }

                    var dedupeKey = $"{schemaName}.{tableName}.{columnName}";
                    if (!seenColumns.Add(dedupeKey))
                    {
                        continue;
                    }

                    if (await ColumnExistsAsync(connection, schemaName, tableName, columnName, cancellationToken))
                    {
                        continue;
                    }

                    var columnType = property.GetColumnType(storeObject)
                        ?? property.GetColumnType()
                        ?? property.GetRelationalTypeMapping().StoreType;

                    var isNullable = property.IsNullable;
                    var defaultValueSql = property.GetDefaultValueSql()
                        ?? GetDefaultLiteralForClrType(property.ClrType);

                    if (!isNullable && string.IsNullOrWhiteSpace(defaultValueSql))
                    {
                        throw new InvalidOperationException(
                            $"Cannot add required column {schemaName}.{tableName}.{columnName} without a default value.");
                    }

                    var nullClause = isNullable ? "NULL" : "NOT NULL";
                    var constraintSuffix = Math.Abs($"{tableName}_{columnName}".GetHashCode()).ToString();
                    var constraintName = $"DF_{constraintSuffix}";
                    var defaultClause = !isNullable
                        ? $" CONSTRAINT [{constraintName}] DEFAULT ({defaultValueSql})"
                        : string.Empty;

                    var addColumnSql =
                        $"ALTER TABLE [{schemaName}].[{tableName}] ADD [{columnName}] {columnType} {nullClause}{defaultClause};";

                    logger.LogWarning(
                        "Adding missing relational column {Schema}.{Table}.{Column} ({Type}).",
                        schemaName,
                        tableName,
                        columnName,
                        columnType);

                    await dbContext.Database.ExecuteSqlRawAsync(addColumnSql, cancellationToken);
                }
            }
        }
        finally
        {
            if (openedHere)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task EnsureSchemaExistsAsync(DbContext dbContext, string schemaName, CancellationToken cancellationToken)
    {
        var escapedSchema = schemaName.Replace("'", "''", StringComparison.Ordinal);
        var sql = $"IF SCHEMA_ID(N'{escapedSchema}') IS NULL EXEC(N'CREATE SCHEMA [{schemaName}]');";
        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private static async Task<bool> ColumnExistsAsync(
        DbConnection connection,
        string schema,
        string table,
        string column,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT 1
WHERE EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table AND COLUMN_NAME = @column
);";

        var schemaParameter = command.CreateParameter();
        schemaParameter.ParameterName = "@schema";
        schemaParameter.Value = schema;
        command.Parameters.Add(schemaParameter);

        var tableParameter = command.CreateParameter();
        tableParameter.ParameterName = "@table";
        tableParameter.Value = table;
        command.Parameters.Add(tableParameter);

        var columnParameter = command.CreateParameter();
        columnParameter.ParameterName = "@column";
        columnParameter.Value = column;
        command.Parameters.Add(columnParameter);

        var scalarResult = await command.ExecuteScalarAsync(cancellationToken);
        return scalarResult is not null && scalarResult is not DBNull;
    }

    private static string? GetDefaultLiteralForClrType(Type clrType)
    {
        var underlyingType = Nullable.GetUnderlyingType(clrType) ?? clrType;

        if (underlyingType == typeof(string)) return "N''";
        if (underlyingType == typeof(bool)) return "0";
        if (underlyingType == typeof(byte) || underlyingType == typeof(short) ||
            underlyingType == typeof(int) || underlyingType == typeof(long)) return "0";
        if (underlyingType == typeof(float) || underlyingType == typeof(double) || underlyingType == typeof(decimal)) return "0";
        if (underlyingType == typeof(DateTime)) return "SYSUTCDATETIME()";
        if (underlyingType == typeof(DateTimeOffset)) return "SYSDATETIMEOFFSET()";
        if (underlyingType == typeof(Guid)) return "NEWID()";
        if (underlyingType.IsEnum) return "0";

        return null;
    }
}