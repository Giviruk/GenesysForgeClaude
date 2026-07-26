# Исправления правил Core/RoT — прогресс реализации (`claude/rot-remediation-tasks`)

- **ТЗ:** [rot-rules-remediation-tasks.md](rot-rules-remediation-tasks.md) — источник истины по каждому пункту.
- **Ветка:** `claude/rot-remediation-tasks-24ebb2`
- **Базовая ветка:** `master`
- **PR:** — (после создания)
- **Статус:** 🚧 In progress

## Контекст

ТЗ содержит ~95 задач (`ROT-*`) по 11 разделам: каждая требует правила в Domain/Application,
персистентности, безопасного API, UI и тестов. Работа идёт **строго по порядку файла ТЗ**; каждая
завершённая группа коммитится отдельно, чтобы следующий агент мог продолжить с первого незакрытого
пункта этого чеклиста.

Обязательные соглашения из §0 ТЗ (приоритет RoT → Core → FAQ/Errata v1.1, предварительная валидация
до первой мутации, стабильные `code`/ID вместо имён, versioned export, audit-запись, domain error с
машинным `reasonCode`) применяются ко всем пунктам и отдельно в чеклисте не повторяются.

## Чеклист по разделам ТЗ

### 1. Создание персонажа и стартовая прогрессия

- [x] **ROT-CRE-01** — видовые карьерные навыки и общий предел рангов
- [x] **ROT-CRE-02** — заморозка Wound/Strain Threshold после создания
- [ ] **ROT-CRE-03** — стандартные деньги или целый карьерный комплект
- [ ] **ROT-CRE-04** — комплект Scout с учётом errata

### 2. Видовые способности RoT

- [ ] **ROT-SPECIES-01** — полный каталог 14 вариантов и исполняемые способности

### 3. Таланты RoT

- [ ] **ROT-TAL-01** — состав каталога и metadata
- [ ] **ROT-TAL-02** — prerequisites, взаимоисключения, покупка и refund
- [ ] **ROT-TAL-03** — обязательные сохраняемые параметры талантов
- [ ] **ROT-TAL-04** — карьерные навыки, выдаваемые талантами (+ cost stack для refund)
- [ ] **ROT-TAL-05** — общий lifecycle активных талантов
- [ ] **ROT-TAL-06** — исправить неверные исполняемые эффекты
- [ ] **ROT-TAL-07** — полный PrivateFull и согласованный RU/EN

### 4. Heroic Abilities

- [ ] **ROT-HA-01**, **ROT-HA-02**, **ROT-HA-05**, **ROT-HA-08**, **ROT-HA-10**, **ROT-HA-CONTENT**

### 5. Бой, здоровье и Defense

- [ ] **ROT-CMB-01** … **ROT-CMB-06**

### 6. Minion Groups

- [ ] **ROT-MIN-01** … **ROT-MIN-07**

### 7. Социальные столкновения

- [ ] **ROT-SOC-01** … **ROT-SOC-10**

### 8. Снаряжение, экономика, оружие, броня, attachments

- [ ] **ROT-EQP-01**, **ROT-ECO-01**, **ROT-EQP-02**, **ROT-EQP-GEAR-01**, **ROT-EQP-SVC-01**
- [ ] **ROT-MOUNT-ITEM-01**, **ROT-WPN-01**, **ROT-ARM-01**, **ROT-WPN-02**
- [ ] **ROT-EQP-ATT-01** … **ROT-EQP-ATT-03**, **GEN-EQP-DMG-01**, **ROT-EQP-AMMO-01**
- [ ] **GEN-EQP-QUAL-01**, **ROT-MAG-IMP-01**, **ROT-MAG-MAT-01**, **ROT-EQP-SRC-01**

### 9. Магия

- [ ] **ROT-MAG-01** … **ROT-MAG-08**, **ROT-MAG-10** … **ROT-MAG-12**, **ROT-MAG-CONTENT**

