# Infrastructure Architecture

## Runtime Components

- Docker Compose orchestrates local/runtime stack
- SQL Server stores service-owned schemas/databases
- RabbitMQ is provisioned for future event-driven integration
- Each service runs as an independent container

## Network and Ports

- Web: `5001`
- Locations: `5101`
- ScanJobs: `5102`
- FileInventory: `5103`
- Duplicates: `5104`
- Analytics: `5105`
- Scanner: `5106`
- Hashing: `5107`
- Orchestrator: `5108`
- SQL Server: `14333`
- RabbitMQ: `5672`, `15672`

## Infrastructure Characteristics

- Stateless service containers
- Persistent SQL volume (`storagelens-sql-data`)
- Configurable via environment variables
- Health-check-compatible runtime endpoints

## Non-Functional Goals

- Service isolation for fault containment
- Horizontal scaling support for compute stages
- Repeatable deployment topology across environments
