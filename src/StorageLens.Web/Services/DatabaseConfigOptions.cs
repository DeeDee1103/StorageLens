namespace StorageLens.Web.Services;

public class DatabaseConfigOptions
{
    public const string SectionName = "DatabaseConfig";

    /// <summary>
    /// Database provider: "LocalDb" or "AzureSql".
    /// </summary>
    public string Provider { get; set; } = "LocalDb";

    /// <summary>
    /// When true all services use an in-memory database (data lost on restart).
    /// </summary>
    public bool UseInMemory { get; set; } = false;

    // ── LocalDB settings ────────────────────────────────────────────────────

    public string LocalDbInstance { get; set; } = "MSSQLLocalDB";
    public string LocalDbDatabase { get; set; } = "StorageLensMvp";

    // ── Azure SQL settings ───────────────────────────────────────────────────

    /// <summary>Short server name without the .database.windows.net suffix.</summary>
    public string AzureSqlServer { get; set; } = string.Empty;

    public string AzureSqlDatabase { get; set; } = "StorageLens";
    public string AzureSqlUserId { get; set; } = string.Empty;
    public bool AzureSqlEncrypt { get; set; } = true;
    public bool AzureSqlTrustServerCertificate { get; set; } = false;
    public int ConnectionTimeout { get; set; } = 30;

    // ── Additional options ───────────────────────────────────────────────────

    /// <summary>
    /// ADO.NET connection-pool minimum size (0 = driver default).
    /// </summary>
    public int MinPoolSize { get; set; } = 0;

    /// <summary>
    /// ADO.NET connection-pool maximum size. Default is 100.
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;

    /// <summary>
    /// Application name embedded in the connection string for easier identification in Azure SQL logs.
    /// </summary>
    public string ApplicationName { get; set; } = "StorageLens";
}
