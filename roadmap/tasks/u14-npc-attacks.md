# Структурированные атаки NPC (u14-npc-attacks)

- **Roadmap:** U-14 — структурированные атаки NPC (см. [unified-roadmap.md](../unified-roadmap.md)); источник GF-008 / Аудит §5.
- **Ветка:** `feature/u14-npc-attacks`
- **Базовая ветка:** `master` (стек пуст — U-01…U-13 слиты, master @ cf7116f).
- **PR:** _(пока нет)_
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

- [ ] Domain: `NpcAttack` (NpcId/Name/SkillName/Damage/Critical/RangeBand/Notes + nav `Qualities`) и
      `NpcAttackQuality` (NpcAttackId/QualityDefId/Rating + денорм `QualityCode`/`NameRu`); в `Npc` — nav `Attacks`.
- [ ] EF: `AppDbContext`/`IAppDbContext` DbSet'ы + конфиг (FK cascade Npc→Attack→Quality, индекс NpcId);
      миграция `AddNpcAttacks`. Backfill-парсер боевых строк `Equipment`→`NpcAttack` внутри миграции/seed
      (Урон/Крит/Дистанция/качества по справочнику; неразобранное оставить в `Equipment`).
- [ ] DTO: `NpcAttackDto` (+ `NpcAttackQualityDto`); добавить `Attacks` в `NpcDetailDto`; `NpcInput.Attacks`.
      `NpcMapper.ToDetail`/`Apply` — маппинг и валидация (skill/damage/range обязательны, crit ≥1 или пусто,
      качество резолвится по `QualityDef.Code` либо custom). `LoadAsync` — `Include(Attacks).ThenInclude(Qualities)`.
- [ ] Команды: `CreateNpcCommand`/`UpdateNpcCommand`/`DuplicateNpcCommand` сохраняют атаки (дубликат — копия
      детей с новыми Id).
- [ ] Генератор: `QuickDraftNpcHandler.PickWeapon`/`ApplyCatalogLoadout` кладут оружие как `NpcAttack`
      (skill/damage/crit/range/качества из `ItemDef`), а не строкой в `Equipment`. `NpcDraftGenerator.WeaponFor`
      — отдавать данные для атаки.
- [ ] Frontend: типы `types.ts`, секция «Атаки» в форме NPC (skill dropdown, damage, crit, range, пикер качеств
      из каталога), отображение атак в детальной карточке; статблок-атаки в печатной карточке
      [cards.tsx](../../frontend/src/components/print/cards.tsx) с бейджами-tooltip по качествам.
- [ ] Тесты: Domain (парсер боевых строк, маппинг ItemDef→NpcAttack), Api (CRUD с атаками, duplicate копирует
      атаки, валидация, QuickDraft даёт структурную атаку, backfill миграции), Vitest форма NPC.
- [ ] docs/database.md, docs/domain-model.md обновить.
- [ ] PR открыть, номер записать сюда + в unified-roadmap.

## Что осталось / блокеры

- Требуется применить миграцию + перезапуск backend (backfill отрабатывает на старте/в миграции).

## Заметки / решения

- Парсер боевых строк — best-effort: распознаём «Урон +N/N», «Крит N», русские дистанции, качества по NameRu из
  каталога; что не разобралось — строка остаётся в `Equipment` (без потери данных).
- `Encounter`/Game Table читают `NpcDetailDto.Attacks` — проверить, что карточки участников стола их видят.
- Боевой роллер (U-17) и RoT-генератор (U-16) — потребители этой модели; здесь только структура + базовый UI.
