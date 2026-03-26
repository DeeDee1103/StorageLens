$ErrorActionPreference = 'Stop'

$services = @(
    @{ Name = 'web'; Base = 'http://localhost:5001' },
    @{ Name = 'locations'; Base = 'http://localhost:5101' },
    @{ Name = 'scanjobs'; Base = 'http://localhost:5102' },
    @{ Name = 'fileinventory'; Base = 'http://localhost:5103' },
    @{ Name = 'duplicates'; Base = 'http://localhost:5104' },
    @{ Name = 'analytics'; Base = 'http://localhost:5105' },
    @{ Name = 'scanner'; Base = 'http://localhost:5106' },
    @{ Name = 'hashing'; Base = 'http://localhost:5107' },
    @{ Name = 'orchestrator'; Base = 'http://localhost:5108' }
)

$outputPath = 'runtime-health-sweep.txt'
Remove-Item $outputPath -ErrorAction SilentlyContinue

foreach ($service in $services) {
    foreach ($probe in @('live', 'ready')) {
        $url = "{0}/health/{1}" -f $service.Base, $probe

        try {
            $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 15
            $status = [int]$response.StatusCode
            $ok = if ($status -ge 200 -and $status -lt 300) { 'PASS' } else { 'FAIL' }
            "SERVICE={0} PROBE={1} STATUS={2} RESULT={3}" -f $service.Name, $probe, $status, $ok | Add-Content $outputPath
        }
        catch {
            $statusCode = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 'ERROR' }
            "SERVICE={0} PROBE={1} STATUS={2} RESULT=FAIL ERROR={3}" -f $service.Name, $probe, $statusCode, $_.Exception.Message | Add-Content $outputPath
        }
    }
}

Get-Content $outputPath
