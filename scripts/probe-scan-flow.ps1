$ErrorActionPreference = 'Stop'

$apiKey = 'storagelens-dev-key'
$headers = @{ 'X-API-Key' = $apiKey }

$locations = Invoke-RestMethod -Headers $headers -Uri 'http://localhost:5101/api/locations' -Method Get
if (-not $locations -or $locations.Count -eq 0) {
    throw 'No locations available to start a scan.'
}

$locationId = [string]$locations[0].id
$start = Invoke-WebRequest -UseBasicParsing -Headers $headers -Uri ("http://localhost:5108/api/orchestrator/scans/start/{0}" -f $locationId) -Method Post

Start-Sleep -Seconds 2
$jobs = Invoke-RestMethod -Headers $headers -Uri 'http://localhost:5102/api/scanjobs/recent' -Method Get
$job = $jobs | Sort-Object StartTime -Descending | Select-Object -First 1
if (-not $job) {
    throw 'No scan jobs returned after start.'
}

$jobId = [string]$job.id
$cancel = Invoke-WebRequest -UseBasicParsing -Headers $headers -Uri ("http://localhost:5108/api/orchestrator/scans/{0}/cancel" -f $jobId) -Method Post

Start-Sleep -Seconds 1
$afterCancel = Invoke-RestMethod -Headers $headers -Uri ("http://localhost:5102/api/scanjobs/{0}" -f $jobId) -Method Get

$resume = Invoke-WebRequest -UseBasicParsing -Headers $headers -Uri ("http://localhost:5108/api/orchestrator/scans/{0}/resume" -f $jobId) -Method Post

Start-Sleep -Seconds 1
$afterResume = Invoke-RestMethod -Headers $headers -Uri ("http://localhost:5102/api/scanjobs/{0}" -f $jobId) -Method Get

"LOCATION_ID=$locationId"
"START_STATUS=$([int]$start.StatusCode)"
"JOB_ID=$jobId"
"JOB_STATUS_BEFORE=$($job.status)"
"CANCEL_STATUS=$([int]$cancel.StatusCode)"
"JOB_STATUS_AFTER_CANCEL=$($afterCancel.status)"
"RESUME_STATUS=$([int]$resume.StatusCode)"
"JOB_STATUS_AFTER_RESUME=$($afterResume.status)"

$results = @(
    "LOCATION_ID=$locationId",
    "START_STATUS=$([int]$start.StatusCode)",
    "JOB_ID=$jobId",
    "JOB_STATUS_BEFORE=$($job.status)",
    "CANCEL_STATUS=$([int]$cancel.StatusCode)",
    "JOB_STATUS_AFTER_CANCEL=$($afterCancel.status)",
    "RESUME_STATUS=$([int]$resume.StatusCode)",
    "JOB_STATUS_AFTER_RESUME=$($afterResume.status)"
)

$results | Set-Content -Path "runtime-scan-flow-check.txt"
