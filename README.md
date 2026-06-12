# GenesysForge

Интерактивный лист персонажа для НРИ-систем **Genesys Core** и **Realms of Terrinoth** с переключением между системами.

## Возможности

- Регистрация / авторизация (JWT)
- Лист персонажа: имя, архетип (раса), карьера, характеристики, раны (HP), стрейн (стамина), защита, поглощение, переносимый вес
- Навыки: дайс-пул, карьерный/некарьерный, ранги, связанная характеристика
- Пирамида талантов с покупкой по правилам Genesys (тиры 1–5, стоимость tier × 5 XP, требование пирамиды, ранговые таланты покупаются на тир выше)
- Героические способности для Realms of Terrinoth
- Инвентарь: состояния «используется / не используется / в рюкзаке», автоматический пересчёт поглощения, защиты, порога переносимого веса (надетая броня: encumbrance −3)
- Кастомный контент через UI: навыки, таланты, предметы, героические способности

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

## Деплой на VPS

Нужен Linux-сервер с Docker (Engine + compose-plugin). Шаги:

```bash
# 1. Установить Docker (Ubuntu/Debian)
curl -fsSL https://get.docker.com | sh

# 2. Получить код
git clone https://github.com/Giviruk/GenesysForgeClaude.git
cd GenesysForgeClaude

# 3. Настроить секреты — ОБЯЗАТЕЛЬНО смените значения
cp .env.example .env
nano .env        # POSTGRES_PASSWORD, JWT_KEY (openssl rand -base64 48), WEB_PORT=80
                 # POSTGRES_PORT наружу на VPS лучше не публиковать — удалите строку ports у postgres
                 # или закройте порт фаерволом

# 4. Запуск
docker compose up -d --build

# 5. Обновление до новой версии
git pull && docker compose up -d --build
```

Данные Postgres живут в named-томе `pgdata` и переживают пересборку контейнеров.
Бэкап: `docker exec genesysforge-db pg_dump -U genesys genesysforge > backup.sql`.

**HTTPS.** Самый простой способ — поставить перед `web` реверс-прокси с автоматическим TLS,
например [Caddy](https://caddyserver.com/): на хосте `caddy reverse-proxy --from example.com --to localhost:8080`,
либо добавить caddy-сервис в compose. Альтернатива — nginx + certbot.

**Замечание о схеме БД.** Сейчас схема создаётся при старте через `EnsureCreated` и сид встроенного контента.
При эволюции схемы в проде стоит перейти на EF Core Migrations (`dotnet ef migrations add … && db.Database.Migrate()`).

## Примечание о данных систем

Сид-данные (архетипы, карьеры, таланты, предметы, героические способности) воспроизводят структуру и правила официальных книг (Genesys CRB, Realms of Terrinoth), но включают сокращённый набор контента; точные значения можно править и расширять кастомным контентом через UI.
