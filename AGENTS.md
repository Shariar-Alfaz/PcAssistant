# AGENTS.md

Guidance for AI agents and contributors working in this repository.

## Project Shape

This repository contains two application surfaces:

- `src/PcAssistant`: .NET MAUI Blazor Hybrid client.
- `src/pcAssistantApi`: FastAPI backend.

Treat both as one product, but keep their architecture boundaries explicit. The MAUI client owns presentation, local UI state, device/platform integration, and API communication. The FastAPI backend owns server-side use cases, persistence, integrations, security boundaries, and transactional consistency.

## Core Engineering Rules

- Follow Clean Architecture: dependencies point inward toward domain and application abstractions.
- Follow SOLID: small cohesive types, dependency inversion through interfaces/protocols, explicit contracts, and no hidden global coupling.
- Use Repository Pattern for persistence access.
- Use Unit of Work Pattern for transaction boundaries and coordinated repository commits.
- Keep business rules out of UI components, API route handlers, ORM models, and platform-specific files.
- Prefer dependency injection over static access, service locators, or module-level mutable state.
- Preserve existing behavior unless the task explicitly asks to change it.
- Keep changes scoped. Do not mix refactors, formatting churn, and feature work unless required.
- Add or update tests when changing use cases, repositories, transactions, serialization contracts, or security-sensitive behavior.

## Clean Architecture Layers

Use these conceptual layers on both sides, even if the initial project is small:

1. Domain
   - Entities, value objects, domain services, domain errors, invariants.
   - No framework references.
   - No database, HTTP, UI, filesystem, or platform APIs.

2. Application
   - Use cases, commands/queries, DTOs, validation orchestration, ports/interfaces.
   - Depends on Domain.
   - Defines repository and unit-of-work abstractions.
   - Owns transaction intent, but not database implementation details.

3. Infrastructure
   - Database models/mappings, repository implementations, external API clients, file/device adapters.
   - Depends on Application abstractions and Domain types.
   - Contains implementation details that can be replaced.

4. Presentation
   - MAUI Blazor pages/components and FastAPI route handlers.
   - Thin orchestration only: parse input, call use cases, return view/API output.
   - No direct database calls.
   - No business rules beyond display concerns and transport validation.

## Repository Pattern

- Repositories expose domain-oriented operations, not raw ORM/query-builder details.
- Return domain entities or application DTOs as appropriate; do not leak SQLAlchemy sessions, EF tracking concerns, HTTP clients, or platform handles.
- Keep repository interfaces in the Application layer.
- Keep repository implementations in Infrastructure.
- Prefer intention-revealing methods such as `get_by_id`, `list_active`, `save`, `exists_for_owner` over exposing generic query objects everywhere.
- Avoid repositories becoming catch-all services. If behavior is business logic, move it to a use case or domain service.

## Unit Of Work Pattern

- One use case should normally use one unit of work.
- A unit of work owns transaction lifecycle: begin, commit, rollback, cleanup.
- Repositories used in the same use case should share the same unit-of-work/session context.
- Do not commit from repositories. Commit at the use-case boundary through the unit of work.
- Roll back on exceptions.
- Keep read-only use cases explicit when no transaction is needed.
- In tests, use fake/in-memory unit-of-work implementations for application tests and real database-backed implementations for integration tests.

## .NET MAUI Blazor Hybrid Client

- Keep Razor components focused on rendering and user interaction.
- Move client use cases, API calls, local persistence, device APIs, and platform adapters behind services.
- Register dependencies in `MauiProgram.cs` using clear lifetimes.
- Prefer typed API clients over scattered `HttpClient` calls in components.
- Use async all the way for I/O; never block UI paths with `.Result`, `.Wait()`, or long synchronous work.
- Put platform-specific behavior behind interfaces and implementations under `Platforms/*` only when necessary.
- Keep shared UI state explicit and minimal. Avoid static mutable state.
- Keep CSS/component changes responsive across desktop and mobile form factors.
- Do not put secrets, API keys, or privileged tokens in the client.
- Treat client-side validation as user assistance only; enforce security and invariants on the backend.
- Use the latest supported stable .NET MAUI target for this repo. Avoid preview frameworks unless the project explicitly opts in.

