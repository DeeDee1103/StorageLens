# Coding Standards

## General

- Follow idiomatic C# and .NET 8 patterns
- Keep files focused and cohesive
- Use meaningful names and immutable DTO contracts where practical

## Service Design

- Respect bounded contexts and service ownership
- Avoid direct database access across services
- Keep API contracts backward compatible whenever possible

## Error Handling

- Use explicit status and progress semantics
- Log actionable context including correlation IDs
- Distinguish recoverable and terminal workflow failures

## Documentation Standards

- Update `docs/` in the same PR as source changes
- Include Mermaid diagrams for workflow/architecture changes
