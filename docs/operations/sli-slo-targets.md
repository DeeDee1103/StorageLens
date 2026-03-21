# SLI/SLO Targets

## Purpose

Define reliability and performance targets for StorageLens services and workflows.

## Service Level Indicators (SLIs)

- API availability: successful requests / total requests
- API latency: p95 response time by endpoint category
- Workflow success rate: completed workflows / started workflows
- Workflow completion time: p95 end-to-end scan duration
- Error rate: 5xx responses / total responses

## Service Level Objectives (SLOs)

### Core API SLOs (Locations, ScanJobs, FileInventory, Duplicates, Analytics)

- Availability: 99.5% monthly
- p95 latency:
  - Read endpoints: <= 500 ms
  - Write endpoints: <= 1000 ms
- 5xx error rate: <= 1.0% monthly

### Workflow Service SLOs (Orchestrator, Scanner, Hashing)

- Workflow success rate: >= 98% monthly
- p95 workflow completion time: target by file-count tier:
  - Small scans (<= 100k files): <= 30 min
  - Medium scans (<= 1M files): <= 120 min
- Failed workflow retry recovery (resume/retry) success: >= 95%

## Alert Thresholds

- Availability below 99.0% (rolling 24h)
- API p95 latency breach for 3 consecutive intervals
- Workflow failure spike > 5% in 30 minutes
- Backlog growth without drain for > 60 minutes

## Review Cadence

- Monthly: review SLI trends and adjust alert tuning.
- Quarterly: validate SLOs against business needs and scale profile.

## Reporting

- Publish monthly SLI/SLO summary in operational review notes.
- Link major incidents to impacted SLIs and corrective actions.
