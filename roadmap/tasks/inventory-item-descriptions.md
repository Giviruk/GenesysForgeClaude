# Описания предметов в инвентаре (inventory-item-descriptions)

- **Пункт ТЗ:** Not found in current codebase — пользовательская задача
- **Ветка:** `feature/inventory-item-descriptions`
- **Базовая ветка:** `master`
- **PR:** будет добавлен после создания
- **Статус:** 🚧 In progress

## Контекст

Карточка предмета в инвентаре использует общий `itemDescription`, но `CharacterItemDto` не
передаёт `SafeDescription`. В режиме `PublicSafe` полное описание намеренно пустое, поэтому
карточка остаётся без текста, хотя copyright-safe описание есть у `ItemDef`.

## План выполнения

- [x] Добавить `SafeDescription` в DTO позиции инвентаря и frontend-тип
- [x] Передавать safe-описание из `ItemDef` при построении полного листа и slices
- [x] Добавить API-регрессионный тест контракта описания
- [x] Добавить Vitest на отображение safe-описания в карточке инвентаря
- [x] Запустить релевантные и полные backend/frontend проверки
- [x] Подтвердить отсутствие миграций и изменений seed
- [ ] PR открыт

## Что осталось / блокеры

Блокеров нет.

## Проверки

- `dotnet test backend/GenesysForge.slnx --no-restore` — 1431 тест пройден.
- `npm run lint` — успешно.
- `npm test` — 272 теста пройдено.
- `npm run build` — успешно.
- Targeted: `CharacterSlicesApiTests` — 10 тестов; `InventoryCraftsmanship.test.tsx` — 11 тестов.

## Заметки / решения

- Изменение API additive: в `CharacterItemDto` добавляется поле `safeDescription`.
- Persistent model не меняется, миграция не требуется.
- Seed и copyright-sensitive тексты не меняются; используется уже существующий safe-парафраз.
- Задача не относится к ROT remediation, поэтому progress-файл не меняется.
