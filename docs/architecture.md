# Architecture

## Общая схема

```text
frontend React SPA
  -> /api/* through Vite proxy or nginx
GenesysForge.Api
  -> Application command/query handlers
  -> Domain rules/entities
  -> Infrastructure EF Core/PostgreSQL/auth services
```

Backend следует Clean Architecture + CQRS без MediatR. Фактическая зависимость проектов:

```text
Api -> Infrastructure -> Application -> Domain
```

## Backend

```text
backend/src/
  GenesysForge.Domain/
    Entities, Enums, Exceptions, Rules, ValueObjects
  GenesysForge.Application/
    Abstractions, Common, Dtos, Exceptions, Features
  GenesysForge.Infrastructure/
    Auth, Persistence
  GenesysForge.Api/
    Endpoints, Program.cs
```

### Domain

Хранит сущности и чистые правила:

- `GenesysRules`
- `PurchaseValidator`
- `SheetCalculator`

Здесь находятся формулы XP, dice pool, talent pyramid, derived stats и item load.

### Application

Хранит use cases:

- `Features/Auth`
- `Features/Characters`
- `Features/CustomContent`
- `Features/Reference`

Common services/helpers:

- `CharacterLoader`
- `SheetBuilder`
- `TalentTierCounter`
- `Mappers`

Application работает через `IAppDbContext`, `ITokenService`, `IPasswordHasherService`.

### Infrastructure

Хранит доступ к данным и внешние реализации:

- `AppDbContext`
- EF Core migrations
- `SeedData`
- JWT token service
- password hasher service

`InitializeDatabase()` применяет migrations и seed при старте.

### API

`Program.cs` настраивает:

- Application/Infrastructure DI;
- JWT Bearer authentication;
- authorization;
- CORS;
- JSON enum converter;
- OpenAPI;
- exception mapping.

Endpoints:

- `AuthEndpoints`
- `ReferenceEndpoints`
- `CharacterEndpoints`
- `CustomContentEndpoints`

## Frontend

```text
frontend/src/
  api/
  components/
  pages/
  utils/
  auth.tsx
  auth-context.ts
  App.tsx
  main.tsx
```

Routing фактически реализован не через router library, а через React state:

- без token показывается `AuthPage`;
- после входа показывается `CharactersPage`;
- выбранный `characterId` переключает UI на `SheetPage`.

State management:

- auth state in `AuthProvider`;
- page/component local state;
- no external state manager.

## Frontend/backend interaction

- Frontend API client: `frontend/src/api/client.ts`.
- Types: `frontend/src/api/types.ts`.
- Dev proxy configured by Vite.
- Production web container serves static build through nginx and proxies API to backend.

## Infrastructure

- PostgreSQL 17 container.
- API container built from `backend/Dockerfile`.
- Web container built from `frontend/Dockerfile`.
- `docker-compose.yml` wires `postgres`, `api`, `web`.
- CI: `.github/workflows/ci.yml` builds/tests backend and lint/tests/builds frontend.

## Где находится бизнес-логика

- Pure game rules: `GenesysForge.Domain/Rules`.
- Character use cases: `GenesysForge.Application/Features/Characters`.
- Custom content rules: `GenesysForge.Application/Features/CustomContent`.
- Sheet assembly: `SheetBuilder`.
- Ownership/data loading: `CharacterLoader` and handlers.

## Где находится доступ к данным

- EF Core context: `Infrastructure/Persistence/AppDbContext.cs`.
- Migrations: `Infrastructure/Persistence/Migrations`.
- Seed: `Infrastructure/Persistence/SeedData.cs`.

## Где находятся тесты

- Domain tests: `backend/tests/GenesysForge.Domain.Tests`.
- API tests: `backend/tests/GenesysForge.Api.Tests`.
- Frontend tests: `frontend/src/**/*.test.ts`.

