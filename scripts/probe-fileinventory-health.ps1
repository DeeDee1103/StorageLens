$ErrorActionPreference = 'Stop'

$apiUrl = 'http://localhost:5103/api/files/search?query=&extension=&page=1&pageSize=1'
$pageUrl = 'http://localhost:5001/FileInventory'

$apiOut = 'runtime-fileinventory-api-direct.txt'
$pageOut = 'runtime-fileinventory-page-direct.txt'

Remove-Item $apiOut, $pageOut -ErrorAction SilentlyContinue

try {
    $resp = Invoke-WebRequest -Uri $apiUrl -UseBasicParsing -TimeoutSec 20 -Headers @{ 'X-API-Key' = 'storagelens-dev-key' }
    $body = $resp.Content
    if ($body.Length -gt 500) {
        $body = $body.Substring(0, 500)
    }

    @(
        "STATUS=$([int]$resp.StatusCode)"
        "BODY=$body"
    ) | Set-Content -Path $apiOut
}
catch {
    $statusCode = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 'ERROR' }
    "STATUS=$statusCode ERROR=$($_.Exception.Message)" | Set-Content -Path $apiOut
}

try {
    $page = Invoke-WebRequest -Uri $pageUrl -UseBasicParsing -TimeoutSec 20
    $warn = $page.Content -match 'File Inventory service is currently unavailable'

    @(
        "STATUS=$([int]$page.StatusCode)"
        "UNAVAILABLE_BANNER=$warn"
    ) | Set-Content -Path $pageOut
}
catch {
    $statusCode = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 'ERROR' }
    "STATUS=$statusCode ERROR=$($_.Exception.Message)" | Set-Content -Path $pageOut
}

Get-Content -Path $apiOut
Get-Content -Path $pageOut
