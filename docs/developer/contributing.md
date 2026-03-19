# Contributing

## Branching and PRs

- Branch from `develop`
- Keep PRs focused and reviewable
- Include tests and documentation updates

## Pull Request Checklist

- Solution builds and tests pass
- API or contract changes documented
- Operational impact documented (config, deploy, scale)
- Backward compatibility assessed
- `docs/CHANGELOG.md` updated for user-visible, API, or operational changes

## Documentation Update Requirement

Any change to services, contracts, workflows, or deployment behavior must update relevant files under `docs/` before merge.

Guard enforcement rules:
- If source/config/workflow/test files change, at least one file under `docs/` must also change.
- If source/config/workflow/test files change, `docs/CHANGELOG.md` must be updated in the same PR.
- If `.instructions.md`, `.prompt.md`, `.agent.md`, `SKILL.md`, `copilot-instructions.md`, or `AGENTS.md` change, `docs/CHANGELOG.md` must reflect the updates.

CI enforces this rule through:
- `.github/workflows/docs-guard.yml`

**For detailed information about the documentation guardrail system**, see:
- [`docs/developer/documentation-guardrail.md`](documentation-guardrail.md) — How the guardrail works, enforcement rules, and requirement categories
- [`docs/DeveloperChecklist.md`](../DeveloperChecklist.md) — Phase 10 covers documentation validation

## Copilot Customization

StorageLens is enhanced with specialized Copilot agents and skills:

- **Agents**: 6 specialized personas for different development tasks (microservice work, cross-service integration, contracts, testing/docs, architecture, devops)
- **Skills**: 9 domain-specific capabilities for service boundaries, contract design, testing, deployment, etc.

See [`docs/developer/copilot-agents-guide.md`](copilot-agents-guide.md) for detailed information on:
- How to invoke agents for your task
- Common development scenarios and agent workflows
- Best practices for agent-assisted development

The configuration is automatic—just ask Copilot about your StorageLens task!
