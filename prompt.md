# GenesysForge Implementation Prompt

Ты Senior Full-Stack/DevOps/Product engineer. Работай в текущем репозитории GenesysForge.

## Обязательный старт

1. Прочитай `AGENTS.md`, `docs/ai-context.md`, `docs/current-state.md`.
2. При необходимости читай профильные docs: `architecture`, `domain-model`, `api`, `database`, `frontend`, `testing`, `development-workflow`, `open-questions`.
3. Выполни `git status`; не трогай чужие незакоммиченные изменения.
4. Сначала выведи: текущее состояние, план, затрагиваемые файлы, нужные миграции, секреты/данные от пользователя, риски, какие части делать отдельными PR.

## Общие правила

- Сохраняй архитектуру: .NET Clean Architecture + CQRS, React/TS, EF Core/PostgreSQL, Docker/GitHub Actions.
- Не удаляй существующую функциональность. Не коммить секреты. Схему БД меняй только через EF migrations.
- Обновляй docs при изменении API/БД/domain/архитектуры/workflow.
- Добавляй тесты для business rules, auth/security, API contracts и критичного frontend behavior.
- Если данных нет в коде: `Not found in current codebase`. Если предполагаешь: `Assumption`.
- Не делай один огромный PR: разбивай на этапы.

## Фичи

### 1. Expired JWT

Проверь текущую частичную реализацию и доведи до полного поведения:

- backend возвращает `401` для expired/invalid JWT;
- frontend очищает token, возвращает на login, показывает сообщение “сессия истекла”;
- неверный логин не должен выглядеть как истекшая сессия;
- после logout/401 нет циклов запросов;
- тесты: API client/auth flow/backend auth при необходимости.

### 2. CI/CD deploy на VPS

Изучи `.github/workflows/ci.yml`, Dockerfiles, `docker-compose.yml`, `frontend/nginx.conf`.

Сделай deploy workflow после успешных build/test. Все секреты через GitHub Secrets. Без деплоя, пока не получены данные.

Уже решено:

- HTTPS/TLS нужен.
- Использовать GHCR, не build-only на сервере.
- Деплоить обе версии: `Private` и `Public`.
- VPS очищен от старого проекта; GenesysForge может владеть новым Caddy на host ports `80:80` и `443:443`.
- Старый проект `77-239-101-72.sslip.io` больше не нужно учитывать при деплое.
- Предпочтительные sslip.io hostnames:
  - private: `genesys-private.77-239-101-72.sslip.io`
  - public: `genesys-public.77-239-101-72.sslip.io`
- Private owner:
  - email: `giviruk@gmail.com`
  - display name: `Egor`
  - password должен задаваться только через GitHub Secret/env, не писать пароль в repo или docs.
- Full content loading: private seed created from current seed after extending it to full book content.
- GitHub Secrets/Variables уже заведены; перед деплоем проверить их наличие и спрашивать только отсутствующее/конфликтующее. GitHub не раскрывает значения secrets.
- Найденные Secrets: `SSH_HOST`, `SSH_USER`, `SSH_PORT`, `SSH_PRIVATE_KEY`, `POSTGRES_PASSWORD`, `JWT_KEY`, `PRIVATE_OWNER_PASSWORD`, `GHCR_USERNAME`, `GHCR_TOKEN`, `LETSENCRYPT_EMAIL`.
- Найденные Variables: `APP_MODE_PRIVATE`, `APP_MODE_PUBLIC`, `CONTENT_MODE_PRIVATE`, `CONTENT_MODE_PUBLIC`, `DEPLOY_PATH`, `PRIVATE_HOSTNAME`, `PUBLIC_HOSTNAME`, `PRIVATE_OWNER_EMAIL`, `PRIVATE_OWNER_DISPLAY_NAME`, `PRIVATE_WEB_PORT`, `PUBLIC_WEB_PORT`, `PRODUCTION_BRANCH`.

Reverse proxy требования:

- Новый GenesysForge compose должен включать Caddy service и публиковать только Caddy на `80:80` и `443:443`.
- Private/public web/api/postgres services не публиковать напрямую в интернет; они доступны Caddy по compose service names/internal network.
- Перед запуском проверить, что ports `80` и `443` свободны или заняты ожидаемым GenesysForge Caddy.
- Новый Caddy должен маршрутизировать:
  - `genesys-private.77-239-101-72.sslip.io` -> private web/api stack;
  - `genesys-public.77-239-101-72.sslip.io` -> public web/api stack.
