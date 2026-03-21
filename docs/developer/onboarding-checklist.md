# Developer Onboarding Checklist

## Goal

Help new contributors become productive quickly while following StorageLens architecture, quality, and documentation standards.

## Week 1 Checklist

- Clone repository and build solution successfully
- Run core test suites and confirm local setup
- Read developer guide and contributing guide
- Review documentation ownership matrix and changelog policy
- Pair with mentor on one guided code change

## Required Reading

- docs/developer/developer-guide.md
- docs/developer/contributing.md
- docs/developer/docs-ownership-matrix.md
- docs/technical/api-versioning-strategy.md
- docs/operations/ci-cd-pipeline.md

## First Contribution Path

1. Pick a small issue scoped to one service.
2. Create branch from develop.
3. Implement code change with tests.
4. Update impacted documentation and changelog.
5. Submit PR with quality checks passing.

## Mentor Checklist

- Explain service boundaries and ownership model
- Explain workflow orchestration and correlation IDs
- Explain quality gates (tests, coverage, docs guard, code-quality)
- Review first PR for architecture, tests, and docs completeness

## Graduation Criteria

A new contributor is considered onboarded when they have:

- Completed one merged PR with tests and docs updates
- Demonstrated understanding of service boundaries and docs guardrail rules
- Successfully handled one review cycle with actioned feedback
