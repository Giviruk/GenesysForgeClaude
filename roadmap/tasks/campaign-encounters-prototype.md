# Реализация прототипа энкаунтеров кампании (campaign-encounters-prototype)

- **Roadmap:** вне U-нумерации — доработка UI по HTML-прототипу
- **Ветка:** `feature/campaign-encounters-prototype`
- **Базовая ветка:** `master`
- **PR:** [#82](https://github.com/Giviruk/GenesysForgeClaude/pull/82)
- **Статус:** 🚧 In progress

## Контекст

Нужно проанализировать текущую версию вкладки "Энкаунтеры" и довести ее до `docs/campaign-encounters-prototype.html`.
Текущая версия уже содержит часть переноса из PR #81: strip, toolbar, очередь сцен, copy-grid, таблицу участников и быструю сборку. Осталось улучшить детальную рабочую область так, чтобы основные блоки прототипа были выровнены и полезны в текущем flow.

Затрагиваемые файлы:

- `frontend/src/components/EncountersTab.tsx`
- `frontend/src/index.css`

## План выполнения

- [x] Подготовка: AGENTS.md, docs/ai-context.md, graphify query, `git fetch`, `gh pr list`, ветка от свежего `master`
- [x] Сравнить текущий `EncountersTab` с `docs/campaign-encounters-prototype.html`
- [x] Добавить компактную сводку инициативы внутри выбранного энкаунтера без переноса полноценного initiative flow из Game Table
- [x] Перестроить detail layout в 2x2 сетку: содержание сцены, запуск, участники, инициатива
- [x] Сохранить панели заметок/быстрой сборки одной строкой ниже, чтобы они не сдвигали основные блоки
- [x] Проверить responsive layout и отсутствие horizontal overflow
- [x] Запустить релевантные frontend проверки
- [x] PR открыт

## Что осталось / блокеры

Нет.

## Заметки / решения

- Не возвращаем блоки "Готовность" и "Проверка перед стартом" — они были убраны ранее осознанно.
- Инициатива в энкаунтере будет только preview/summary на основе участников и `initiativeSide`; полноценное управление инициативой остается в Game Table.
- Browser/IAB tool недоступен в текущем окружении, визуальная проверка выполнена Playwright fallback на Vite dev server с моками API.
- QA: desktop 1440px и mobile 390px без horizontal overflow; основные 4 панели выровнены по строкам, `Быстрая сборка` и `Заметки` в одной строке ниже.
