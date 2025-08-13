# Developer Store Backend
A pragmatic .NET microservices backend showcasing clean architecture, messaging, validation, and testing—built to be read quickly and run easily.

See [`challenge.md`](challenge.md) for the original task.

## Skills Evidence Matrix
Concrete, skimmable evidence of key backend skills with direct source links:
- Messaging (Rebus + RabbitMQ): [`SaleEvents.cs`](src/Sales/Sales.Application/IntegrationEvents/SaleEvents.cs), [`IntegrationEventPublisher.cs`](src/Sales/Sales.Infrastructure/Messaging/IntegrationEventPublisher.cs)
- Validation (FluentValidation-style): [`Validators.cs`](src/Sales/Sales.Api/Validation/Validators.cs)
- Error handling & problem details: [`ExceptionHandlingMiddleware.cs`](src/Sales/Sales.Api/Middleware/ExceptionHandlingMiddleware.cs)
- Mapping (AutoMapper profiles): [`SalesMappingProfile.cs`](src/Sales/Sales.Api/Features/Sales/SalesMappingProfile.cs)
- Pagination DTO: [`PaginatedResponse.cs`](src/Sales/Sales.Api/Common/PaginatedResponse.cs)
- Domain rules (Sales): [`Domain.cs`](src/Sales/Sales.Domain/Domain.cs)
- Auth & security building blocks: [`Security.cs`](src/Shared/Shared.Common/Security/Security.cs), [`Infrastructure.cs`](src/Auth/Auth.Api/Infrastructure/Infrastructure.cs)
- Test strategy (unit/integration/functional): [`Sales.UnitTests/`](tests/Sales.UnitTests/), [`Sales.IntegrationTests/`](tests/Sales.IntegrationTests/), [`Sales.FunctionalTests/`](tests/Sales.FunctionalTests/)

## Tech Stack
- .NET (ASP.NET Core Web API)
- Clean Architecture: API → Application → Domain → Infrastructure → Shared
- Messaging: Rebus with RabbitMQ
- Data Access: EF Core (DbContext, configurations)
- Object Mapping: AutoMapper
- Validation: FluentValidation-style validators and pipeline
- Auth/Security: JWT, BCrypt-based hashing, shared security utilities
- Testing: xUnit with unit, integration, and functional layers
- Containers: Docker + docker-compose

## Project Structure (high-level)
```
src/
├─ Auth/
│  └─ Auth.Api/                 # Authentication service (JWT issuing, infra)
├─ Sales/
│  ├─ Sales.Api/                # Public HTTP API (controllers, middleware, mapping, validation)
│  ├─ Sales.Application/        # Use cases, integration events, ports
│  ├─ Sales.Domain/             # Core domain model and rules
│  └─ Sales.Infrastructure/     # EF, messaging (Rebus), repositories
└─ Shared/
   └─ Shared.Common/            # Cross-cutting (security, auth helpers, etc.)

tests/
├─ Sales.UnitTests/
├─ Sales.IntegrationTests/
└─ Sales.FunctionalTests/

docker-compose.yml               # Local infra (RabbitMQ, DB, etc.)
scripts/                         # Dev helpers (e.g., E2E flow)
```

## Architecture & Cross-Cutting
- APIs: Thin controllers expose application use-cases and return consistent problem details via middleware.
- Application: Orchestrates business use-cases and publishes integration events (outbox-friendly design).
- Domain: Encapsulates rules and invariants; models kept persistence-agnostic.
- Infrastructure: EF Core, repositories, and Rebus-based integration event publisher to RabbitMQ.
- Messaging: Sales publishes domain events as integration events; other services can subscribe via Rebus.
- Validation: Request validators ensure inputs; pipeline guards use-case execution with clear error messages.

## Run Locally (fast)
Prerequisites: Docker + Docker Compose, .NET SDK installed.

1) Start infra
   - docker compose up -d

2) Build solution
   - dotnet build [`DeveloperStore.sln`](DeveloperStore.sln)

3) Run APIs
   - dotnet run --project src/Auth/Auth.Api/Auth.Api.csproj
   - dotnet run --project src/Sales/Sales.Api/Sales.Api.csproj

4) Try the endpoints
   - Auth: [`Auth.Api.http`](src/Auth/Auth.Api/Auth.Api.http)
   - Sales: [`Sales.Api.http`](src/Sales/Sales.Api/Sales.Api.http)

## Tests
- Run all tests:
  - dotnet test
- Test folders are organized by layer:
  - Unit: [`Sales.UnitTests/`](tests/Sales.UnitTests/)
  - Integration: [`Sales.IntegrationTests/`](tests/Sales.IntegrationTests/)
  - Functional: [`Sales.FunctionalTests/`](tests/Sales.FunctionalTests/)

## Messaging (Rebus + RabbitMQ)
- Integration events live in: [`IntegrationEvents/`](src/Sales/Sales.Application/IntegrationEvents/)
- Publisher implementation: [`IntegrationEventPublisher.cs`](src/Sales/Sales.Infrastructure/Messaging/IntegrationEventPublisher.cs)
- Compose services with RabbitMQ via [`docker-compose.yml`](docker-compose.yml)

## Developer UX
- Global exception handling: [`ExceptionHandlingMiddleware.cs`](src/Sales/Sales.Api/Middleware/ExceptionHandlingMiddleware.cs)
- Paginated responses and mapping helpers for simple, consistent APIs.
- Helper script for quick sales flow testing: [`test-sales.bat`](scripts/test-sales.bat)