- HTTPS/TLS обслуживает новый Caddy.
- Опционально спросить, нужен ли redirect со старого `77-239-101-72.sslip.io` на private или public hostname.

Проверь и запроси только недостающее или требующее подтверждения:

- соответствие secret/variable names будущему workflow;
- GHCR owner/package names;
- подтвердить deploy dir из `DEPLOY_PATH` (`/opt/genesysforge`) или изменить;
- нужны ли отдельные PostgreSQL databases для private/public;
- финальные internal upstream names/ports для private/public.

Если данных не хватает: подготовь workflow/docs и список `gh secret set ...`, но не деплой.

### 3. Две версии приложения

Нужно поддержать два app profile:

- `AppMode=Private`
- `AppMode=Public`

И два content mode:

- private: `ContentMode=PrivateFull`
- public: `ContentMode=PublicSafe`

Private:

- личный некоммерческий доступ с разных устройств;
- приватный repo/VPS;
- full book content допустим в private DB/private seed;
- публичная регистрация отключена;
- login включен;
- при первом запуске создается owner user из env/secrets;
- owner/admin может создавать пользователей-друзей через защищенный UI/API;
- друзья не имеют admin-доступа;
- owner password не хранить в git; существующего owner не перезаписывать без явного действия.

Public:

- copyright-safe seed;
- регистрация и login работают обычно;
- full descriptions не отдаются;
- показываются safe description или source reference.

Обязательно изолируй private/public seed pipelines, чтобы данные не смешивались.

### 4. Content model и copyright-safe режим

Справочные сущности должны поддерживать:

- stable key/code;
- `NameRu`;
- `NameEn`/original name;
- full description;
- public safe description;
- source book/page/section;
- visibility/content metadata.

`PrivateFull`: API/UI показывают полный контент.  
`PublicSafe`: API/UI не показывают full copyrighted descriptions, но показывают русское название и ссылку “см. книгу, стр. X”.

Рекомендуемая private data папка:

```text
private-content/
  genesys-core.ru.json
  realms-of-terrinoth.ru.json
```

Если private files хранятся в git, задокументируй, что перед публичным открытием repo их нужно удалить/вынести. Если не хранятся — добавь `.gitignore`.

Тесты: `PrivateFull` отдает full content; `PublicSafe` не отдает; private registration disabled; public registration enabled; owner bootstrap; owner can create users; regular user cannot.

### 5. Заметки персонажа

Добавить `CharacterNote`: `Id`, `CharacterId`, `OwnerUserId`, `Title`, `Body`, `CreatedAt`, `UpdatedAt`.

Нужно: EF migration, CRUD API, UI tab/section на листе, ownership checks, API tests на CRUD и изоляцию.

### 6. GM и кампании

Добавить GM/campaign область:

- `Campaign`;
- `CampaignMember` или `CampaignCharacter`;
- GM owner;
- список персонажей кампании;
- campaign notes.

GM создает кампанию, видит персонажей кампании, ведет заметки. Игрок не видит чужие GM notes. Если приглашения велики для первого этапа — пометь `Not implemented yet` и сделай минимальный безопасный сценарий добавления персонажа.

Нужно: backend model/API/migration/tests, frontend GM section, docs.

### 7. Заклинания

Добавить magic/spells section:

- направления/типы заклинаний;
- таблицы дополнительных эффектов;
- переключение разделов через dropdown;
- различия Genesys/Terrinoth, если нужны;
- русские названия;
- full text только через `PrivateFull`;
- public mode показывает safe description/source reference.

Нужно: model/API/migration/tests, UI tab/subsection, responsive tables.

### 8. Русские названия

Все display names на русском:

- skills, talents, archetypes/species, spells, heroic abilities, careers, items.

Не ломай enums/stable keys. API должен отдавать русское display name и original/stable name при необходимости. Seed/import поддерживает русские имена. Не используй machine-translated official descriptions как обход copyright policy.

## Рекомендуемый порядок

1. Expired JWT.
2. CI/CD plan + secrets + deploy workflow.
3. `AppMode` Private/Public + auth policy + owner bootstrap.
4. `ContentMode` + separate private/public seeds + source references + Russian names.
5. Character notes.
6. Campaigns/GM notes.
7. Spells.
8. Private full content import and VPS integration.

## Definition of Done

Для каждой фичи: архитектура соблюдена, миграции есть при изменении БД, auth/ownership защищены, frontend обновлен, тесты добавлены или причина отсутствия названа, docs обновлены, проверки запущены или причина указана.
