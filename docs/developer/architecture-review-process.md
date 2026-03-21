# Architecture Review Process

## Purpose

This process defines when architecture review is required, who participates, and what must be approved before implementation or merge.

## When Review Is Required

Architecture review is required when a change affects one or more of the following:

- Service boundaries or data ownership
- Cross-service contracts or integration patterns
- Workflow state models and orchestration behavior
- Security posture, trust boundaries, or secrets handling
- Deployment topology or infrastructure dependencies

## Review Roles

- Change Owner: proposes design and drives implementation.
- Architecture Reviewer: validates architecture alignment and anti-pattern risks.
- Contract Steward: validates API/DTO compatibility and versioning.
- Test-and-Docs Guardian: validates coverage and documentation updates.

## Required Artifacts

1. Design summary (problem, options, selected approach)
2. Impact assessment (services, contracts, data, operations)
3. Test strategy (unit, integration, workflow coverage)
4. Documentation update plan
5. ADR entry for major or irreversible decisions

## Review Checkpoints

### Checkpoint 1: Design Approval (before implementation)

- Problem statement and constraints are clear
- Service boundaries and ownership are explicit
- Risks and alternatives are documented

### Checkpoint 2: Implementation Review (during PR)

- Design implemented as approved
- Contracts remain backward-compatible or versioned
- Resilience, observability, and error handling patterns are consistent

### Checkpoint 3: Readiness Review (before merge)

- Tests and quality gates pass
- Required docs are updated
- ADR added or updated when applicable

## ADR Requirement

An ADR is required for changes that are significant, cross-service, or difficult to reverse. Use:

- docs/architecture/adr/0000-template.md

## Exit Criteria

Architecture review is complete only when:

- All checkpoints are satisfied
- Required reviewers have approved
- Required docs and changelog entries are present
