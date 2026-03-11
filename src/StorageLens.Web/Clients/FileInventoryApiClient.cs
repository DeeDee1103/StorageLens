using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using StorageLens.Shared.Contracts;

namespace StorageLens.Web.Clients;

public class FileInventoryApiClient(HttpClient httpClient, ILogger<FileInventoryApiClient> logger)
{
    public async Task<IReadOnlyList<FileRecordDto>?> GetFilesAsync(string? search, string? extension, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = $"api/files/search?query={Uri.EscapeDataString(search ?? string.Empty)}&extension={Uri.EscapeDataString(extension ?? string.Empty)}&page={page}&pageSize={pageSize}";
        try
        {
            return await httpClient.GetFromJsonAsync<List<FileRecordDto>>(query, cancellationToken) ?? [];
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "FileInventory service unavailable.");
            return null;
        }
    }
}
