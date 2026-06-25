# Operator notes

Эксплуатационные заметки для production. Подробные release/backup/restore/rollback процедуры:
[production-operations.md](production-operations.md).

## Сессии

- Access JWT живёт 30 минут по умолчанию (`Jwt:AccessLifetimeMinutes`,
  env `JWT_ACCESS_LIFETIME_MINUTES`).
- Refresh token передаётся в `HttpOnly`, `SameSite=Lax` cookie `gf_refresh`.
- Cookie всегда `Secure` в Production; forwarded HTTPS scheme принимается от Caddy/nginx.
- Refresh token ротируется при каждом `/api/auth/refresh`; повторное использование старого
  токена отзывает всё семейство.
- Logout отзывает семейство и очищает cookie.

`JWT_KEY` в Production обязателен, должен содержать не менее 32 символов и не может быть
sample/change-me значением. Невалидная конфигурация останавливает API при старте.

## Rate limiting

Auth endpoints ограничиваются по client IP:

- register/login/google/password-reset: 10 запросов в 60 секунд;
- refresh/logout: 30 запросов в 60 секунд;
- providers: 60 запросов в 60 секунд.

Лимиты задаются через `RateLimiting__*` / соответствующие env в `.env.example`.
Ответ при превышении — `429` с `{ "message": "Слишком много запросов..." }`.

## CORS и reverse proxy

Production требует явный `Cors:Origins`: список HTTPS origins через `;`, без path/query.
API принимает `X-Forwarded-For` и `X-Forwarded-Proto` только в topology, где наружу опубликован
только Caddy, а API доступен внутри Docker network.

## Health и логи

`GET /api/health` проверяет подключение EF Core к БД:

- `200 { "status": "ok", "database": "ok" }`;
- `503 { "status": "degraded", "database": "unavailable" }`.

Логирование на **Serilog**: в Production пишется compact JSON в stdout (для агрегаторов логов),
в Development — человекочитаемый текст. `UseSerilogRequestLogging` даёт одну структурную запись
на запрос (method, path, status code, duration) с обогащением `TraceId` и `RemoteIp`; шум фреймворка
(`Microsoft.AspNetCore`) приглушён до Warning. Тела запросов, пароли и токены не логируются.

## PrivateFull / PublicSafe

Production compose поднимает два изолированных стека:

- private: `api` + `postgres`, `Content__Mode=PrivateFull`;
- public: `api-public` + `postgres-public`, `Content__Mode=PublicSafe`.

Public API собирается Docker target `public` с `IncludePrivateContent=false`. Private resources
не встраиваются в public runtime assembly. Оба стека используют отдельные volumes и hostnames.
Public JWT signing key получает отдельный namespace (`JWT_KEY` + public suffix), поэтому private
access tokens не принимаются public API.
