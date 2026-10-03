# Обновление зависимостей до последних stable (update-dependencies)

- **Пункт ТЗ:** вне ROT/GEN — обслуживание; явный запрос владельца 03.10.2026
- **Ветка:** `feature/update-dependencies`
- **Базовая ветка:** `master` (открытых PR нет)
- **PR:** [#255](https://github.com/Giviruk/GenesysForgeClaude/pull/255)
- **Статус:** 🚧 In progress

## Контекст

Сборка предупреждала о транзитивном `Microsoft.OpenApi` 2.0.0 с уязвимостью GHSA-v5pm-xwqc-g5wc (high);
`npm audit` находил уязвимости в dev-зависимостях (brace-expansion, browserslist, postcss, undici и др.).
Владелец попросил обновить все пакеты до последних стабильных версий.

## План выполнения

- [x] NuGet: все пакеты верхнего уровня до latest stable (`dotnet list package --outdated` пуст)
- [x] NuGet: уязвимых пакетов нет (`--vulnerable --include-transitive`), `Microsoft.OpenApi` → 2.12.0
- [x] npm: минорные + SignalR 10, затем мажорные (vitest 5, jsdom 30, jest-dom 7), `npm audit fix` → 0 уязвимостей
- [x] `dotnet test`: 744 domain + 876 API; `npm run lint`, `npm test` (444), `npm run build` (с чистого dist)
- [x] CHANGELOG
- [ ] PR открыт, CI (включая E2E Playwright) зелёный, смержен, задеплоен

## Что осталось / блокеры

Сознательно не обновлено:

- `typescript` 6.0.3 → 7.0: typescript-eslint 8.71 (последний) поддерживает TypeScript `<6.1.0`;
  TS 7 — нативный компилятор, линтер его пока не умеет. Вернуться, когда typescript-eslint добавит поддержку.
- `@vitejs/plugin-react` 6.0.2 → 6.1.1: npm падает с ERESOLVE на цепочке опциональных peer
  (`@rolldown/plugin-babel` → `@babel/plugin-transform-runtime` 8 → `@babel/core` 8), а Babel 7 держат
  `eslint-plugin-react-hooks` и `workbox-build` (`vite-plugin-pwa`). Минорная разница, к безопасности не относится.
- `@types/node` 24.x, а не 26: типы должны совпадать с рантаймом — Node 24 в Docker и CI.
- `xunit` 2.9.3 — последняя версия пакета; xunit v3 — отдельный пакет `xunit.v3`, это миграция, а не обновление.
- Базовые Docker-образы (`nginx:1.27-alpine` и др.) — не пакеты, вне этой задачи.

## Заметки / решения

- Миграции не нужны; API и правила не меняются.
- Copyright: seed/справочники не затрагиваются.
