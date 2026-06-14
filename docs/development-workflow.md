# Development Workflow

## Local startup

Full stack:

```powershell
docker compose up -d --build
```

Backend only with local/default PostgreSQL connection:

```powershell
dotnet run --project backend/src/GenesysForge.Api
```

Frontend dev server:

```powershell
cd frontend
npm install
npm run dev
```

## Running tests

Backend:

```powershell
dotnet test backend/GenesysForge.slnx
```

Frontend:

```powershell
cd frontend
npm run lint
npm test
npm run build
```

## Adding a backend feature

1. Read `AGENTS.md` and `docs/ai-context.md`.
2. Find a similar feature under `GenesysForge.Application/Features`.
3. Put pure rules in `GenesysForge.Domain/Rules`.
4. Add/update DTO in `GenesysForge.Application/Dtos`.
5. Add command/query and handler in the relevant feature folder.
6. Add endpoint mapping only as a thin route layer.
7. Add domain and/or API tests.
8. Update docs if API, domain model or database shape changed.

## Adding a frontend feature

1. Update or add API method in `frontend/src/api/client.ts`.
2. Update API types in `frontend/src/api/types.ts`.
3. Add UI in the relevant page/component.
4. Put pure helper logic in `frontend/src/utils`.
5. Add Vitest/RTL tests for non-trivial behavior.
6. Run lint/test/build.

## Adding a migration

Only when schema changes are explicitly requested.

Expected flow:

```powershell
dotnet ef migrations add <Name> --project backend/src/GenesysForge.Infrastructure --startup-project backend/src/GenesysForge.Api
dotnet test backend/GenesysForge.slnx
```

Then update `docs/database.md`.

## Before commit / PR

- Check `git status`.
- Ensure no unrelated files are included.
- Run relevant tests.
- For code changes, summarize risk and tests.
- For seed/reference changes, explicitly state that no copyrighted descriptions were added.

