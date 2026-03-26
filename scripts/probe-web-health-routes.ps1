$ErrorActionPreference = 'Stop'

$routes = @('/health/live', '/health/ready', '/health', '/Index')
$out = 'runtime-web-health-routes.txt'
Remove-Item $out -ErrorAction SilentlyContinue

foreach ($route in $routes) {
    $url = "http://localhost:5001$route"
    try {
        $resp = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 15
        "ROUTE=$route STATUS=$([int]$resp.StatusCode) RESULT=PASS" | Add-Content $out
    }
    catch {
        $statusCode = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 'ERROR' }
        "ROUTE=$route STATUS=$statusCode RESULT=FAIL ERROR=$($_.Exception.Message)" | Add-Content $out
    }
}

Get-Content $out
