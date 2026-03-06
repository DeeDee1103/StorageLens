param(
    [Parameter(Mandatory = $true)]
    [string]$AzureSqlServer,

    [Parameter(Mandatory = $true)]
    [string]$SqlUser,

    [string]$DatabaseName = "StorageLens"
)

$ErrorActionPreference = "Stop"

$projectPath = Join-Path $PSScriptRoot "..\src\StorageLens.Services.Locations"
if (-not (Test-Path $projectPath)) {
    throw "Unable to locate StorageLens.Services.Locations project at '$projectPath'."
}

$securePassword = Read-Host "Enter Azure SQL password" -AsSecureString
$ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
try {
    $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
}
finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
}

$baseConnectionString = "Server=tcp:$AzureSqlServer.database.windows.net,1433;Initial Catalog=$DatabaseName;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;User ID=$SqlUser;Password=$plainPassword;"

$connectionStringKeys = @(
    "ConnectionStrings:LocationsDb",
    "ConnectionStrings:ScanJobsDb",
    "ConnectionStrings:FileInventoryDb",
    "ConnectionStrings:DuplicatesDb",
    "ConnectionStrings:ScannerDb",
    "ConnectionStrings:HashingDb",
    "ConnectionStrings:OrchestratorDb"
)

Push-Location $projectPath
try {
    foreach ($key in $connectionStringKeys) {
        dotnet user-secrets set $key $baseConnectionString | Out-Null
        Write-Host "Updated $key in user-secrets."
    }
}
finally {
    Pop-Location
}

Write-Host "All Azure SQL connection strings were written to local user-secrets." -ForegroundColor Green
