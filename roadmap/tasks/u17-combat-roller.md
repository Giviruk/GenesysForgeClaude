# Боевой attack/damage roller (u17-combat-roller)

- **Roadmap:** U-17 (см. [unified-roadmap.md](../unified-roadmap.md)); источник GF-011 / Аудит §7.
- **Ветка:** `feature/u17-combat-roller` (от master после U-16).
- **Базовая ветка:** `master`.
- **PR:** [#59](https://github.com/Giviruk/GenesysForgeClaude/pull/59)
- **Статус:** 🚧 In progress

## Контекст

Поверх U-08 (нарративный roller: `diceRoller.ts` + `DiceRoller.tsx`, лог через `RollLogEntry`/`api.createRoll`)
и U-14 (структурные атаки NPC) добавляем боевой слой: от конкретной атаки/оружия — собрать пул, бросить,
посчитать базовый урон и показать качества с ценой активации. v1 — расчёт на фронте; лог в стол через
существующий API. Roller не решает за мастера (показывает расчёт, выбор за GM).

Решения с пользователем:
1. Размещение — **кнопка «Атаковать» от каждой атаки** в листе NPC и оружия в листе персонажа.
2. **С логом в стол** (переиспользовать `RollLogEntry`/`onLog`/`api.createRoll`), где есть кампания.

## План выполнения

- [x] `utils/combat.ts`: `expandDamage(damage, brawn)` (`+N`→Мощь+N, абсолют, иначе null) + `combatTotal(base, netSuccess)`;
      `resolveQualityCosts`/`qualitiesFromProperties` → `{label, activationCost}` (NPC — по коду, персонаж — по имени из properties).
- [x] `DiceRoller.tsx`: опциональный `onResult?: (o: RollOutcome) => void`.
- [x] `CombatRoller.tsx`: модалка — шапка (имя/навык/урон/крит/дистанция, качества с ценой), встроенный `DiceRoller`
      (пул засеян навыком), панель урона (база + нетто-успехи = итог), опц. лог/секрет.
- [x] Лист NPC (`NpcsPage`) — кнопка «🎲 Атаковать» у каждой атаки → CombatRoller (пул из `npcAttackViews`,
      качества из `reference` по коду). Лог: если `npc.campaignId` и `isMine` → `api.createRoll`, секрет для GM.
- [x] Лист персонажа (`InventoryTab.WeaponLine`) — кнопка «🎲 Атаковать» → CombatRoller (пул навыка оружия,
      урон/качества из предмета; качества по имени из `reference`). v1 локально (без лога на листе).
- [x] Тесты: Vitest `combat.ts` (expandDamage/combatTotal/resolveQualityCosts/qualitiesFromProperties) — front 92 зелёные.
      Backend без изменений (переиспользует `RollLogEntry`/`api.createRoll`).
- [ ] docs/PR.

## DoD
- [x] Видно расчёт базового урона по оружию (база + успехи); качества показаны с ценой активации; бросок логируется
  в стол (для NPC кампании); roller не принимает решений за мастера.

## Заметки / решения
- Переиспользуем `diceRoller.ts`/`DiceRoller.tsx` (U-08) и `api.createRoll` (без нового backend).
- Урон оставляем `+N` в данных, раскрываем в Мощь+N при расчёте (как в U-15).
