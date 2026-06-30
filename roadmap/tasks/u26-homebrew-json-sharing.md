# U-26 · Импорт homebrew из JSON + шеринг + per-character toggle

- **Roadmap:** U-26 — Импорт homebrew из JSON + шеринг + per-character toggle (см. [unified-roadmap.md](../unified-roadmap.md))
- **Ветка:** `feature/u26-homebrew-json-sharing`
- **Базовая ветка:** `master`
- **PR:** [#70](https://github.com/Giviruk/GenesysForgeClaude/pull/70)
- **Статус:** 🚧 In progress

## Контекст

Задача продолжает P2 homebrew после U-25. Нужно дать пользователю переносимый JSON-формат для набора homebrew и возможность включать/выключать такие наборы для персонажа/кампании.

Текущий roadmap scope:

- Backend: импорт набора homebrew `skills/talents/items/heroics/archetypes/careers` из JSON.
- Маппинг по `Code`, совместимый формат для переноса между аккаунтами.
- Публикация/шеринг набора другим пользователям отдельно от campaign Content Packs.
- Флаг включения homebrew-набора на персонажа/кампанию.
- DoD: homebrew переносится через JSON; можно расшарить набор; персонаж может включать/выключать наборы.

## План выполнения

- [x] Создать ветку и plan-файл.
- [x] Отметить U-25 как Done после merge PR #69 и U-26 как In progress.
- [x] Изучить текущие модели custom content, ContentPack, character/campaign reference filtering.
- [x] Спроектировать минимальную модель homebrew-набора и JSON DTO без копирования official text.
- [x] Реализовать backend импорт/экспорт/шеринг набора и ownership/visibility.
- [x] Реализовать per-character и per-campaign enable/disable набора.
- [x] Обновить frontend API client и UI.
- [x] Добавить/обновить xUnit и Vitest тесты.
- [x] Создать миграцию и обновить `docs/database.md`, если меняется persistent model.
- [x] Обновить `docs/api.md` и task plan.
- [x] Запустить релевантные проверки.
- [x] Открыть PR.

## Что осталось / блокеры

- Ожидается review/merge PR #70.

## Заметки / решения

- Assumption: U-26 должен использовать собственную сущность homebrew-набора или расширение существующих Content Packs только если это не смешает campaign reference packs с user-shared homebrew imports.
- Решение: добавлен отдельный `HomebrewPack`, существующие campaign `ContentPacks` остаются handbook/справочником кампании.
- Решение: импорт pack создаёт user-owned custom defs с `HomebrewPackId`; shared import копирует pack в аккаунт получателя.
- Решение: `GET /api/reference/{system}` принимает optional `characterId`/`campaignId` и применяет default/target toggles.
- Copyright: JSON импорт должен принимать пользовательский контент; seed/private official text не добавлять.
