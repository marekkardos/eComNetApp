# Clean Architecture Review Skill

## Layer Dependencies (strict)
```
Presentation → Application → Domain
Infrastructure → Application → Domain
```
- Domain has ZERO external dependencies
- Application defines interfaces, Infrastructure implements
- Never reference Infrastructure from Application

## Domain Layer
- Entities encapsulate business rules
- Value Objects for concepts without identity
- Domain Events for cross-aggregate communication
- No anemic models (behavior belongs with data)
- Rich validation in constructors/factory methods
- No framework dependencies (EF attributes, etc.)

## Application Layer
- Use cases as Commands/Queries (CQRS if applicable)
- DTOs for data crossing boundaries
- Interfaces for external dependencies
- Validation at entry points
- No business logic leakage from Domain

## Infrastructure Layer
- Repository implementations
- External service integrations
- Framework-specific code isolated here
- Configuration and DI setup

## Presentation Layer
- Thin controllers (orchestration only)
- No business logic in controllers
- Request/Response models separate from DTOs
- Proper error mapping to HTTP status

## Common Violations
- DbContext injected into controllers
- Business logic in controllers
- Domain entities exposed in API responses
- Infrastructure types in Application layer
