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

## Email (сброс пароля)

Письмо со ссылкой сброса пароля отправляет провайдер из секции `Email`:

- `Email__Provider=Logging` (по умолчанию) — реальная отправка не выполняется, ссылка пишется
  в лог API. Подходит для dev/тестов.
- `Email__Provider=Smtp` — отправка через SMTP-relay (MailKit). Параметры: `Email__From`,
  `Email__FromName`, `Email__Smtp__Host`, `Email__Smtp__Port` (587 + STARTTLS по умолчанию;
  для 465 выставьте `Email__Smtp__UseStartTls=false`), `Email__Smtp__Username/Password`.

В prod compose значения берутся из `.env` (`EMAIL_PROVIDER`, `EMAIL_FROM`, `EMAIL_SMTP_*`).
Пошаговый чеклист «что предоставить» — [email-setup-checklist.md](email-setup-checklist.md).
Ссылка строится из `App__BaseUrl` (в prod — из `PRIVATE_HOSTNAME`/`PUBLIC_HOSTNAME`). Токен сброса
живёт в БД только как хеш, действует 1 час, одноразовый; смена пароля отзывает все refresh-сессии.

## PrivateFull / PublicSafe

Production compose поднимает два изолированных стека:

- private: `api` + `postgres`, `Content__Mode=PrivateFull`;
- public: `api-public` + `postgres-public`, `Content__Mode=PublicSafe`.

Public API собирается Docker target `public` с `IncludePrivateContent=false`. Private resources
не встраиваются в public runtime assembly. Оба стека используют отдельные volumes и hostnames.
Public JWT signing key получает отдельный namespace (`JWT_KEY` + public suffix), поэтому private
access tokens не принимаются public API.

## GEN-CONTENT-01 rollout

Apply AddCampaignMembers before enabling account-only joining. PackLegacyCustomContent
assigns legacy ungrouped owner content to personal packs without changing definition IDs.
Before enabling campaign content isolation, tell players that personal packs no longer permit
new purchases while a character belongs to a campaign. The GM connects the original player pack by shared link in the campaign pack panel.
Ownership, definition IDs and existing ranks are preserved; connecting the pack charges no XP.
Do not create replacement copies for existing characters. Author edits propagate to approved packs;
rotating a share token does not revoke existing campaign connections. Existing purchased skill ranks remain on the sheet;
removing the last character no longer leaves the campaign. Exit/removal is an account action.
Migration deployment and service restart are outside the implementation PRs.

Original shared pack connections require a current campaign member (or its GM) as the owner.
On leaving/removal the original pack remains enabled; the GM sees the former owner marker and
disables it manually. Author edits remain allowed and immediately affect connected campaigns.

Stage 5 requires `20261007090302_AddCustomContentChanges` before the updated API is started.
It adds only the journal table and two indexes; existing definitions/imports receive no invented
dates or history. CRUD events start with the new code. GM/account members can inspect connected
pack history from the campaign overview, including disabled packs; owners can inspect their packs
in the personal library. The “changed after connection” badge compares the latest event to the
connection's last enable/disable timestamp. Deployment remains a separate operator action.
