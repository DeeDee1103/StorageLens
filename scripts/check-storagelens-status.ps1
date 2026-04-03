$ErrorActionPreference = 'Stop'
$lines = docker ps -a --format "{{.Names}}|{{.Status}}" | Select-String 'storagelens-'
$lines | Out-File -Encoding utf8 "runtime-storagelens-ps-now.txt"
Write-Host "Wrote runtime-storagelens-ps-now.txt"
