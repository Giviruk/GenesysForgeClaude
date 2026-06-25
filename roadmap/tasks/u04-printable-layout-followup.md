# Группировка навыков и действия printable-листа (u04-printable-layout-followup)

- **Roadmap:** U-04 — Полный printable / PDF-friendly лист персонажа (см. [unified-roadmap.md](../unified-roadmap.md))
- **Ветка:** `feature/u04-printable-layout-followup`
- **Базовая ветка:** `master` (PR #33 слит, открытых PR нет)
- **PR:** [#34](https://github.com/Giviruk/GenesysForgeClaude/pull/34)
- **Статус:** 🚧 In progress

## Контекст

Follow-up после PR #33: сделать печатный список навыков компактнее и ближе к структуре
основного листа, а действия персонажа стабильно держать в правой части шапки.

## План выполнения

- [x] Разделить печатные навыки по направлениям
- [x] Сбалансировать направления в две колонки на desktop и одну на mobile
- [x] Сгруппировать «Печать листа», «Экспорт JSON», «Завершить создание» справа
- [x] Обновить компонентный тест печатного листа
- [x] Frontend lint/test/build
- [x] Browser QA: desktop/mobile, overflow и console
- [x] Миграции не требуются
- [x] Copyright-проверка: seed/справочники не менялись
- [x] Статус в `unified-roadmap.md` оставлен `In progress` до merge follow-up PR
- [x] PR #34 открыт

## Что осталось / блокеры

- После merge отметить U-04 как `✅ Done (PR #N)`.

## Заметки / решения

- Направления сохраняют порядок: общие, боевые, социальные, знания, магия.
- Граница колонок выбирается по числу строк, без разрыва одной категории между колонками.
- Backend, API contract и persistent model не менялись.
