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
- [x] **ROT-CRE-03** — стандартные деньги или целый карьерный комплект
- [x] **ROT-CRE-04** — комплект Scout с учётом errata

### 2. Видовые способности RoT

- [~] **ROT-SPECIES-01** — каталог, типизированные правила и пассивные эффекты сделаны;
  активируемые способности ждут состояния сессии (см. ниже)

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

- [ ] **ROT-CLEAN-3.1**, **3.2**, **3.5**, **3.6**
- [x] **ROT-CLEAN-3.7** — удалён выдуманный Adventuring Pack (сделан вместе с ROT-CRE-03)
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

### ROT-CRE-03 / ROT-CRE-04 / ROT-CLEAN-3.7 — стартовое снаряжение

- `StartingEquipmentMode` (`StandardMoney` | `CareerPackage`), `Character.StartingPurchaseBudget`,
  `CharacterItem.Provenance`. Отсутствие поля режима у старого клиента = `StandardMoney`.
- Бюджет 500 и карманные 1d100 — **два разных счёта** (`StartingWallet`): покупка при создании
  тратит бюджет первым, продажа возвращает в бюджет первым, иначе цикл «купить → продать» превращал
  бы бюджет в реальные деньги.
- `MoneyFormula` разбирает `200 + 1d100` и бросает через инъецированный `IDiceRoller`
  (`SystemDiceRoller` на криптостойком RNG); формула и фактический бросок пишутся в audit
  (`CharacterAuditAction.CharacterCreated`).
- `CareerPackageResolver` выдаёт комплект только целиком: точное множество групп, ровно одна опция,
  без дублей и чужих опций; отказ — `DomainRuleException` с машинным `reasonCode`, который теперь
  доезжает до клиента в `ErrorResponse.ReasonCode`.
- Каталог `career-extras` переписан по таблице ТЗ для всех 9 карьер. Scout: первая группа — ровно
  `Bow` | `Light Spear`, `Leather Armor` вынесена в фиксированные, поэтому `Leather Armor ×2` больше
  не возникает. `Traveling Gear` разложен на 6 реальных предметов.
- Выдуманный `Adventuring Pack` помечен `Retired`; заодно `Retired` добавлен во все content-таблицы
  и включён в фильтры справочника — это база для раздела 11.
- Автоматическая раскладка legacy-паков выполняется только при доказанных provenance/quantity/state;
  иначе строка остаётся read-only.

### ROT-SPECIES-01 — виды (частично)

Сделано:

- Все 14 профилей сверены с таблицей ТЗ построчно (они уже были верны) и закреплены
  table-тестом на каждое значение, включая silhouette.
- `SpeciesAbilityRuleKind` + структурные параметры (`RuleValue`, `RuleParameters`, `UsesPerScope`,
  `UseScope`, `StoryPointCost`) на `ArchetypeAbilityDef`; все 19 способностей RoT типизированы.
  Механика больше не выводится из имени или описания.
- `ArchetypeDef.Silhouette` (гномы 0) и правило `Small` через тот же типизированный механизм.
- Nimble реализован как provider: `SheetCalculator` берёт max(броня, видовая база), поэтому
  Defense 1 с бронёй Defense 1 остаётся 1, а таланты по-прежнему прибавляются сверху.
- Обязательный выбор Half-Catfolk: `Character.SpeciesAbilityChoiceCode`, валидация при создании с
  `species.choice.required` / `unknown_option` / `not_applicable`, перенос в duplicate/export/import,
  `SpeciesChoiceIncomplete` на листе для legacy, пикер и гейт в форме создания.

Осталось (нужен рантайм сессии/encounter, которого в приложении ещё нет):

- Счётчики применений со scope session/encounter и их сброс в `EndSession`/`EndEncounter`.
- Активации, тратящие Story Point: Ready for Adventure, Tough as Nails, Tricksy.
- Правила, которым нужен единый check context с `sourceTag` и стабильной целью: Dark Vision,
  Stubborn, Battle Rage, Hot Tempered, Tenacious, Militia Training.
- Claws как virtual attack profile и структурированные manoeuvres для Fleet of Paw.

## Что осталось / блокеры

- Следующий пункт — доделать активируемые способности ROT-SPECIES-01 (нужен check context и
  счётчики сессии), затем раздел 3 (**ROT-TAL-01** …).
- ROT-TAL-04 требует cost stack покупок рангов (`CharacterSkillRankPurchase`) и repair-флоу для
  legacy без доказанной цены — крупнее остальных пунктов раздела 3, планировать отдельно.

## Заметки / решения

- `Assumption`: в §0.4 ТЗ разрешает типизированные domain-strategy вместо универсального
  JSON-интерпретатора — карьерные выдачи и план создания сделаны типизированными.
- Шаги 3–4 legacy-стратегии ROT-CRE-02 (восстановление из видимого итога и `LegacyEstimated`)
  в БД не нужны: шаг 2 применим всегда, потому что после создания характеристику меняет только
  Dedication и её выдачи сохранены. Оба провенанса всё же заведены — их использует импорт.
