# Синхронизация документации с кодом (u01-docs-sync)

- **Roadmap:** U-01 — Синхронизация документации с кодом (см. [unified-roadmap.md](../unified-roadmap.md))
- **Ветка:** `feature/u01-docs-sync`
- **Базовая ветка:** `master` (открытых PR нет — стека нет)
- **PR:** #<номер> (после создания)
- **Статус:** 🚧 In progress

## Контекст

Документы расходятся с реальным кодом. AI-агенты получают неверный контекст. Подтверждённые
дрейфы (проверено по коду):

- **Refresh-токены** — реализованы (`RefreshTokenService`, `RefreshToken` entity, миграция
  `AddRefreshTokens`, refresh-cookie), но `current-state.md` числит их в *Not implemented*.
- **Password reset** — есть handlers (`RequestPasswordResetHandler`/`ConfirmPasswordResetHandler`),
  `PasswordResetToken` entity, endpoints; e-mail — стаб `LoggingEmailSender`. Должно быть
  *Partially implemented*, а не *Not implemented*.
- **Frontend routing** — есть `frontend/src/router.ts` с `popstate`/`window.location`; claim
  «local React state, no URL routing» вероятно устарел — проверить точный объём.
- **Rate limiting** — действительно отсутствует (соответствует U-05 Todo).
- **Game Table / Magic builder / Campaigns** — уже задокументированы, сверить детали.

Решения по scope (от пользователя): **полный аудит** всех claim против кода; чинить **и смежные
docs**, если в них найдены прямые противоречия коду.

Scope-файлы: `README.md`, `docs/current-state.md`, `docs/feature-roadmap.md`,
`docs/mvp-ux-account-readiness.md`, `docs/api.md` (+ смежные при противоречиях:
`docs/ai-context.md`, `docs/project-overview.md`, `docs/architecture.md`, …).

## План выполнения

- [x] Аудит auth: refresh-токены (✅), password reset (✅, email-стаб), Google OAuth (✅, off), rate limiting (нет — U-05)
- [x] Аудит кампаний/NPC/encounter/Game Table/content packs/SignalR — соответствует коду
- [x] Аудит магии и custom content — соответствует коду
- [x] Аудит frontend routing (`router.ts`) — History API, deep links, wired в `App.tsx`/`AuthPage`
- [x] Сверка `docs/api.md` со списком реальных endpoints — уже актуален, правок не нужно
- [x] Обновить `current-state.md` (Implemented / Partially / Not implemented / Technical risks)
- [x] Обновить `feature-roadmap.md` (отметить готовое: refresh/reset/oauth/deep links/real-time)
- [x] Обновить `mvp-ux-account-readiness.md` (Current state + статусы 1–8 + checklist)
- [x] Обновить `README.md` (возможности: Google/refresh/reset/deep links/real-time)
- [x] Поправить смежные docs: `ai-context.md`, `project-overview.md`, `frontend.md`
- [x] Самопроверка Markdown и соответствия коду (grep-свип на устаревшие фразы)
- [x] Статус в `unified-roadmap.md` обновлён (🚧 In progress)
- [ ] PR открыт

## Что осталось / блокеры

- Открыть PR; после merge — `unified-roadmap.md` U-01 → ✅ Done (PR #N).

## Заметки / решения

- Документация только. Test suite не требуется (AGENTS §Тестовые требования).
- Copyright: docs не содержат оригинальных текстов книг.
- `docs/api.md` оказался уже синхронным с кодом — не трогал.
- Главный дрейф был в `mvp-ux-account-readiness.md` (раздел Current state) и в claim про routing
  в нескольких docs: код имеет refresh-токены, password reset, Google OAuth, History-API routing
  и SignalR real-time, но docs числили их в «Not implemented».
- `rate limiting` действительно отсутствует — оставлено в U-05 как Todo, в docs не объявлено готовым.
