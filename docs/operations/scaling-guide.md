# Scaling Guide

## Scaling Candidates

- `Scanner`: I/O-bound workload, scales with scan concurrency policy
- `Hashing`: CPU and I/O-intensive, candidate for horizontal scaling
- `Orchestrator`: scales by workflow throughput and queue behavior
- `Analytics`: scale reads based on dashboard/report traffic

## Scaling Principles

- Keep services stateless where possible
- Preserve idempotent endpoint behavior
- Use correlation-safe progress update semantics
- Tune worker batch sizes and parallelism settings

## Database Scaling

- Isolate service workloads with per-service databases/schemas
- Apply index tuning based on query profiles
- Archive historical execution records to reduce hot-path load
