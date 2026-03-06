# Local Development

## Prerequisites

- .NET 8 SDK
- Docker Desktop
- PowerShell or Bash

## Build and Test

```bash
dotnet clean StorageLens.sln
dotnet build StorageLens.sln
dotnet test StorageLens.sln
```

## Run with Docker Compose

```bash
docker compose up -d --build
docker compose ps -a
```

## Run Services Directly

```bash
dotnet run --project src/StorageLens.Services.Locations
dotnet run --project src/StorageLens.Services.ScanJobs
dotnet run --project src/StorageLens.Services.FileInventory
dotnet run --project src/StorageLens.Services.Duplicates
dotnet run --project src/StorageLens.Services.Analytics
dotnet run --project src/StorageLens.Services.Scanner
dotnet run --project src/StorageLens.Services.Hashing
dotnet run --project src/StorageLens.Services.Orchestrator
dotnet run --project src/StorageLens.Web
```

## Local URL Map

- Web `http://localhost:5001`
- Services `http://localhost:5101` to `http://localhost:5108`
