# Campaign overview dashboard (campaign-overview-dashboard)

- **Roadmap:** вне unified roadmap — UI redesign
- **Ветка:** `feature/campaign-overview-dashboard`
- **Базовая ветка:** `master`
- **PR:** #79
- **Статус:** 🚧 In progress

## Контекст

Переделать вкладку «Обзор» в разделе кампании по локальному прототипу `docs/gm-dashboard-prototype.html`.

Затрагиваемые зоны: `frontend/src/pages/CampaignsPage.tsx`, стили frontend, тесты страницы кампаний.

## План выполнения

- [x] Синхронизировать `master`, проверить открытые PR и создать ветку.
- [x] Разобрать текущую вкладку «Обзор» и доступные API-данные кампании.
- [x] Перенести визуальную структуру прототипа на реальные данные кампании без изменения backend.
- [x] Адаптировать responsive-раскладку и состояния пустых данных.
- [x] Обновить/добавить frontend tests.
- [x] Запустить frontend проверки.
- [x] Открыть PR.

## Что осталось / блокеры

Нет.

Визуальная проверка через e2e не запускалась: существующий Playwright smoke ожидает поднятый full-stack на `localhost:8080`.

## Заметки / решения

- Assumption: `docs/gm-dashboard-prototype.html` является принятой дизайн-спекой; отдельный Image Gen концепт не требуется.
- Решение: не менять backend; блоки прототипа заполняются существующими данными, недоступные значения получают компактный fallback.
