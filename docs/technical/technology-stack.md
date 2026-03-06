# Technology Stack

## Overview

StorageLens is built as a .NET 8 microservices solution with a Razor Pages web frontend, API-first service boundaries, and containerized local deployment.

## Core Platform

- Runtime: .NET 8
- Language: C# 12
- Web framework: ASP.NET Core
- UI framework: Razor Pages + Bootstrap 5
- API style: REST over HTTP + JSON
- Messaging: RabbitMQ (service workflow/event integration)
- Data platform: SQL Server
- Containerization: Docker + Docker Compose

## Service-Level Stack

### `StorageLens.Web`
- ASP.NET Core Razor Pages
- Bootstrap 5 + Bootstrap Icons
- Chart.js for dashboard visualization
- Typed `HttpClient` integrations for backend services

### `StorageLens.Services.Locations`
- ASP.NET Core Web API
- Entity/data access layer for location metadata
- CRUD endpoints for location lifecycle

### `StorageLens.Services.ScanJobs`
- ASP.NET Core Web API
- Scan job state tracking and progress updates
- Workflow status APIs consumed by UI and orchestrator

### `StorageLens.Services.Orchestrator`
- ASP.NET Core Web API
- Coordinates cross-service scan workflow
- Starts/monitors scan pipeline phases

### `StorageLens.Services.Scanner`
- ASP.NET Core background/service APIs
- File system traversal (local/UNC targets)
- Emits file metadata for inventory ingestion

### `StorageLens.Services.FileInventory`
- ASP.NET Core Web API
- Stores and serves normalized file metadata
- Filtering and inventory query endpoints

### `StorageLens.Services.Hashing`
- ASP.NET Core Web API/background worker
- File hashing workflows for duplicate detection support

### `StorageLens.Services.Duplicates`
- ASP.NET Core Web API
- Duplicate group aggregation and savings insights

### `StorageLens.Services.Analytics`
- ASP.NET Core Web API
- Dashboard/report metrics aggregation

### Shared Projects
- `StorageLens.Shared.Contracts`: DTOs and cross-service contracts
- `StorageLens.Shared.Infrastructure`: shared infrastructure helpers/utilities

## Development Tooling

- SDK/build: `dotnet` CLI
- Solution layout: multi-project `.sln` with per-service `.csproj`
- Local orchestration: `docker-compose.yml`
- Source control: Git + GitHub pull requests

## Configuration Model

- Environment-specific settings via `appsettings.json` and `appsettings.Development.json`
- Service endpoint wiring via typed options + DI
- Container/runtime configuration through Docker files and compose definitions

## Notes

- Keep this document updated whenever services, frameworks, messaging infrastructure, or data/storage technologies change.
- Update `docs/CHANGELOG.md` in the same change set whenever stack changes are user-visible or operationally significant.
