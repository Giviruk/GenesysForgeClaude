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

## Рабочий процесс (обязательно для всех агентов)

Эти пять правил обязательны для каждой задачи в этом репозитории. Не работайте напрямую в `master`.

### 1. Ветка на задачу

- Каждая новая задача — в отдельной ветке.
- Имя ветки: `feature/<краткое-описание-латиницей>` (kebab-case), напр. `feature/character-json-export`.
- Если задача из roadmap — включайте её ID в slug: `feature/u03-character-json-export`.
- Перед созданием ветки: `git fetch`, проверьте `git status` и открытые PR (`gh pr list --state open`).

### 2. Стекинг поверх неслитых PR

- Если есть открытые (неслитые) PR, **новую ветку создавайте от ветки последнего открытого PR**, а не от `master`, и складывайте ветки/PR друг в друга (stacked PRs).
- «Последний» = самый свежий открытый PR (`gh pr list --state open` — верхний по дате).
- PR открывайте с базой на ветку предыдущего PR: `gh pr create --base <ветка-предыдущего-PR>`.
- Когда нижний PR в стеке слит — сделайте rebase вышестоящих веток на новую базу (`master`) и обновите базу их PR.
- Если открытых PR нет — ветка создаётся от свежего `master`.

### 3. PR после завершения задачи

- По завершении задачи открывайте PR (в `master`, либо в базовую ветку стека — см. п.2).
- Описание PR: цель, изменения, тесты, миграции, риски, copyright-note для seed/справочников и **ссылка на файл плана** (п.4) + краткий чеклист.
- Не включайте несвязанные refactor-изменения.

### 4. План выполнения (чтобы задачу мог продолжить другой агент)

- Для каждой задачи заводите файл плана: `roadmap/tasks/<branch-slug>.md` (шаблон — [roadmap/tasks/_TEMPLATE.md](roadmap/tasks/_TEMPLATE.md)).
- План — markdown-чеклист (`- [ ]` / `- [x]`). Отмечайте выполненные пункты **по ходу работы** и коммитьте обновления плана в ту же ветку.
- Файл плана содержит: ссылку на задачу roadmap (U-xx), контекст, пошаговый план, статус, что осталось/блокеры.
- Агент, продолжающий задачу, **сначала читает файл плана**, затем продолжает с первого незакрытого пункта.

### 5. Отметка прогресса в roadmap

- Меняя статус задачи, обновляйте статус-строку в [roadmap/unified-roadmap.md](roadmap/unified-roadmap.md) сразу под заголовком задачи `U-xx`:
  - `**Статус:** ⬜ Todo` → `🚧 In progress` → `✅ Done (PR #N)`.
- Легенда статусов — в шапке `unified-roadmap.md`.
- `✅ Done` ставится только после слияния PR.

## Что нельзя менять без явного запроса

- Game rules и формулы.
- Seed data и legal/copyright policy.
- Auth/JWT behavior.
- Database schema и migrations.
- CI/CD workflow.
- Docker compose и deployment config.
- Package versions и SDK target framework.
- Public API contract.