### 10. Magic items, crafting и alchemy

- [ ] **ROT-MITEM-01**, **ROT-CRAFT-01**, **ROT-CRAFT-MAGIC-01**
- [ ] **ROT-ALCH-01**, **ROT-ALCH-02**, **ROT-MITEM-CONTENT**

### 11. Очистка каталогов, бестиарий, скакуны

- [ ] **ROT-CLEAN-3.1**, **3.2**, **3.5**, **3.6**, **3.7**
- [ ] **ROT-BEST-01**, **ROT-MOUNT-NPC-01**, **ROT-BEST-CONTENT**

### 11.1. Переработка конструктора NPC

- [ ] **ROT-NPC-01** … **ROT-NPC-13**

## Выполнено подробно

### ROT-CRE-01 — видовые карьерные навыки и общий предел рангов

- `ArchetypeStartingSkill.GrantsCareerSkill` + флаг в `archetypes.catalog.json` ровно у двух выдач:
  Deep Elf `Knowledge (Forbidden)`, Highborn Elf `Divine`.
- Новый доменный `CareerSkillResolver` (`Domain/Rules/CareerSkillResolver.cs`): карьера ∪ вид ∪
  таланты, дедупликация по `SkillDefId`, нерезолвленные имена возвращаются отдельно, а не теряются.
- `TalentDef.CareerSkillNames` заведён заранее — ветка талантов резолвера уже подключена, наполнение
  каталога делает ROT-TAL-04.
- Новый `CreationSkillPlan` (`Domain/Rules/CreationSkillPlan.cs`): создание собирает полный план всех
  бесплатных прибавок и проверяет предел ранга 2 **до** первой записи; превышение — ошибка с
  перечислением источников, обрезки ранга нет.
- Резолвер подключён в `CreateCharacterHandler`, `SheetBuilder` и `BuySkillRankHandler`; хранимый
  `CharacterSkill.IsCareer` остался кэшем.
- `CharacterSkillDto.CareerSources` + `ArchetypeStartingSkillDto.GrantsCareerSkill` в API;
  фронт показывает объединённый список карьерных навыков, источники в подсказке и disabled reason
  для уже добитого до 2 видового навыка.

### ROT-CRE-02 — заморозка порогов после создания

- `Character.CreationWoundThreshold/CreationStrainThreshold/ThresholdSnapshotProvenance/RulesReviewRequired`.
- `CompleteCreation` пишет snapshot в той же транзакции до смены фазы; повторный вызов идемпотентен.
- `SheetCalculator.ComputeDerived` принимает snapshot; явные бонусы порога прибавляются поверх один раз.
- Единый `CharacterDerived.Compute` — лист, список персонажей и Game Table больше не дублируют маппинг.
- Export поднят до `genesysforge.character.v2`; v1 читается с предупреждением и детерминированным
  расчётом вместо нуля. Duplicate копирует snapshot.
- Backfill в миграции восстанавливает характеристику на момент completion точно (текущая − выдачи
  Dedication), а не угадывает.

## Что осталось / блокеры

- Следующий пункт — **ROT-CRE-03** (взаимоисключающие `standardMoney` / `careerPackage`).
- ROT-TAL-04 требует cost stack покупок рангов (`CharacterSkillRankPurchase`) и repair-флоу для
  legacy без доказанной цены — крупнее остальных пунктов раздела 3, планировать отдельно.

## Заметки / решения

- `Assumption`: в §0.4 ТЗ разрешает типизированные domain-strategy вместо универсального
  JSON-интерпретатора — карьерные выдачи и план создания сделаны типизированными.
- Шаги 3–4 legacy-стратегии ROT-CRE-02 (восстановление из видимого итога и `LegacyEstimated`)
  в БД не нужны: шаг 2 применим всегда, потому что после создания характеристику меняет только
  Dedication и её выдачи сохранены. Оба провенанса всё же заведены — их использует импорт.
