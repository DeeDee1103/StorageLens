using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StorageLens.Shared.Contracts;
using StorageLens.Web.Clients;

namespace StorageLens.Web.Pages;

public class FileInventoryModel(FileInventoryApiClient fileInventoryApiClient) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Extension { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; } = 25;
    public IReadOnlyList<FileRecordDto> Files { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "File Inventory";
        ViewData["Nav"] = "FileInventory";
        Files = await fileInventoryApiClient.GetFilesAsync(Search, Extension, PageNumber, PageSize, cancellationToken);
    }
}
