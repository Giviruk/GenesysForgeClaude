# AGENTS.md

Главный файл инструкций для AI-агентов, работающих с GenesysForge. Перед задачей прочитайте этот файл и [docs/ai-context.md](docs/ai-context.md).

## Кратко о проекте

GenesysForge — веб-приложение для создания и ведения листов персонажей для Genesys Core и Realms of Terrinoth. В текущем коде уже реализованы регистрация/вход, создание персонажей, справочники, покупка характеристик/навыков/талантов за XP, пирамида талантов, героические способности Terrinoth, инвентарь, экипировка, пересчет производных характеристик и пользовательский контент.

## Фактический стек

- Backend: .NET 10, ASP.NET Core Minimal API, EF Core 10, Npgsql, PostgreSQL 17, JWT Bearer auth.
- Frontend: React 19, TypeScript 6, Vite 8, Vitest, React Testing Library.
- Tests: xUnit для domain/api, Vitest для frontend.
- Infra: Docker, docker compose, nginx для frontend container, GitHub Actions.

## Команды

Полный стек:

```powershell
docker compose up -d --build
```

Backend:

```powershell
dotnet run --project backend/src/GenesysForge.Api
dotnet test backend/GenesysForge.slnx
```

Frontend:

```powershell
cd frontend
npm install
npm run dev
npm run lint
npm test
npm run build
```

## Структура репозитория

- `backend/GenesysForge.slnx` — .NET solution file.
- `backend/src/GenesysForge.Domain` — сущности, enum, value objects, чистые правила.
- `backend/src/GenesysForge.Application` — CQRS handlers, DTO, ports, mappers.
- `backend/src/GenesysForge.Infrastructure` — EF Core, migrations, seed data, JWT/password services.
- `backend/src/GenesysForge.Api` — Minimal API endpoints и middleware ошибок.
- `backend/tests` — xUnit tests.
- `frontend/src` — React SPA.
- `.github/workflows/ci.yml` — CI.
- `docker-compose.yml` — PostgreSQL + API + web.

## Правила для AI-агентов

- Разрешено создавать и редактировать Markdown-документы для документационных задач.
- Не реализуйте новую функциональность, если задача только про документацию.
- Не меняйте бизнес-логику, код приложения, package/csproj/sln/docker/workflow файлы без явного запроса.
- Не удаляйте файлы без явного запроса.
- Не создавайте миграции без явного запроса.
- Не устанавливайте зависимости без явного запроса.
- Перед изменениями проверяйте `git status`; не перетирайте чужие незакоммиченные изменения.
- Если информации нет в коде, пишите `Not found in current codebase`.
- Если делаете предположение, помечайте `Assumption`.

## Copyright ограничения

- Не добавлять оригинальные тексты из Genesys Core Rulebook, Realms of Terrinoth или других официальных книг.
- Не хранить оригинальные описания талантов, способностей, предметов, архетипов и карьер.
- Разрешены структурные данные, числовые параметры, собственные краткие paraphrase-описания и пользовательский контент.
- При работе с `SeedData.cs` особенно проверяйте, что новые описания не копируют official text.

## Архитектурные правила

- Domain не зависит от EF Core, ASP.NET, DI, JSON и HTTP.
- Application содержит use cases и вызывает domain rules.
- Infrastructure реализует Application abstractions.
- API endpoints должны оставаться тонкими: route parsing, auth user id, handler call, response.
- Frontend не должен напрямую вызывать `fetch` из компонентов для backend API; добавляйте методы в `frontend/src/api/client.ts`.
- Source of truth для правил XP, покупок, refund, пирамиды и derived stats — backend domain/application.

## Тестовые требования

Добавляйте/обновляйте тесты, если меняются:

- правила XP, dice pool, derived stats;
- ограничения `IsCreationPhase`;
- покупка/refund характеристик, навыков, талантов;
- пирамида талантов;
- героические способности;
- инвентарь и экипировка;
- ownership и видимость custom content;
- API request/response shape;
- frontend API client или чистые utils.

Документационные изменения не требуют запуска test suite, но требуют самопроверки Markdown и соответствия текущему коду.

## Миграции

- Схема БД находится в EF Core migrations: `backend/src/GenesysForge.Infrastructure/Persistence/Migrations`.
- Любое изменение persistent model требует миграции и обновления [docs/database.md](docs/database.md).
- Не используйте destructive миграции без явного решения.
- Seed должен быть идемпотентным и не трогать custom content (`OwnerUserId != null`).

## Ветки и PR

- Перед созданием ветки проверьте текущую ветку и статус.
- Новые рабочие ветки по умолчанию с префиксом `codex/`, если пользователь не просит иначе.
- PR должен содержать цель, изменения, тесты, миграции, риски и copyright note для справочников/seed.
- Не включайте несвязанные refactor-изменения.

## Что нельзя менять без явного запроса

- Game rules и формулы.
- Seed data и legal/copyright policy.
- Auth/JWT behavior.
- Database schema и migrations.
- CI/CD workflow.
- Docker compose и deployment config.
- Package versions и SDK target framework.
- Public API contract.

