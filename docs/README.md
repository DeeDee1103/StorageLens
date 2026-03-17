# StorageLens

![StorageLens Logo](../src/StorageLens.Web/wwwroot/images/storagelens-logo.png)

StorageLens is a SaaS platform for analyzing NAS and cloud-connected storage footprints across distributed environments. It helps organizations understand storage growth, detect duplicate content, and optimize storage cost/risk with workflow-driven scanning and analytics.

## Purpose

StorageLens provides a production-ready architecture for:
- centralized storage inventory
- scan workflow orchestration
- duplicate identification and savings estimation
- operational observability for storage scan pipelines

## High-Level Capabilities

- Multi-service architecture with clear service ownership
- End-to-end scan lifecycle tracking with correlation IDs
- File metadata ingestion and inventory normalization
- Hashing pipeline and duplicate grouping
- KPI and trend analytics for dashboards and reports
- API-first design suitable for UI, automation, and integration

## Architecture Summary

- Frontend: `StorageLens.Web` (Razor Pages)
- Domain services: `Locations`, `ScanJobs`, `FileInventory`, `Duplicates`, `Analytics`
- Workflow services: `Orchestrator`, `Scanner`, `Hashing`
- Shared contracts/infrastructure: `StorageLens.Shared.Contracts`, `StorageLens.Shared.Infrastructure`
- Runtime stack: Docker Compose with SQL Server and RabbitMQ

## Documentation Navigation

## Architecture
- [System Overview](architecture/system-overview.md)
- [Microservices Architecture](architecture/microservices-architecture.md)
- [Infrastructure Architecture](architecture/infrastructure-architecture.md)
- [Data Architecture](architecture/data-architecture.md)
- [Security Architecture](architecture/security-architecture.md)

## Diagrams
- [System Architecture Diagram](diagrams/system-architecture.md)
- [Service Interactions](diagrams/service-interactions.md)
- [Scan Workflow](diagrams/scan-workflow.md)
- [Retry and Resume Workflow](diagrams/retry-resume-workflow.md)
- [Deployment Architecture](diagrams/deployment-architecture.md)

## Technical
- [Technical Specification](technical/technical-specification.md)
- [Technology Stack](technical/technology-stack.md)
- [Database Schema](technical/database-schema.md)
- [API Specification](technical/api-specification.md)
- [Service Contracts](technical/service-contracts.md)
- [Configuration Guide](technical/configuration-guide.md)

## Release History
- [Changelog](CHANGELOG.md)

## Operations
- [Deployment Guide](operations/deployment-guide.md)
- [Docker Deployment](operations/docker-deployment.md)
- [Monitoring and Logging](operations/monitoring-logging.md)
- [Scaling Guide](operations/scaling-guide.md)
- [Disaster Recovery](operations/disaster-recovery.md)

## Developer
- [Developer Guide](developer/developer-guide.md)
- [Local Development](developer/local-development.md)
- [Coding Standards](developer/coding-standards.md)
- [Contributing](developer/contributing.md)

## User Guides
- [User Guide](user-guide/user-guide.md)
- [Dashboard Guide](user-guide/dashboard-guide.md)
- [Scanning Guide](user-guide/scanning-guide.md)
- [Duplicate Analysis Guide](user-guide/duplicate-analysis-guide.md)

## Product
- [Product Overview](product/product-overview.md)
- [Roadmap](product/roadmap.md)

## Documentation Maintenance Policy

Documentation should be updated in the same pull request as source-code changes.

Recommended governance:
- Treat documentation as a release artifact
- Require impacted docs updates in PR reviews
- Require `docs/CHANGELOG.md` updates for user-visible behavior, API, operational, or workflow-impacting changes
- Use CI docs guard (`.github/workflows/docs-guard.yml`) to fail PRs when source/config/workflow/test changes are not reflected in `docs/`
