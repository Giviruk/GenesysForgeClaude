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
| Backend | C# / .NET 10, ASP.NET Core Minimal API, EF Core 9 + Npgsql |
| БД | PostgreSQL 17 (docker-compose) |
| Frontend | React 19, Vite, TypeScript |
| Тесты | xUnit (domain + api), Vitest (frontend) |
| CI | GitHub Actions: build + тесты на PR и push в master |

## Структура

```
backend/
  src/GenesysForge.Domain/   — движок правил Genesys (чистый C#, без зависимостей)
  src/GenesysForge.Api/      — ASP.NET Core API, EF Core, JWT, сид-данные систем
  tests/GenesysForge.Domain.Tests/
  tests/GenesysForge.Api.Tests/
frontend/                    — React SPA
.github/workflows/ci.yml     — CI pipeline
docker-compose.yml           — PostgreSQL
```

## Запуск

```powershell
# 1. БД
docker compose up -d

# 2. Backend (http://localhost:5080)
dotnet run --project backend/src/GenesysForge.Api

# 3. Frontend (http://localhost:5173, проксирует /api на backend)
cd frontend
npm install
npm run dev
```

## Тесты

```powershell
dotnet test backend/GenesysForge.sln
cd frontend; npm test
```

## Примечание о данных систем

Сид-данные (архетипы, карьеры, таланты, предметы, героические способности) воспроизводят структуру и правила официальных книг (Genesys CRB, Realms of Terrinoth), но включают сокращённый набор контента; точные значения можно править и расширять кастомным контентом через UI.
