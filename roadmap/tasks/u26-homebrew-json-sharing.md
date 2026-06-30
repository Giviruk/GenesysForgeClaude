# U-26 · Импорт homebrew из JSON + шеринг + per-character toggle

- **Roadmap:** U-26 — Импорт homebrew из JSON + шеринг + per-character toggle (см. [unified-roadmap.md](../unified-roadmap.md))
- **Ветка:** `feature/u26-homebrew-json-sharing`
- **Базовая ветка:** `master`
- **PR:** pending
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
- [ ] Изучить текущие модели custom content, ContentPack, character/campaign reference filtering.
- [ ] Спроектировать минимальную модель homebrew-набора и JSON DTO без копирования official text.
- [ ] Реализовать backend импорт/экспорт/шеринг набора и ownership/visibility.
- [ ] Реализовать per-character и per-campaign enable/disable набора.
- [ ] Обновить frontend API client и UI.
- [ ] Добавить/обновить xUnit и Vitest тесты.
- [ ] Создать миграцию и обновить `docs/database.md`, если меняется persistent model.
- [ ] Обновить `docs/api.md` и task plan.
- [ ] Запустить релевантные проверки.
- [ ] Открыть PR.

## Что осталось / блокеры

- Нужно уточнить фактическую архитектуру существующих Content Packs и custom content перед финальным выбором модели.

## Заметки / решения

- Assumption: U-26 должен использовать собственную сущность homebrew-набора или расширение существующих Content Packs только если это не смешает campaign reference packs с user-shared homebrew imports.
- Copyright: JSON импорт должен принимать пользовательский контент; seed/private official text не добавлять.
