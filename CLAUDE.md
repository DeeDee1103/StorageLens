# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

StorageLens is a .NET 8 microservices app for storage analytics and duplicate detection across local and NAS (UNC) paths. A Razor Pages web front end sits on top of eight backend services. Scanning is read-only: it never changes or deletes the files it scans.

## Commands

```bash
dotnet restore StorageLens.sln
dotnet build StorageLens.sln

# All tests (CI runs Release + coverage; see below)
dotnet test StorageLens.sln

# One test project
dotnet test tests/StorageLens.Services.Orchestrator.Tests

# A single test class or method (xUnit)
dotnet test tests/StorageLens.Services.Duplicates.Tests --filter "FullyQualifiedName~DuplicatesServiceTests"
dotnet test tests/StorageLens.Services.Duplicates.Tests --filter "FullyQualifiedName~DuplicatesServiceTests.<MethodName>"

# Formatting check (CI fails on any diff); drop --verify-no-changes to fix
dotnet format StorageLens.sln --verify-no-changes

# Run one service with hot reload
dotnet watch run --project src/StorageLens.Web
```

To run locally, either start each project with `dotnet run --project src/<Project>`, using the ports below, or run the whole stack with Docker. For Docker, copy `.env.example` to `.env`, then run `docker compose up --build`, or use `scripts/compose-dev.ps1 -Mode up|up-clean|restart-clean|full-recover`. The script fills in a dev SQL password and service API key if they aren't set. Health probe scripts live in `scripts/probe-*.ps1`. Local SQL secrets go in user-secrets via `scripts/set-azure-sql-user-secrets.ps1`, never in appsettings.

| Service | Port | | Service | Port |
|---|---|---|---|---|
| Web | 5001 | | Analytics | 5105 |
| Locations | 5101 | | Scanner | 5106 |
| ScanJobs | 5102 | | Hashing | 5107 |
| FileInventory | 5103 | | Orchestrator | 5108 |
| Duplicates | 5104 | | | |

The compose stack also runs SQL Server (host port 14333), Redis, RabbitMQ (not used by the current workflow) and Jaeger (UI on 16686, OTLP on 4317).

## CI gates (all must pass on PRs to `main`/`develop`)

- **Format and vulnerabilities** (`code-quality.yml`): `dotnet format --verify-no-changes`, plus a failure if any package (including transitive ones) is flagged as vulnerable.
- **Coverage** (`tests-and-coverage.yml`): line coverage must be **≥ 80% in every coverage report**, meaning per test project, not overall. New code without tests can fail the build.
- **Docs guard** (`docs-guard.yml`): any change to `src/`, `tests/`, `infra/`, `scripts/`, workflows, compose, `azure.yaml`, the solution files, `README.md`, or the Copilot/agent files (`.instructions.md`, `.agent.md`, `.prompt.md`, `SKILL.md`, `AGENTS.md`, `copilot-instructions.md`) must also change **some file under `docs/` and `docs/CHANGELOG.md`**. Add the changelog entry under `[Unreleased]`. The root `CHANGELOG.md` doesn't count.
- **Bicep** (`infra-validation.yml`): `az bicep build` must succeed for `infra/main.bicep`.

## Architecture

### Scan workflow (Orchestrator saga)

Web calls `POST /api/orchestrator/scans/start/{locationId}`. The Orchestrator saves a workflow with a correlation ID and a scan job, then puts it on an in-memory `WorkflowQueue`. `WorkflowBackgroundService` picks it up and runs `WorkflowOrchestratorService.ProcessWorkflowAsync`. Up to `WorkflowProcessing:MaxConcurrentWorkflows` workflows run at once.

The steps run in this order: **Scanning → Hashing → DetectingDuplicates → RefreshingAnalytics**. Each step is saved when it finishes. On resume or retry, completed steps are skipped (checkpoint/resume), and failed runs go through `Sagas/WorkflowCompensator`. During the run:
- Scanner enumerates the files and posts metadata batches to FileInventory (`POST /api/files/batch`).
- Hashing pulls pending files from FileInventory and streams a SHA-256 hash for each.
- Duplicates groups files by hash, where a hash shared by more than one file is a duplicate group.
- ScanJobs status and progress are updated at every stage.

The Orchestrator talks to the other services only through named `HttpClient`s (`"locations"`, `"scanjobs"`, `"scanner"`, `"hashing"`, …). These are set up in `Orchestrator/Program.cs` with Polly policies:
- Retries apply **only to GET/HEAD/OPTIONS**; there are no retries for POST/PUT.
- Each client has its own circuit breaker.
- Settings come from the `UpstreamResilience` config, with per-client overrides. Scanner and Hashing have a 4-hour timeout and a single attempt.

### Service boundaries

- Each service has its own `DbContext` and SQL schema (`locations`, `scanjobs`, `fileinventory`, `duplicates`, `analytics`, `scanner`, `hashing`, `orchestration`). Web also has its own `ClientsDbContext`.
- Services never query another service's database; they call that service's HTTP API.
- Web reaches the backend through typed clients in `src/StorageLens.Web/Clients/`.
- Shared request/response DTOs live in `StorageLens.Shared.Contracts`, one file per service (`*Contracts.cs`), written as immutable records. Changing one affects every caller, so check its usages across `src/` before changing its shape.

### Shared.Infrastructure (used by every service's `Program.cs`)

Each service's `Program.cs` follows the same pattern, so copy it when adding a service:
- `KeyVaultIntegration.AddKeyVaultConfiguration` and `AddStorageLensObservability` (OpenTelemetry).
- API-key auth via `ApiKeyAuthenticationHandler`. Services check the `X-API-Key` header against the `ServiceApiKey` config value, and outgoing clients add that header.
- `CorrelationIdMiddleware` on incoming requests and `CorrelationIdDelegatingHandler` on outgoing `HttpClient`s pass the `X-Correlation-ID` header along. Add the handler to any new client.
- `AddStorageLensHealthChecks` / `MapStorageLensHealthChecks`. The Orchestrator probes the other services at `/health/live`.
- `BackgroundDatabaseInitialization.Start<TContext>(app, ...)` runs at startup. For relational databases it applies EF migrations (each service has a `Migrations/` folder), with retries and a fallback. For the in-memory provider it calls `EnsureCreated`. Schema changes need a new EF migration in the owning service.
- Setting `Database:UseInMemory=true` switches a service to the EF in-memory provider, so it runs without SQL Server.

### Tests

The tests use xUnit, coverlet, and the EF InMemory and Sqlite providers. There is one `tests/<Project>.Tests` per `src` project. `tests/StorageLens.Integration.Tests` spins up the services with `WebApplicationFactory<Program>`, which is why each `Program.cs` ends with `public partial class Program { }`.

### Deployment

The app deploys to Azure Container Apps with `azd`; the service map is in `azure.yaml` and the infrastructure in `infra/main.bicep`. Only Web is public; the backend services are internal and find each other through Container Apps FQDNs. Shared build and version settings (currently `0.4.0`) are in `Directory.Build.props`.

## Repo notes

- Several root docs (`README.md`, `AGENTS.md`, `copilot-instructions.md`, `.instructions.md`, `.agent.md`) are UTF-16 encoded. Read them with PowerShell `Get-Content`, since `cat`/`grep` show them garbled.
- Windows needs `git config core.longpaths true`; some paths in the repo are longer than 260 characters.
- More detail lives in `docs/architecture/`, `docs/technical/api-specification.md`, `docs/technical/database-schema.md`, and `docs/operations/deployment-guide.md`.
