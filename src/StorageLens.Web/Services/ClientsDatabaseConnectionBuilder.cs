using System.Text;

namespace StorageLens.Web.Services;

/// <summary>
/// Builds a SQL Server connection string from the <see cref="DatabaseConfigOptions"/> section
/// so that the Web project can connect to the same database server used by the back-end services.
/// </summary>
public static class ClientsDatabaseConnectionBuilder
{
    public static string Build(DatabaseConfigOptions cfg, string? password = null)
    {
        if (cfg.Provider == "AzureSql")
        {
            var sb = new StringBuilder();
            sb.Append($"Server=tcp:{cfg.AzureSqlServer.Trim()}.database.windows.net,1433;");
            sb.Append($"Initial Catalog={cfg.AzureSqlDatabase};");
            sb.Append($"Encrypt={cfg.AzureSqlEncrypt};");
            sb.Append($"TrustServerCertificate={cfg.AzureSqlTrustServerCertificate};");
            sb.Append($"Connection Timeout={cfg.ConnectionTimeout};");
            sb.Append($"User ID={cfg.AzureSqlUserId};");
            if (!string.IsNullOrWhiteSpace(password))
                sb.Append($"Password={password};");
            if (!string.IsNullOrWhiteSpace(cfg.ApplicationName))
                sb.Append($"Application Name={cfg.ApplicationName};");
            if (cfg.MinPoolSize > 0)
                sb.Append($"Min Pool Size={cfg.MinPoolSize};");
            if (cfg.MaxPoolSize != 100)
                sb.Append($"Max Pool Size={cfg.MaxPoolSize};");
            return sb.ToString();
        }
        else
        {
            var sb = new StringBuilder();
            sb.Append($"Server=(localdb)\\{cfg.LocalDbInstance};");
            sb.Append($"Database={cfg.LocalDbDatabase};");
            sb.Append("Trusted_Connection=True;");
            sb.Append("TrustServerCertificate=True;");
            if (!string.IsNullOrWhiteSpace(cfg.ApplicationName))
                sb.Append($"Application Name={cfg.ApplicationName};");
            if (cfg.MinPoolSize > 0)
                sb.Append($"Min Pool Size={cfg.MinPoolSize};");
            if (cfg.MaxPoolSize != 100)
                sb.Append($"Max Pool Size={cfg.MaxPoolSize};");
            return sb.ToString();
        }
    }
}
