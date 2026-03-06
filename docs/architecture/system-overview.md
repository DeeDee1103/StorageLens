# System Overview

## Platform Overview

StorageLens is a microservices-based .NET 8 platform that discovers, inventories, and analyzes enterprise file storage. It supports scan orchestration, metadata normalization, duplicate detection, and KPI reporting.

## Problem Statement

Organizations with large NAS and shared storage estates commonly face:
- low visibility into storage composition
- untracked duplicate data growth
- slow and manual scan operations
- fragmented ownership across operations and governance teams

StorageLens addresses this through a workflow-based architecture that captures scan progress, centralizes inventory metadata, and exposes analyzable APIs.

## Core Capabilities

- Storage location registration and lifecycle state tracking
- Scan job creation, stage progression, and progress updates
- File metadata ingestion in batches
- Hashing and duplicate classification workflows
- Aggregated analytics for dashboards and reports
- Operational health endpoints for runtime diagnostics

## System Design Principles

- API-first service boundaries
- Independent service deployability
- Explicit data ownership per bounded context
- Observable workflow progression via correlation IDs
- Production-safe startup and retry behavior for infrastructure dependencies

## Microservices Approach

StorageLens decomposes capabilities by domain and process stage:
- Domain records are owned by dedicated services
- Workflow sequencing is coordinated by Orchestrator
- Data exchange occurs through HTTP contracts in `StorageLens.Shared.Contracts`
- Services remain independently scalable and operable
