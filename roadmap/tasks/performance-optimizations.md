# Оптимизация запросов и загрузки frontend (performance-optimizations)

- **Пункт ТЗ:** Not found in current codebase — техническая задача по результатам аудита
- **Ветка:** `feature/performance-optimizations`
- **Базовая ветка:** `master`
- **PR:** [#148](https://github.com/Giviruk/GenesysForgeClaude/pull/148)
- **Статус:** ✅ Реализация завершена, draft PR открыт; ожидается CI/review

## Контекст

Снизить число DB round trips и объём загружаемых данных без изменения публичного API, игровых
правил и persistent model. Отдельно разбить стартовый frontend bundle по страницам.

## План выполнения

- [x] Пакетно загружать определения для импорта персонажа и соблюдать owner visibility
- [x] Убрать загрузку инвентаря из списка персонажей без изменения порогов
- [x] Заменить предварительную материализацию campaign ids на SQL `EXISTS`
- [x] Не выполнять membership-запрос для GM
- [x] Убрать повторное чтение `QualityDefs` в справочнике
- [x] Добавить lazy loading страниц frontend
- [x] Добавить/обновить регрессионные тесты
- [x] Запустить backend и frontend проверки
- [x] Разделить изменения на логические коммиты для безопасного `git revert`
- [x] PR открыт

## Что осталось / блокеры

Блокеров нет.

## Проверки

- Backend: `dotnet test backend/GenesysForge.slnx` — 1430 тестов пройдено.
- Frontend: `npm test` — 271 тест пройден; `npm run lint` и `npm run build` успешны.
- Стартовый JS chunk: 758,56 → 236,58 kB; gzip: 208,20 → 74,74 kB.
- `git diff --check` — ошибок нет.

## Безопасный откат

- Backend-оптимизации изолированы в коммите `134bd9c`.
- Frontend code splitting изолирован в коммите `1591e73`.
- Оба изменения откатываются независимо через `git revert <commit>`; миграций и ручного
  отката данных не требуется.

## Заметки / решения

- Миграции и изменение API-контракта не входят в эту ветку.
- Поисковые trigram-индексы и пагинация остаются отдельной задачей: первое требует миграции,
  второе меняет публичный API.
- `rot-rules-remediation-progress.md` не меняется: задача не относится к ROT remediation.
- Seed и copyright-sensitive контент не меняются.
