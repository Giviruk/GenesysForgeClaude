# Автоматизация талантов и героических способностей (u18-talent-automation)

- **Roadmap:** U-18 (см. [unified-roadmap.md](../unified-roadmap.md)); источник GF-012 / Аудит §2.2.
- **Ветка:** `feature/u18-talent-automation` (от master после U-17).
- **Базовая ветка:** `master`.
- **PR:** _(Stage 1 — будет указан)_
- **Статус:** 🚧 In progress

## Контекст

Таланты/героика имеют только текстовые `ActivationCost/Duration/Frequency` — без структурной автоматизации.
Живое состояние (current раны/усталость) есть у `GameParticipant` (Game Table); лист персонажа хранит только
пороги. Цель (с пользователем): **оба места** активации и **полный каталог** размеченных эффектов.

Доводим двумя PR (ревьюабельность):
- **Stage 1:** модель эффектов + разметка героики + активация на Game Table (есть живое состояние).
- **Stage 2:** живое состояние у `Character` + активация на листе + audit-log (U-09) + добор талантов + boost-к-следующей.

НЕ автоматизируем сложные нарративные/GM-эффекты → показываем как manual prompt.

## Stage 1 — модель эффектов + Game Table  ✅ (PR — будет указан)

- [x] Domain: enum `RuleEffectKind` (HealWounds/HealStrain/AdjustSoak/AdjustMeleeDefense/AdjustRangedDefense/
      AdjustWoundThreshold/AdjustStrainThreshold/AddBoostNextCheck/AddSetbackNextCheck/SpendStoryPoint/Manual);
      `RuleEffectDef` (HeroicAbilityDefId, Kind, Amount, Duration, Description).
- [x] Привязка эффектов к `HeroicAbilityDef` (nav `Effects`), seed-разметка героики (`HeroicCatalog.EffectsFor`):
      размечены явно-механические (hard-to-kill → +4 Soak, miraculous-recovery → лечит 3 раны), прочие = Manual.
      Миграция `AddRuleEffectDefs`. Эффекты отданы в reference (`HeroicAbilityDto.Effects` + `Code`).
- [x] `ICombatTarget` + `RuleEffectApplier` (чистая логика): heal/adjust меняют поля цели с клампами,
      boost/setback/story/manual → подсказки. `GameParticipant : ICombatTarget`.
- [x] API: `POST /api/campaigns/{cid}/session/participants/{id}/activate` (AbilityCode) → применяет эффекты,
      пишет в roll-log сводку; manual-части возвращаются как prompt. Доступ: GM или владелец PC (как UpdateParticipant).
- [x] Frontend: у участника Game Table — выбор способности (героики с эффектами из reference) + «Активировать»,
      показ applied/manual.
- [x] Тесты: Domain `RuleEffectApplierTests` (4), Api `ActivateAbility...` (участник+лог). Domain 102 / Api 187 / front 92.

## Stage 2 — лист персонажа (следующий PR)
- Character: `WoundsCurrent`/`StrainCurrent` (+ миграция), отображение/правка на листе.
- Активация талантов/героик на листе → применение + `CharacterAuditEntry` (U-09).
- Boost/setback к следующей проверке (pending-модификатор), добор разметки активных талантов.

## DoD (вся U-18)
- Простые активные эффекты применяются кнопкой (стол и лист); сложные — manual prompt; видно, что применилось.

## Заметки / решения
- Story points — пул сессии/партии (не на персонаже); в v1 spend story point = manual prompt (модель пула — позже).
- Эффекты привязаны к встроенному контенту по `Code`; кастом — без авторазметки (manual).
