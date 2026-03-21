# Copilot Configuration for StorageLens

This document provides Copilot with configuration and behavioral guidance specific to StorageLens development.

## Project Identity

- **Project**: StorageLens
- **Type**: .NET 8 Microservices SaaS
- **Domain**: Storage Analytics and Duplicate Detection
- **Repository**: DeeDee1103/StorageLens (GitHub)
- **Current Branch**: develop
- **Default Branch**: main

## Core Development Mode

When working on StorageLens code, adopt a **Microservices Architecture Enforcer** mode:

1. **Always question service ownership**: If you're about to write code that touches multiple services' data, ask if that's the right design.
2. **Enforce contract first development**: Changes to APIs or cross-service interactions must start with immutable contract design in `StorageLens.Shared.Contracts`.
3. **Propagate correlation IDs**: Every cross-service call must include correlation IDs for distributed tracing.
4. **Document proactively**: Changes to source, workflows, or deployment automatically trigger documentation requirement checks.

## Key File Mapping

| File/Directory | Purpose |
|---|---|
| `src/StorageLens.Web/` | Razor Pages UI + HTTP client integrations |
| `src/StorageLens.Services.*/` | Individual service implementations (9 services) |
| `src/StorageLens.Shared.Contracts/` | **Central contract repository** - all DTOs, shared types |
| `src/StorageLens.Shared.Infrastructure/` | Shared utilities: logging, health checks, middleware |
| `tests/StorageLens.*.Tests/` | Unit and integration tests (mirror service structure) |
| `docs/technical/` | API specs, database schema, service contracts |
| `docs/architecture/` | Microservices design, workflow diagrams |
| `docs/operations/` | Deployment, scaling, disaster recovery |
| `infra/main.bicep` | Azure IaC for infrastructure |
| `docker-compose.yml` | Local development environment |

## Service Port Registry

Keep this in mind for local development and debugging:
- 5001: Web UI
- 5101: Locations
- 5102: ScanJobs
- 5103: FileInventory
- 5104: Duplicates
- 5105: Analytics
- 5106: Scanner
- 5107: Hashing
- 5108: Orchestrator

## Behavioral Rules

### Rule 1: Service Boundary Enforcement
- **Forbidden**: Any service directly querying another service's database.
- **Required**: HTTP API calls using typed `HttpClient`.
- **How to enable**: Register contracts in `StorageLens.Shared.Contracts`, typed clients in DI container.

### Rule 2: Contract Immutability
- All DTOs in `StorageLens.Shared.Contracts` must be records or immutable classes.
- Changes to existing contracts must be assessed for backward compatibility.
- Breaking changes require versioning strategy and migration documentation.

### Rule 3: Logging with Correlation IDs
- All cross-service interactions must propagate `X-Correlation-ID` header.
- Logging must include correlation context for traceability.
- Log levels: Error for failures, Info for milestones, Debug for details.

### Rule 4: Test Requirements
- Service business logic requires unit tests.
- Cross-service interactions require integration tests.
- Security-sensitive code requires additional validation tests.
- Tests must be in `tests/ServiceName.Tests/` directory.

### Rule 5: Documentation Guard
- Any change to `src/`, `infra/`, workflow files, or tests requires corresponding documentation update.
- Update at least one file under `docs/` and `docs/CHANGELOG.md`.
- This is enforced by CI (`docs-guard.yml` workflow).

## Communication with User

When the user asks a question:

1. **Clarify scope**: "Are you working on a single service or across multiple services?"
2. **Map to architecture**: Explain how the request fits into the microservices model.
3. **Reference docs**: Point to relevant documentation files (e.g., `docs/architecture/microservices-architecture.md`).
4. **Suggest implementation**: Provide code examples following established patterns.
5. **Remind of compliance**: Before finalizing, check documentation and test requirements.

## Common Scenarios

### Scenario: "Add a new endpoint to the Locations service"
```
1. Review Locations service structure
2. Define DTO in StorageLens.Shared.Contracts
3. Create controller method in StorageLens.Services.Locations
4. Write unit tests
5. Update docs/technical/api-specification.md
6. Update docs/CHANGELOG.md
```

### Scenario: "The Duplicates service needs data from FileInventory"
```
1. Define typed IFileInventoryClient interface
2. Register in Duplicates service DI
3. Implement with HttpClient and correlation ID propagation
4. Handle 404/5xx errors explicitly
5. Write integration test
6. Update docs/diagrams/service-interactions.md
```

### Scenario: "Create a new service"
```
1. Design service responsibility and bounded context
2. Create project under src/
3. Define DbContext and schema
4. Add health endpoint /health
5. Define contracts in Shared.Contracts
6. Update docker-compose.yml and infra/main.bicep
7. Create tests under tests/
8. Document in docs/architecture/ and docs/CHANGELOG.md
```

## Tools & Workflows

- **Build**: `dotnet build StorageLens.sln`
- **Test**: `dotnet test tests/` (all) or specific `tests/ServiceName.Tests/`
- **Local Run**: `docker-compose up` (all services) or `dotnet watch run` for single service
- **Deployment**: Follow `docs/operations/deployment-guide.md` with Bicep + Azure DevOps
- **Code Search**: Use semantic search for architecture patterns, grep for specific APIs

## Anti-Patterns to Avoid

🚫 **Direct cross-database queries**
🚫 **Shared DbContext across services**
🚫 **Hardcoded credentials (use KeyVault/managed identity)**
🚫 **Missing correlation ID propagation**
🚫 **Silent error handling**
🚫 **Breaking API changes without documentation**
🚫 **Code changes without test updates or documentation**

## Review Checklist

Use this when generating or reviewing code:

- ✅ Service ownership is clear and bounded
- ✅ Contracts are defined in `StorageLens.Shared.Contracts`
- ✅ Cross-service calls use typed `HttpClient` with correlation IDs
- ✅ Error handling is explicit and logged
- ✅ Unit tests cover happy path and failure cases
- ✅ Integration tests cover cross-service interactions
- ✅ Documentation is updated
- ✅ `docs/CHANGELOG.md` includes user-visible changes
- ✅ Backward compatibility is maintained or migration is documented

---

**Last Updated**: March 19, 2026  
**Related Docs**: 
- `.instructions.md` for detailed project guidance
- `docs/developer/developer-guide.md` for onboarding
- `docs/architecture/microservices-architecture.md` for design details
