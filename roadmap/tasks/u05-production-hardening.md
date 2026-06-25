# Production hardening (u05-production-hardening)

- **Roadmap:** U-05 — Production hardening (см. [unified-roadmap.md](../unified-roadmap.md))
- **Ветка:** `feature/u05-production-hardening`
- **Базовая ветка:** `master` (PR #34 слит, открытых PR нет)
- **PR:** pending
- **Статус:** 🚧 In progress

## Контекст

Усилить production-безопасность и эксплуатационную готовность: защита auth от brute force,
корректные secure cookies за reverse proxy, отказ от слабого JWT key, DB health check,
структурные request logs, автоматические backup/restore/rollback инструкции и отдельный
PublicSafe-стек без embedded private-content.

## План выполнения

- [x] Rate limiting для `/api/auth/*` с отдельными лимитами login/register/reset и refresh
- [x] Forwarded headers + secure refresh-cookie в Production
- [x] Валидация JWT key и CORS-конфигурации при старте
- [x] Структурное request logging на встроенном `ILogger`
- [x] `/api/health` проверяет API и доступность БД
- [x] Тесты rate limit, cookie security, JWT validation и health
- [x] PublicSafe API image без embedded private-content
- [x] Отдельные PublicSafe database/API/web/Caddy services
- [x] Ежедневный backup, restore/rollback scripts и release checklist
- [x] Обновить operator/deploy/API документацию и `.env.example`
- [x] Frontend/public-publish/compose checks
- [x] Повторный backend test после финального CORS/health hardening (62 domain + 132 API, all green)
- [ ] Docker image builds (локальный Docker engine не запущен; проверит PR CI/deploy Buildx)
- [x] Миграции не требуются
- [x] Copyright: public assembly содержит только три safe catalog resource
- [x] Статус roadmap обновлён
- [ ] PR открыт

## Что осталось / блокеры

- Serilog требует новую NuGet-зависимость. До явного разрешения используется встроенный
  структурный `ILogger`; это единственное осознанное отклонение от roadmap scope.
- Docker engine локально недоступен; private/public/web Docker builds должны пройти в GitHub Buildx.
- ~~Повторный backend test заблокирован лимитом внешнего запуска~~ — выполнен локально:
  `dotnet test backend/GenesysForge.slnx` → 62 domain + 132 API тестов, 0 fail.
- Наличие GitHub variable `PUBLIC_HOSTNAME` не удалось проверить из-за лимита внешних операций.
  Без variable безопасный fallback — `public-disabled.localhost`.

## Заметки / решения

- PublicSafe использует отдельную БД, чтобы private/public seed pipelines и пользовательские
  данные не смешивались.
- Health endpoint реализуется без нового пакета через `Database.CanConnectAsync`.
