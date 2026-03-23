# Developer Guide

## Audience

Engineers implementing features across UI, service APIs, workflow stages, and shared contracts.

## Quick Start with Copilot Agents

StorageLens is enhanced with specialized Copilot agents to assist with development tasks. The agents understand the microservices architecture and can help with:

- **Adding service features** - Ask the Microservice-Developer agent
- **Cross-service integration** - Ask the Cross-Service-Integrator agent
- **API contract design** - Ask the Contract-Steward agent
- **Test and documentation** - Ask the Test-and-Docs-Guardian agent
- **Architecture validation** - Ask the Architecture-Reviewer agent
- **Deployment and Docker** - Ask the DevOps-and-Deployment agent

See [`docs/developer/copilot-agents-guide.md`](copilot-agents-guide.md) for complete agent documentation and usage examples.

**Pro tip**: Just ask Copilot about your task—it will suggest the right agent and guide you through implementation!

## Codebase Layout

- `src/StorageLens.Web`
- `src/StorageLens.Services.*`
- `src/StorageLens.Shared.Contracts`
- `src/StorageLens.Shared.Infrastructure`
- `tests/`

## Data Model Reference

Use [Data Dictionary](../data-dictionary.md) as the source of truth for persisted schemas, key fields, and cross-service identifiers.

For implementation terminology and architecture language, use [Glossary of Terms - Technical](../glossary-terms-technical.md).

## Development Workflow

1. Pull latest `develop`
2. Build solution
3. Run service subset or full compose stack
4. Implement changes with tests
5. Update docs for architecture/API/contract changes
6. Update `docs/CHANGELOG.md`, `docs/DeveloperChecklist.md`, and `docs/developer/developer-guide.md` when process, quality, or workflow behavior changes

## Documentation Update Cadence

Treat documentation as part of the Definition of Done.

- Every PR: update impacted docs and `docs/CHANGELOG.md` in the same PR.
- Monthly: review developer and operations docs for drift.
- Quarterly: review architecture and API versioning docs.

Use `docs/developer/docs-ownership-matrix.md` to determine owners, update triggers, and required review cadence.

## Testing and QA

StorageLens testing and QA now uses an explicit quality gate model:

- Unit and service-level tests must pass locally before commit.
- Workflow-level orchestration paths must include success and failure coverage.
- Coverage artifacts are generated in CI for every push and pull request to `main` and `develop`.

### Local verification commands

Run full local test verification:

```powershell
dotnet test StorageLens.sln --configuration Debug --verbosity minimal
```

Run test verification with coverage output:

```powershell
dotnet test StorageLens.sln --configuration Debug --collect:"XPlat Code Coverage" --results-directory TestResults
```

### CI quality gate

The repository includes a dedicated test and coverage workflow:

- Workflow file: `.github/workflows/tests-and-coverage.yml`
- Workflow name: `quality-gates`
- Required check name: `Build Test Coverage`
- Trigger: `push` and `pull_request` on `main` and `develop`
- Outputs: test results and coverage artifacts uploaded from `TestResults`
- Gate: workflow fails if any generated coverage report has line coverage below `80%`

### Code quality automation

The repository includes automated code quality checks:

- Workflow file: `.github/workflows/code-quality.yml`
- Check name: `Code Quality Checks`
- Enforcement:
   - formatting verification via `dotnet format --verify-no-changes`
   - vulnerable package detection via `dotnet list package --vulnerable --include-transitive`
   - release build verification

### Branch protection setup

Configure branch protection for `main` and `develop` with these required status checks:

- `Build Test Coverage` (from `quality-gates` workflow)
- `Require docs for code changes` (from `Documentation Guard` workflow)

### Coverage targets

Use the following thresholds as release-readiness criteria:

- Business logic: 80%+
- Data access paths: 80%+
- Controllers/API endpoints: 70%+
- Critical workflow orchestration paths: success and failure scenarios required

## Engineering Practices

- Keep service boundaries explicit
- Prefer additive contract changes
- Preserve correlation ID propagation
- Include health and operational considerations in feature design

## Copilot Support

StorageLens is powered by specialized Copilot agents that understand the project's microservices architecture. Two comprehensive guides are available:

1. **[Copilot Agents Guide](copilot-agents-guide.md)** — For developers using agents to get help with tasks
   - 6 specialized agents and their invocation patterns
   - Common development scenarios with step-by-step agent workflows
   - Best practices for agent-assisted development

2. **[Copilot Configuration Reference](copilot-config-reference.md)** — For project maintainers updating Copilot configuration
   - Detailed explanation of each configuration file
   - When and how to update configuration
   - Maintenance best practices

**Pro tip**: Just ask Copilot about your task—it will suggest the right agent and guide you!

## Complete Rebuild Checklist

If you need to rebuild StorageLens from scratch or set up a fresh development environment, see **[Developer Rebuild Checklist](../DeveloperChecklist.md)** for a comprehensive 18-phase checklist covering:
- Environment setup and prerequisites
- All services (8 services with specific requirements)
- Database and infrastructure
- Testing and verification
- Documentation
- Copilot configuration
- Final sign-off criteria

Estimated time: 4-8 hours depending on experience level.

## Documentation Guardrail System

StorageLens uses an automated CI/CD workflow to ensure code changes are always accompanied by documentation updates. For a complete guide on:
- How the guardrail works
- What files trigger documentation requirements
- Which documentation files to update for different change types
- PR workflow with guardrail validation
- Troubleshooting failed guardrail checks

See **[Documentation Guardrail Guide](documentation-guardrail.md)** for detailed information.

## Documentation Governance References

Use these references when making operational or API-impacting changes:

- Incident response: `docs/operations/incident-response-runbook.md`
- Secrets management: `docs/operations/secrets-rotation-runbook.md`
- CI/CD quality controls: `docs/operations/ci-cd-pipeline.md`
- API evolution rules: `docs/technical/api-versioning-strategy.md`
- Changelog consistency template: `docs/CHANGELOG.md` (top section)
- Documentation ownership and review cadence: `docs/developer/docs-ownership-matrix.md`
- Issue triage and release planning: `docs/developer/issue-management-and-release-process.md`
- Reliability targets: `docs/operations/sli-slo-targets.md`
- Project health scorecard: `docs/operations/project-health-dashboard.md`

## Architecture Review and ADR

For major design or cross-service changes:

- Follow `docs/developer/architecture-review-process.md`
- Add ADR entries using `docs/architecture/adr/0000-template.md` when decision impact is significant or hard to reverse

## Onboarding Path

For new contributors and mentoring flow, use:

- `docs/developer/onboarding-checklist.md`

## Documentation Definition of Done

Before requesting review, confirm:

- Documentation changes are included for all behavior/config/API/workflow changes.
- Checklist/process docs are updated when team practices change.
- Changelog entry clearly describes user, operational, or developer impact.

---

## Engineering Practices

### Initialization Pattern

Frontend initialization is organized by feature/module:

- **`site.js`** - Common/global page initialization (theme, search, animations)
- **`profile.js`** - Profile page specific initialization and avatar management
- **Page-specific scripts** - Loaded via `@section Scripts { <script> }` in individual `.cshtml.cs` pages

Each script module is responsible for its own DOM initialization. Do not mix initialization concerns in `site.js`; keep page-specific or feature-specific logic in dedicated modules.
