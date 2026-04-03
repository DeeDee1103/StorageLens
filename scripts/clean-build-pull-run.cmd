@echo off
setlocal
cd /d "%~dp0.."

echo [1/5] docker compose down --remove-orphans> runtime-clean-build-run-cmd.log
docker compose down --remove-orphans >> runtime-clean-build-run-cmd.log 2>&1
if errorlevel 1 goto :fail

echo.>> runtime-clean-build-run-cmd.log
echo [2/5] docker compose pull>> runtime-clean-build-run-cmd.log
docker compose pull >> runtime-clean-build-run-cmd.log 2>&1
if errorlevel 1 goto :fail

echo.>> runtime-clean-build-run-cmd.log
echo [3/5] docker compose build --no-cache --pull>> runtime-clean-build-run-cmd.log
docker compose build --no-cache --pull >> runtime-clean-build-run-cmd.log 2>&1
if errorlevel 1 goto :fail

echo.>> runtime-clean-build-run-cmd.log
echo [4/5] docker compose up -d --force-recreate>> runtime-clean-build-run-cmd.log
docker compose up -d --force-recreate >> runtime-clean-build-run-cmd.log 2>&1
if errorlevel 1 goto :fail

echo.>> runtime-clean-build-run-cmd.log
echo [5/5] docker compose ps>> runtime-clean-build-run-cmd.log
docker compose ps >> runtime-clean-build-run-cmd.log 2>&1
docker compose ps --format json > runtime-clean-build-run-cmd-ps.json
if errorlevel 1 goto :fail

echo SUCCESS> runtime-clean-build-run-cmd.status
exit /b 0

:fail
echo FAILED> runtime-clean-build-run-cmd.status
exit /b 1
