$ErrorActionPreference = 'Stop'

"[1/4] docker compose down --remove-orphans" | Out-File -Encoding utf8 runtime-clean-cycle.log
& docker compose down --remove-orphans *>> runtime-clean-cycle.log

"`n[2/4] docker compose build --no-cache --pull" | Out-File -Encoding utf8 -Append runtime-clean-cycle.log
& docker compose build --no-cache --pull *>> runtime-clean-cycle.log

"`n[3/4] docker compose up -d --force-recreate" | Out-File -Encoding utf8 -Append runtime-clean-cycle.log
& docker compose up -d --force-recreate *>> runtime-clean-cycle.log

"`n[4/4] docker compose ps" | Out-File -Encoding utf8 -Append runtime-clean-cycle.log
& docker compose ps *>> runtime-clean-cycle.log

Write-Host "Clean cycle complete. See runtime-clean-cycle.log"
