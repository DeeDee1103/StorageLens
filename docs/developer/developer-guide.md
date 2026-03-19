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

## Development Workflow

1. Pull latest `develop`
2. Build solution
3. Run service subset or full compose stack
4. Implement changes with tests
5. Update docs for architecture/API/contract changes

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

---

## Engineering Practices

### Initialization Pattern

Frontend initialization is organized by feature/module:

- **`site.js`** - Common/global page initialization (theme, search, animations)
- **`profile.js`** - Profile page specific initialization and avatar management
- **Page-specific scripts** - Loaded via `@section Scripts { <script> }` in individual `.cshtml.cs` pages

Each script module is responsible for its own DOM initialization. Do not mix initialization concerns in `site.js`; keep page-specific or feature-specific logic in dedicated modules.
