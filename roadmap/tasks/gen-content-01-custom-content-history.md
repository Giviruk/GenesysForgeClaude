# GEN-CONTENT-01 — даты и журнал кастомного контента (этап 5)

- **ТЗ:** [account-campaign-content.md](../../docs/account-campaign-content.md), этап 5, редакция ca41d13.
- **Ветка:** `feature/gen-content-01-custom-content-history`
- **База:** `feature/gen-content-01-campaign-content-access`, PR #268.
- **Статус:** исправления ревью 08.10.2026 реализованы для [PR #269](https://github.com/Giviruk/GenesysForgeClaude/pull/269); актуальные результаты CI — [GitHub checks](https://github.com/Giviruk/GenesysForgeClaude/pull/269/checks). Слияние не выполнять.

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

## Ревью 08.10.2026

В [#268](https://github.com/Giviruk/GenesysForgeClaude/pull/268#issuecomment-6064627720)
предыдущие исправления приняты, новых требований нет. В
[#269](https://github.com/Giviruk/GenesysForgeClaude/pull/269#issuecomment-6064628435)
нужно исключить ложную метку у собственного контента мастера и скрыть отсутствующие даты.
Это продолжение этапа 5 в его ветке; computed DTO field не требует изменения БД.

- [x] ChangedAfterConnection вычисляется сервером по владельцу относительно мастера кампании.
- [x] UI использует серверный флаг; даты и разделители показываются только при наличии даты.
- [x] Регрессии создания/правки набора мастера и правки исходного набора игрока; API/client/UI/E2E.
- [x] Обновить ТЗ/API/context/database/operator notes/progress и подготовить исправления для #269.
- [x] Локальные API/frontend/E2E, build/lint и отсутствие pending model changes проверены.

Публикация исправлений и ответы ревьюеру отслеживаются в комментариях #268/#269;
CI финальной ревизии — в GitHub checks. Слияние не выполнять.

Проверки ревью: 17 API integration tests (CustomContentChangeTests/HomebrewPackTests), все 504
frontend-теста, build/lint, EF без pending model changes. Все 9 PublicSafe Chromium E2E прошли
локально с production frontend, InMemory API и действующим rate limiting; история проверена визуально.
Первая локальная попытка обнаружила потерю долей миллисекунды при сравнении дат через JavaScript:
создание проверяет `>=`, а отдельная правка мастера по-прежнему проверяет `>` и отсутствие метки.
Новая миграция не требуется. Полные backend/PostgreSQL проверки выполняет CI финального head.

## Остаток / блокеры

До ревью 08.10.2026: 759 domain + 924 API, 502 frontend; build/lint; EF без pending model changes,
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
