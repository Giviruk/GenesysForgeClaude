# Russian term consistency (russian-term-consistency)

- **Roadmap:** вне unified roadmap — polish/localization audit
- **Ветка:** `feature/russian-term-consistency`
- **Базовая ветка:** `master`
- **PR:** #77
- **Статус:** 🚧 In progress

## Контекст

Проверить, где один и тот же термин переводится по-разному или где UI показывает английские поля вместо русских. Отдельно исправить отображение навыков на русском и предметов в инвентаре.

Затрагиваемые зоны: frontend labels/helpers, character sheet, inventory, print views, NPC/encounter/game table, backend sheet DTO mapping при необходимости.

## План выполнения

- [x] Создать ветку и task plan.
- [x] Найти места, где UI отображает английские `name`/`skillName`/`itemName` при наличии русских полей.
- [x] Исправить отображение навыков на русском языке.
- [x] Исправить отображение предметов в инвентаре на русском языке.
- [x] Проверить и выровнять спорные русские термины/лейблы.
- [x] Обновить/добавить релевантные frontend tests.
- [x] Запустить релевантные проверки.
- [x] PR открыт.

## Что осталось / блокеры

PR открыт: #77.

## Заметки / решения

- Assumption: пользовательские названия персонажей/NPC/custom content не переводим автоматически; исправляем только встроенный reference/content display и статичные UI-термины.
- Единый пользовательский термин для `strain`: «усталость»; кодовые поля/DTO (`strainCurrent`, `healStrain`) не переименовывались.
- `NPC` оставлен как короткая системная аббревиатура в разделах бестиария/энкаунтеров.
- Проверки: `dotnet test backend/GenesysForge.slnx`, `npm run test`, `npm run lint`, `npm run build`.
