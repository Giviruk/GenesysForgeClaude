# GEN-CONTENT-01 — даты и журнал кастомного контента (этап 5)

- **ТЗ:** [account-campaign-content.md](../../docs/account-campaign-content.md), этап 5, редакция ca41d13.
- **Ветка:** `feature/gen-content-01-custom-content-history`
- **База:** `feature/gen-content-01-campaign-content-access`, PR #268.
- **Статус:** реализация завершена, [PR #269](https://github.com/Giviruk/GenesysForgeClaude/pull/269); слияние не выполнять.

## Контекст / решения

Автор может изменять оригинальный подключённый набор; правка действует сразу. Требуется
прозрачность для мастера и участников: история «было → стало» и даты правок, без запрета
изменений, версий или снимков листов. После ухода автора набор остаётся включённым до решения мастера.
Новая таблица журнала и миграция без backfill прямо разрешены пользователем в этапе 5 ТЗ.

## План

- [x] Прочитать ТЗ/ревью/инструкции, проверить status/fetch/PR и создать ветку от #268.
- [x] CustomContentChange, EF mappings/миграция AddCustomContentChanges без backfill.
- [x] Общий diff DTO и запись Create/Update/Delete для шести типов в том же SaveChanges.
- [x] Доступ к истории для владельца либо участников кампании с подключённым набором.
- [x] CustomLastEditedAt, LastChangedAt/ConnectedAt и чтение списка наборов участниками.
- [x] Даты записей, история в кампании/личной библиотеке, поля «было → стало» с метками.
- [x] Backend/frontend регрессии, полные тесты, build/lint, EF/SQL проверки.
- [x] Документация/progress, отдельный stacked PR и [ответ в #268](https://github.com/Giviruk/GenesysForgeClaude/pull/268#issuecomment-6034895156).
- [x] Запустить CI PrivateFull/PublicSafe: backend/frontend, PostgreSQL миграции, E2E;
  актуальные результаты — [GitHub checks](https://github.com/Giviruk/GenesysForgeClaude/pull/269/checks).

## Остаток / блокеры

Проверки: 759 domain + 924 API, 502 frontend; build/lint; EF без pending model changes,
SQL миграции содержит только создание таблицы/двух индексов. Chromium E2E изменения исходного
таланта и просмотра журнала участником прошёл локально (InMemory) и на PostgreSQL в обоих
режимах в CI 37599653863. Backend/frontend и обе миграции того же CI успешны. Старый PublicSafe
password-reset smoke получил 429: лишний четвёртый аккаунт нового сценария превысил общий
auth quota. Сценарий использует три нужных роли; все 9 PublicSafe smoke прошли локально с
действующим rate limiting и production frontend. Финальные CI результаты фиксируются в PR.
Миграция не применялась к production.

Реализация завершена; review/merge и production deployment остаются отдельными действиями.
Слияние выполнять нельзя по указанию пользователя. Другие пользовательские
untracked-файлы оставлены без изменений. Seed/copyright policy, XP/formулы и JWT не меняются.
