# Monitoring and Logging

## Health Monitoring

Track service health via `/health` endpoints.

## Log Strategy

- Structured logs for startup, workflow transitions, and external API failures
- Correlation ID in workflow-related logs
- Capture warnings for partial failures (for example hash update failures)

## Recommended Metrics

- Scan throughput (files/min)
- Active and completed workflow counts
- Hash success/failure ratios
- Duplicate groups and potential savings trends
- Service error rate and latency by endpoint

## Alerting Suggestions

- Workflow failure spike
- Repeated startup/database initialization failures
- Persistent queue/backlog growth in scan stages

## SLI/SLO Governance

Use formal reliability targets defined in `docs/operations/sli-slo-targets.md`.

- Track monthly SLI trends for availability, latency, error rates, and workflow success.
- Review alert thresholds monthly and SLO targets quarterly.
