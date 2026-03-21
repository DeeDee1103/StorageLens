---
name: StorageLens Development Helper
description: Specialized skills for StorageLens microservices development, contract management, and architecture compliance.
applyTo: a:/source/_VSCode/StorageLens/**
---

# StorageLens Development Skills

This skill file defines specialized capabilities for working within the StorageLens codebase.

## Skill: Understanding Service Boundaries

**When to use**: When working on a service and uncertain about responsibility or cross-service interaction.

**Provides**:
- Quick lookup of which service owns a given domain (Locations, Duplicates, etc.)
- Identifies cross-service HTTP integration points
- Flags anti-patterns like direct database access across services
- Suggests proper contract definitions and typed client registration

**Example**:
> "Which service should own the file duplication calculation?"  
> Response: Duplicates service owns duplicate group computation; FileInventory provides hash data via HTTP API.

---

## Skill: Contract Design

**When to use**: When designing, modifying, or versioning API contracts and shared DTOs.

**Provides**:
- Reviews existing contracts in `StorageLens.Shared.Contracts`
- Suggests immutable DTO patterns for serialization
- Assesses backward compatibility of API changes
- Recommends versioning strategies for breaking changes
- Ensures correlation ID propagation patterns

**Example**:
> "I need to add a new field to the scan job response."  
> Response: Check current `ScanJobDto`, assess consumer impact, decide if nullable/optional, update docs.

---

## Skill: Cross-Service Communication

**When to use**: When one service needs to call another, or building the orchestration layer.

**Provides**:
- Typed `HttpClient` registration patterns
- Correlation ID propagation across services
- Retry and error handling strategies
- Integration test setup for sync points
- Health check endpoint patterns

**Example**:
> "How do I call the FileInventory service from Duplicates?"  
> Response: Register typed `IFileInventoryClient`, propagate correlation IDs, handle 404/5xx, test integration.

---

## Skill: Documentation Guard Compliance

**When to use**: Before committing or creating a PR; to ensure documentation is updated.

**Provides**:
- Checklist of documentation files affected by the change
- Validates `docs/CHANGELOG.md` includes the update
- Links to documentation files that need modification
- Explains the CI guard enforcement
- Suggests documentation structure and formatting

**Example**:
> "I changed the scan workflow in the Orchestrator. What docs do I need to update?"  
> Response: 
> - `docs/diagrams/scan-workflow.md` (workflow diagram)
> - `docs/architecture/microservices-architecture.md` (interaction changes)
> - `docs/CHANGELOG.md` (summary of change)
> - `docs/technical/service-contracts.md` if API changed

---

## Skill: Testing Patterns

**When to use**: When writing unit tests, integration tests, or setting up test infrastructure.

**Provides**:
- Unit test structure for service logic and controllers
- Mock patterns for HTTP clients and external dependencies
- Integration test setup for cross-service workflows
- Test data builders and fixtures
- Coverage gap identification
- CI/CD test execution commands

**Example**:
> "How do I test the Orchestrator's retry logic?"  
> Response: Mock ScanJobs/FileInventory/Hashing clients, simulate failures, verify retry count/backoff, test success path.

---

## Skill: Microservices Anti-Pattern Detection

**When to use**: When designing features or reviewing code for architectural issues.

**Provides**:
- Identifies cross-database queries and direct schema dependencies
- Flags tight coupling between services
- Detects missing correlation ID logging
- Suggests proper error handling and retry semantics
- Recommends API contract improvements

**Example**:
> [Red Flag] "Let me query the Duplicates schema from the Analytics service to get stats."  
> Response: Anti-pattern! Analytics must call Duplicates HTTP API. This enforces resilience and ownership.

---

## Skill: Local Development & Docker

**When to use**: When setting up local environment, debugging Docker issues, or scaling services.

**Provides**:
- Docker Compose service configuration
- Port mapping and networking setup
- Volume mounting for development
- Health check configuration
- Service startup order and dependencies
- Environment variable management

**Example**:
> "How do I add the new Profiling service to docker-compose?"  
> Response: See docker-compose.yml structure; add service block with image, ports (5109), environment, health check.

---

## Skill: Deployment & Infrastructure

**When to use**: When deploying to Azure or modifying Bicep infrastructure code.

**Provides**:
- Bicep template patterns for services, databases, and networking
- Azure resource naming conventions
- KeyVault integration for secrets
- Managed identity and RBAC setup
- Monitoring and logging configuration
- Scaling and failover strategies

**Example**:
> "I added a new service. What Bicep changes are needed?"  
> Response: Add App Service/Container App resource, SQL schema, networking rules, managed identity, KeyVault access.

---

## Skill: Code Review Checklist

**When to use**: Before submitting a PR or reviewing peer code.

**Provides**:
- Service boundary verification
- Contract immutability and versioning
- Error handling and logging completeness
- Test coverage requirements
- Documentation update checklist
- Performance and security considerations

**Example**:
> Run this checklist before creating a PR:
> - [ ] No cross-service direct DB access?
> - [ ] Contracts immutable and versioned?
> - [ ] Unit tests cover happy path and errors?
> - [ ] Correlation IDs propagated?
> - [ ] docs/ and CHANGELOG.md updated?
> - [ ] Backward compatible or migration documented?

---

## Skill: Workflow Orchestration

**When to use**: When designing or modifying the scan workflow, retry logic, or resume capabilities.

**Provides**:
- State machine patterns for workflow management
- Retry and backoff strategies
- Resume/checkpoint logic for long-running operations
- Error categorization (recoverable vs. terminal)
- Correlation ID threading through workflow stages
- Monitoring and alerting hooks

**Example**:
> "How should the scan workflow handle failures in the Hashing stage?"  
> Response: Categorize error, decide if recoverable (retry Hashing) or terminal (mark job failed), log with correlation ID, expose in ScanJobs API.
