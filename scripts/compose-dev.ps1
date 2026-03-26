param(
    [ValidateSet("up", "restart-clean", "up-clean", "full-recover")]
    [string]$Mode = "up"
)

$ErrorActionPreference = "Stop"

# Docker build/progress streams use stderr for normal output.
# Keep PowerShell from treating those stderr lines as terminating errors.
if (Get-Variable -Name PSNativeCommandUseErrorActionPreference -ErrorAction SilentlyContinue) {
    $PSNativeCommandUseErrorActionPreference = $false
}

if (-not $env:STORAGELENS_SQL_PASSWORD) {
    $env:STORAGELENS_SQL_PASSWORD = "ReplaceWithStrongPassword123!"
}

if (-not $env:STORAGELENS_SERVICE_API_KEY) {
    $env:STORAGELENS_SERVICE_API_KEY = "storagelens-dev-key"
}

switch ($Mode) {
    "full-recover" {
        $ids = docker ps -aq --filter "name=^/storagelens-"
        if ($ids) {
            docker rm -f $ids
        }
        docker compose down --remove-orphans
        # Reset SQL persisted state so the active SA password is reapplied on startup.
        docker volume rm -f storagelens_storagelens-sql-data 2>&1 | Out-Null
        docker compose up -d --build
    }

    "restart-clean" {
        docker compose down --remove-orphans
        $sqlContainerId = docker ps -aq --filter "name=^/storagelens-sql$"
        if ($sqlContainerId) {
            docker rm -f $sqlContainerId 2>&1 | Out-Null
        }
        docker compose up -d --build
    }

    "up-clean" {
        $sqlContainerId = docker ps -aq --filter "name=^/storagelens-sql$"
        if ($sqlContainerId) {
            docker rm -f $sqlContainerId 2>&1 | Out-Null
        }
        docker compose up -d --build
    }

    default {
        docker compose up -d --build
    }
}