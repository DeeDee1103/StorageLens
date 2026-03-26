# StorageLens

![StorageLens Logo](../src/StorageLens.Web/wwwroot/images/storagelens-logo.png)

StorageLens is a SaaS platform for analyzing NAS and cloud-connected storage footprints across distributed environments. It helps organizations understand storage growth, detect duplicate content, and optimize storage cost/risk with workflow-driven scanning and analytics.

## Recent Updates (Last 2 Days)


- Global storage distribution data is now fully live:
	- Global View location totals now derive from aggregated FileInventory stats rather than stale location defaults.
	- Analytics now synchronizes per-location `TotalSizeBytes` and `FileCount` values back into Locations metadata.
	- New locations are automatically included in global distribution and map plotting after analytics refresh.
- Marketing and header branding polish:
	- StorageLens logo scale reduced for cleaner visual balance.
	- Header subtitle "Storage intelligence for modern operations" moved directly under the logo with refined typography.
- Browser tab icon alignment:
	- Shared and marketing layouts now use `storagelens-logo-small.png` for both `icon` and `shortcut icon` links.
- Home, dashboard, and navigation UX:
	- `/` and `/Index` now open the marketing-style product home.
	- Global View is the primary operational dashboard entry from the landing experience.
	- The old sidebar Home link pattern has been retired in favor of product-home and dashboard entry points.
- Product-led conversion and reporting UX:
	- The product home includes demo request capture, pilot/demo CTAs, FAQ/contact sections, and quick links into app modules.
	- Reports now provides CSV exports for oldest files, largest files, common file types, and duplicate-heavy locations.
	- Duplicates now opens in Interactive mode by default, with the Original view still available.
- Pricing alignment:
	- Public pricing now reflects the active three-plan catalogue: Starter ($99), Professional ($499), and Enterprise (contact sales).
- Frontend module organization:
	- Profile page initialization logic was split from `site.js` into dedicated `profile.js` for cleaner page-level script boundaries.
- Runtime verification updates:
	- Docker compose stack URLs remain standardized (`web` on `http://localhost:5001`, services on `5101-5108`).
	- Recent local verification confirms web endpoint response with HTTP 200.

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
- [ADR Template](architecture/adr/0000-template.md)

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
- [Data Dictionary](data-dictionary.md)
- [Glossary of Terms - Technical](glossary-terms-technical.md)
- [Terminology Consistency Matrix](technical/terminology-consistency-matrix.md)
- [API Specification](technical/api-specification.md)
- [API Versioning Strategy](technical/api-versioning-strategy.md)
- [Service Contracts](technical/service-contracts.md)
- [Configuration Guide](technical/configuration-guide.md)

## Glossaries
- [Glossary of Terms - Business Users](glossary-terms-business-users.md)
- [Glossary of Terms - Technical](glossary-terms-technical.md)

## Release History
- [Changelog](CHANGELOG.md)

## Reviews
- [Review Refresh (2026-03-20)](reviews/review-refresh-2026-03-20.md)

## Operations
- [Deployment Guide](operations/deployment-guide.md)
- [Docker Deployment](operations/docker-deployment.md)
- [Monitoring and Logging](operations/monitoring-logging.md)
- [SLI/SLO Targets](operations/sli-slo-targets.md)
- [Project Health Dashboard](operations/project-health-dashboard.md)
- [Scaling Guide](operations/scaling-guide.md)
- [Disaster Recovery](operations/disaster-recovery.md)
- [Incident Response Runbook](operations/incident-response-runbook.md)
- [Secrets Rotation Runbook](operations/secrets-rotation-runbook.md)
- [CI/CD Pipeline](operations/ci-cd-pipeline.md)

## Developer
- [Developer Guide](developer/developer-guide.md)
- [Local Development](developer/local-development.md)
- [Coding Standards](developer/coding-standards.md)
- [Contributing](developer/contributing.md)
- [Docs Ownership Matrix](developer/docs-ownership-matrix.md)
- [Architecture Review Process](developer/architecture-review-process.md)
- [Onboarding Checklist](developer/onboarding-checklist.md)
- [Issue Management and Release Process](developer/issue-management-and-release-process.md)

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