Suggested client structure as the app grows:

```text
src/PcAssistant/PcAssistant/
  Domain/
  Application/
    Abstractions/
    UseCases/
    Dtos/
  Infrastructure/
    Api/
    LocalStorage/
    Platform/
  Components/
    Pages/
    Layout/
```

## FastAPI Backend

- Keep `main.py` as composition/startup only once the API grows.
- Use routers for transport boundaries and use cases for business workflows.
- Use Pydantic schemas for request/response contracts.
- Use SQLAlchemy 2.x style models/sessions when relational persistence is added.
- Use FastAPI dependency injection for request-scoped dependencies such as unit of work, authentication context, and settings.
- Prefer async endpoints and async database/session APIs when the stack is async end-to-end.
- Do not put database logic in route handlers.
- Do not pass ORM models directly as public API contracts unless that is an explicit, reviewed choice.
- Centralize exception mapping so domain/application errors become consistent HTTP responses.
- Validate inputs at the edge, enforce invariants in Domain/Application.
- Use settings from environment/configuration, never hardcoded secrets.
- Add health checks and structured logging before production deployment.

Suggested backend structure as the app grows:

```text
src/pcAssistantApi/
  app/
    main.py
    api/
      routers/
      dependencies.py
    domain/
      entities/
      errors.py
    application/
      abstractions/
      use_cases/
      schemas/
    infrastructure/
      persistence/
        models/
        repositories/
        unit_of_work.py
      settings.py
  tests/
```

## FastAPI Repository And Unit Of Work Example Shape

Keep this shape in mind when adding persistence:

```python
class UnitOfWork(Protocol):
    users: UserRepository

    async def commit(self) -> None: ...
    async def rollback(self) -> None: ...


async def create_user(command: CreateUserCommand, uow: UnitOfWork) -> UserDto:
    user = User.create(command.email, command.display_name)
    await uow.users.add(user)
    await uow.commit()
    return UserDto.from_domain(user)
```

The actual SQLAlchemy implementation should own the session and rollback/close it in a request-scoped dependency.

## Dependency Direction

Allowed:

- Presentation -> Application
- Infrastructure -> Application
- Application -> Domain
- Infrastructure -> Domain

Not allowed:

- Domain -> Application, Infrastructure, Presentation
- Application -> Infrastructure or Presentation
- Repositories committing their own transactions
- UI components or FastAPI routers importing database/session implementation details directly

## Testing Expectations

- Domain tests should be fast and framework-free.
- Application tests should use fake repositories/unit of work.
- Infrastructure tests should use real adapters with isolated test databases/files/services.
- API tests should use FastAPI test clients with dependency overrides.
- MAUI service tests should cover use cases and API-client behavior without requiring the UI host.
- Add regression tests for bugs whenever practical.

## Code Quality

- Use clear names that describe business intent.
- Prefer small methods/classes with one reason to change.
- Prefer explicit error types/results over stringly-typed control flow.
- Avoid premature abstraction, but do introduce interfaces/protocols at architecture boundaries.
- Keep formatting consistent with the existing file.
- Keep comments rare and useful; explain why, not what.
- Run the most relevant build/test command before finishing a change and report anything that could not be run.

## Security And Reliability

- Validate and authorize every backend operation that reads or mutates protected data.
- Keep audit-relevant actions loggable without logging secrets or sensitive payloads.
- Use cancellation/timeouts for network and long-running operations.
- Keep transactional changes idempotent where retry behavior is possible.
- Sanitize file paths and never trust client-provided paths or process commands.
- Handle offline/failed API scenarios gracefully in the MAUI client.

## Agent Workflow

Before editing:

- Inspect current files and patterns.
- Check `doc/` for local vendor documentation before using MCP or web sources when the topic is covered there.
- Identify the affected layer.
- Choose the smallest change that preserves architecture boundaries.

While editing:

- Keep Domain/Application free of framework-specific dependencies.
- Add abstractions in Application and implementations in Infrastructure.
- Use dependency injection to connect layers.

Before finishing:

- Run relevant tests/builds when available.
- Summarize changed files and verification.
- Mention unresolved risks or skipped checks plainly.
