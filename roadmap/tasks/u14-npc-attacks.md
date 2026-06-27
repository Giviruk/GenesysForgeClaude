# Структурированные атаки NPC (u14-npc-attacks)

- **Roadmap:** U-14 — структурированные атаки NPC (см. [unified-roadmap.md](../unified-roadmap.md)); источник GF-008 / Аудит §5.
- **Ветка:** `feature/u14-npc-attacks`
- **Базовая ветка:** `master` (стек пуст — U-01…U-13 слиты, master @ cf7116f).
- **PR:** [#54](https://github.com/Giviruk/GenesysForgeClaude/pull/54)
- **Статус:** 🚧 In progress

## Контекст

`Npc.Equipment` сейчас `List<string>` — боевые строки («Длинный меч (Урон +3, Крит 2, Вплотную)») лежат
вперемешку с небоевым снаряжением. Этого мало для боевого роллера (U-17), карточек стола, encounter'ов и
импорта adversaries. U-14 выносит атаки в структурную модель `NpcAttack` (+ качества), оставляя `Equipment`
для небоевого.

Аналог уже есть у предметов: оружейный `ItemDef` хранит `SkillName/Damage/Crit/RangeBand/Properties` +
структурные `ItemQualityValue` → `QualityDef` (U-10, каталог 94 качества). Атаки NPC переиспользуют тот же
справочник качеств.

Решения с пользователем:
1. **Миграция:** best-effort парсер вытаскивает боевые строки из `Equipment` в `NpcAttack`, остальное
   остаётся в `Equipment`.
2. **Качества:** `NpcAttackQuality` ссылается на каталог `QualityDef` (Code+Rating), `NameRu` денормализуем
   для отображения — как `ItemQualityValue`.
3. **Генератор:** `QuickDraftNpcHandler`/`NpcDraftGenerator` сразу выдают структурные `NpcAttack` (из выбранного
   оружейного `ItemDef`), а не строку в `Equipment`.

## План выполнения

- [x] Domain: `NpcAttack` (NpcId/Name/SkillName/Damage/Critical/RangeBand/Notes + nav `Qualities`) и
      `NpcAttackQuality` (NpcAttackId/QualityDefId/Rating + денорм `QualityCode`/`NameRu`); в `Npc` — nav `Attacks`.
- [x] EF: `AppDbContext`/`IAppDbContext` DbSet'ы + конфиг (FK cascade Npc→Attack→Quality, SetNull Quality→QualityDef,
      индекс NpcId); миграция `AddNpcAttacks`. Backfill `Equipment`→`NpcAttack` в `SeedData.BackfillNpcAttacks`
      через `NpcEquipmentParser` (Урон/Крит/Дистанция/качества; неразобранное остаётся в `Equipment`; идемпотентно).
- [x] DTO: `NpcAttackDto` (+ `NpcAttackQualityDto`); `Attacks` в `NpcDetailDto`; `NpcInput.Attacks`.
      `NpcMapper.ToDetail`/`Apply` маппинг; `ResolveAttackQualitiesAsync` резолвит код→`QualityDefId`+канон NameRu
      (несопоставленное — custom). `LoadAsync` — `Include(Attacks).ThenInclude(Qualities)`.
- [x] Команды: Create/Update вызывают резолв качеств; `DuplicateNpcHandler` копирует атаки с детьми (новые Id).
- [x] Генератор: `QuickDraftNpcHandler.ApplyCatalogLoadout` кладёт оружие как `NpcAttack` (`AttackFromWeapon` —
      skill/damage/crit/range/качества из `ItemDef`), броня остаётся в `Equipment`.
- [x] Frontend: типы `types.ts`; `npcStats.npcAttackViews`/`npcGearViews`; секция «Атаки» в детали и `AttacksEditor`
      (skill dropdown боевые/магия, damage/crit/range, пикер качеств из каталога с рейтингом); статблок-атаки в
      печатной карточке [cards.tsx](../../frontend/src/components/print/cards.tsx) и markdown.
- [x] Тесты: Domain `NpcEquipmentParserTests` (7); Api round-trip качества + duplicate копирует атаки; QuickDraft-тест
      обновлён на структурную атаку; Vitest `npcAttackViews`. Итог: Api 178 / Domain 72 / front 77 зелёные.
- [x] docs/database.md обновлён (секция NpcAttacks + миграция). domain-model.md NPC не документирует — не трогаем.
- [x] PR открыт — #54.

## Что осталось / блокеры

- Дождаться ревью/слияния PR #54 → статус Done.
- Применить миграцию + перезапуск backend на проде (backfill `BackfillNpcAttacks` отрабатывает на старте).

## Заметки / решения

- Парсер боевых строк — best-effort: распознаём «Урон +N/N», «Крит N», русские дистанции, качества по NameRu из
  каталога; что не разобралось — строка остаётся в `Equipment` (без потери данных).
- `Encounter`/Game Table читают `NpcDetailDto.Attacks` — проверить, что карточки участников стола их видят.
- Боевой роллер (U-17) и RoT-генератор (U-16) — потребители этой модели; здесь только структура + базовый UI.
