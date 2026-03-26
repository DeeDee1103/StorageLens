$ErrorActionPreference = 'Stop'

$key = 'storagelens-dev-key'
$headers = @{ 'X-API-Key' = $key }

$locations = Invoke-RestMethod -Method Get -Uri 'http://localhost:5101/api/locations' -Headers $headers
if ($locations -isnot [System.Array]) {
    $locations = @($locations)
}
if ($locations.Count -eq 0) {
    throw 'No locations returned from locations service.'
}
$locationId = [Guid]($locations | Select-Object -First 1 -ExpandProperty id)

$scheduled1Utc = (Get-Date).ToUniversalTime().AddMinutes(20).ToString('o')
$scheduled2Utc = (Get-Date).ToUniversalTime().AddMinutes(25).ToString('o')

$scheduledStart1 = Invoke-RestMethod -Method Post -Uri ("http://localhost:5108/api/orchestrator/scans/start/{0}" -f $locationId) -Headers $headers -ContentType 'application/json' -Body (@{ scheduledStartUtc = $scheduled1Utc } | ConvertTo-Json)
$scheduledStart2 = Invoke-RestMethod -Method Post -Uri ("http://localhost:5108/api/orchestrator/scans/start/{0}" -f $locationId) -Headers $headers -ContentType 'application/json' -Body (@{ scheduledStartUtc = $scheduled2Utc } | ConvertTo-Json)

Start-Sleep -Seconds 2

$beforeJobs = Invoke-RestMethod -Method Get -Uri 'http://localhost:5102/api/scanjobs/recent' -Headers $headers
$beforeTracked = $beforeJobs |
    Where-Object { $_.id -in @($scheduledStart1.scanJobId, $scheduledStart2.scanJobId) } |
    Select-Object id,status,currentStage,progressPercent,filesScanned,filesHashed,startTime,endTime

$immediateStart = Invoke-RestMethod -Method Post -Uri ("http://localhost:5108/api/orchestrator/scans/start/{0}" -f $locationId) -Headers $headers -ContentType 'application/json' -Body '{}'

Start-Sleep -Seconds 12

$afterJobs = Invoke-RestMethod -Method Get -Uri 'http://localhost:5102/api/scanjobs/recent' -Headers $headers
$afterTracked = $afterJobs |
    Where-Object { $_.id -in @($scheduledStart1.scanJobId, $scheduledStart2.scanJobId, $immediateStart.scanJobId) } |
    Select-Object id,status,currentStage,progressPercent,filesScanned,filesHashed,startTime,endTime

$wf1 = Invoke-RestMethod -Method Get -Uri ("http://localhost:5108/api/orchestrator/workflows/{0}" -f $scheduledStart1.workflowId) -Headers $headers
$wf2 = Invoke-RestMethod -Method Get -Uri ("http://localhost:5108/api/orchestrator/workflows/{0}" -f $scheduledStart2.workflowId) -Headers $headers
$wfNow = Invoke-RestMethod -Method Get -Uri ("http://localhost:5108/api/orchestrator/workflows/{0}" -f $immediateStart.workflowId) -Headers $headers

$result = [ordered]@{
    locationId = $locationId
    created = [ordered]@{
        scheduled1 = $scheduledStart1
        scheduled2 = $scheduledStart2
        immediate = $immediateStart
    }
    beforeTrackedJobs = $beforeTracked
    afterTrackedJobs = $afterTracked
    workflowStatuses = [ordered]@{
        scheduled1 = $wf1.status
        scheduled2 = $wf2.status
        immediate = $wfNow.status
    }
}

$result | ConvertTo-Json -Depth 8 | Set-Content -Path 'runtime-smoke-scan-isolation.json' -Encoding utf8
Write-Output 'Wrote runtime-smoke-scan-isolation.json'
