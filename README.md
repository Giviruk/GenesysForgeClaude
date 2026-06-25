# GenesysForge

Интерактивный лист персонажа для НРИ-систем **Genesys Core** и **Realms of Terrinoth** с переключением между системами.

## Возможности

- Регистрация / авторизация: email+пароль (JWT), вход через Google (опционально), refresh-токены с ротацией, self-service сброс пароля (e-mail-провайдер подключается отдельно)
- Лист персонажа: имя, архетип (раса), карьера, характеристики, раны (HP), стрейн (стамина), защита, поглощение, переносимый вес
- Навыки: дайс-пул, карьерный/некарьерный, ранги, связанная характеристика
- Пирамида талантов с покупкой по правилам Genesys (тиры 1–5, стоимость tier × 5 XP, требование пирамиды, ранговые таланты покупаются на тир выше)
- Героические способности для Realms of Terrinoth
- Инвентарь: состояния «используется / не используется / в рюкзаке», автоматический пересчёт поглощения, защиты, порога переносимого веса (надетая броня: encumbrance −3)
- Кастомный контент через UI: навыки, таланты, предметы, героические способности
- Кампании с join code, заметками, энкаунтерами, Game Table и campaign handbook/content packs; real-time обновления стола/кампании через SignalR
- Бестиарий/NPC, сборка магических действий и печать карточек игровых материалов через browser print
- Deep links / URL-маршруты для персонажей, кампаний, NPC и магии (refresh и прямые ссылки работают)

## Стек

| Слой | Технологии |
|---|---|
| Backend | C# / .NET 10, ASP.NET Core Minimal API, EF Core 10 + Npgsql |
| БД | PostgreSQL 17 |
| Frontend | React 19, Vite, TypeScript; в проде — nginx |
| Тесты | xUnit (domain + api), Vitest (frontend) |
| CI | GitHub Actions: build + тесты на PR и push в master |

## Архитектура (Clean Architecture + CQRS)

```
backend/src/
  GenesysForge.Domain/          — ядро: сущности, enums, value objects, правила Genesys
    Entities/  Enums/  ValueObjects/  Rules/  Exceptions/
  GenesysForge.Application/     — use-cases: CQRS-команды/запросы и хендлеры
    Abstractions/  (ICommand, IQuery, IAppDbContext, ITokenService, …)
    Features/      (Auth, Characters, CustomContent, Reference — Command + Handler на файл)
    Dtos/          (контракты API, один record на файл)
    Common/        (SheetBuilder, CharacterLoader, TalentTierCounter, Mappers)
  GenesysForge.Infrastructure/  — EF Core (Npgsql/InMemory), сид-данные, JWT, хешер паролей
  GenesysForge.Api/             — тонкие minimal-api endpoints поверх хендлеров
backend/tests/                  — GenesysForge.Domain.Tests, GenesysForge.Api.Tests
frontend/                       — React SPA (+ Dockerfile с nginx)
```

Зависимости направлены строго внутрь: `Api → Infrastructure → Application → Domain`.
CQRS без MediatR: лёгкие `ICommandHandler<,>` / `IQueryHandler<,>` через DI.

## Запуск одной командой (Docker)

```bash
docker compose up -d --build
```

Поднимает PostgreSQL, API и фронтенд (nginx). Сайт: **http://localhost:8080**.
Настройки (порт, пароли, JWT-ключ) — через `.env`, см. [.env.example](.env.example).

## Запуск для разработки

```powershell
docker compose up -d postgres                          # только БД
dotnet run --project backend/src/GenesysForge.Api     # API на http://localhost:5080
cd frontend; npm install; npm run dev                  # SPA на http://localhost:5173 (проксирует /api)
```

## Тесты

```powershell
dotnet test backend/GenesysForge.slnx
cd frontend; npm test
```

## Деплой на VPS (автоматический, через GitHub Actions)

Деплой идёт сам при пуше в `master`: workflow [deploy.yml](.github/workflows/deploy.yml) после успешного CI
собирает образы `api`/`web`, публикует их в **GHCR** (`ghcr.io/giviruk/genesysforge-api` и `-web`)
и разворачивает на VPS по SSH прод-стек [docker-compose.prod.yml](docker-compose.prod.yml):
PostgreSQL + API + web (nginx) + **Caddy** с автоматическим HTTPS (Let's Encrypt). Наружу публикуется только Caddy (80/443).

Сейчас разворачивается один private-стек на `PRIVATE_HOSTNAME` в режиме `ContentMode=PrivateFull`.
Публичный `PUBLIC_HOSTNAME` в prod-compose/Caddy пока не подключён; его нужно вернуть отдельным `PublicSafe`-стеком перед публичным запуском.

### Конфигурация — только через GitHub Secrets/Variables (в репозитории секретов нет)

Secrets: `SSH_HOST`, `SSH_USER`, `SSH_PORT`, `SSH_PRIVATE_KEY`, `POSTGRES_PASSWORD`, `JWT_KEY`,
`GHCR_USERNAME`, `GHCR_TOKEN` (с правом `write:packages`), `LETSENCRYPT_EMAIL`, `PRIVATE_OWNER_PASSWORD`.
Variables: `DEPLOY_PATH` (`/opt/genesysforge`), `PRIVATE_HOSTNAME` и др. режимные переменные. `PUBLIC_HOSTNAME` понадобится при добавлении публичного `PublicSafe`-стека.

### Требования к VPS

- Docker Engine + compose-plugin (`curl -fsSL https://get.docker.com | sh`).
- `SSH_USER` имеет доступ на запись в `DEPLOY_PATH`; для swap желателен passwordless `sudo` (иначе шаг swap пропускается).
- Свободны порты `80` и `443` (их займёт Caddy).
- DNS/sslip.io хосты указывают на IP сервера.

### Первый запуск

После настройки secrets/variables запустите workflow вручную: вкладка **Actions → Deploy → Run workflow**
(последующие пуши в `master` деплоят автоматически после CI).

Данные Postgres живут в named-томе `pgdata`. Бэкап:
`docker exec genesysforge-db pg_dump -U genesys genesysforge > backup.sql`.

Схема БД применяется через **EF Core migrations** на старте (`Database.Migrate()`), сид встроенного контента идемпотентен.

## Примечание о данных систем

Сид-данные (архетипы, карьеры, таланты, предметы, героические способности) воспроизводят структуру и правила официальных книг (Genesys CRB, Realms of Terrinoth), но включают сокращённый набор контента; точные значения можно править и расширять кастомным контентом через UI.

## Лицензия и правовая информация

- **Код** — под лицензией [Apache License 2.0](LICENSE).
- **История изменений** — [CHANGELOG.md](CHANGELOG.md) (формат Keep a Changelog).
- **О проекте** — страница «О проекте» доступна в приложении по адресу `/about` (ссылка в футере).

GenesysForge — независимый некоммерческий фан-проект. Он **не аффилирован** с Fantasy Flight Games, Edge Studio или Asmodee и не одобрен ими. «Genesys», «Realms of Terrinoth» и связанные названия — товарные знаки их правообладателей. Проект не содержит оригинальных текстов официальных книг; подробности — в [NOTICE](NOTICE).
