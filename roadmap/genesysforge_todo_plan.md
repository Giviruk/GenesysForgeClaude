# GenesysForgeClaude — план доработок

Документ описывает, чего не хватает в репозитории `Giviruk/GenesysForgeClaude`, и какие задачи стоит выполнить дальше. Формат рассчитан на то, чтобы его можно было положить в `docs/` репозитория и давать Claude/Codex как контекст для разработки.

## 0. Контекст проекта

Проект — интерактивный лист персонажа и инструменты мастера для **Genesys Core** и **Realms of Terrinoth**.

Уже есть:

- backend на .NET / ASP.NET Core Minimal API;
- frontend на React + TypeScript + Vite;
- PostgreSQL через EF Core migrations;
- авторизация, JWT, refresh-cookie flow;
- персонажи, навыки, таланты, инвентарь, героические способности;
- справочник магии и Magic Action Builder;
- кампании, NPC/bestiary, encounter builder, Game Table;
- custom content;
- CI, Docker Compose и deploy workflow.

Главная проблема сейчас не в отсутствии базового MVP, а в том, что многие правила Genesys/RoT ещё представлены как текст, строки или справочные записи, а не как машинно-обрабатываемые данные. Следующая стадия проекта должна превратить приложение из “листа + справочника” в **rules-aware tool**, который умеет считать, валидировать, экспортировать и помогать мастеру в игре.

---

## 1. Приоритеты

### P0 — сначала

Эти задачи нужно закрыть первыми, потому что они влияют на дальнейшую разработку и работу AI-агентов.

1. Синхронизировать документацию с фактическим кодом.
2. Добавить импорт/экспорт персонажей в JSON.
3. Сделать полный printable/PDF-friendly лист персонажа.
4. Добавить XP history / audit log.
5. Структурировать свойства предметов и эффекты заклинаний как данные.

### P1 — основа rules engine

1. Структурировать оружие/атаки NPC.
2. Расширить модели архетипов/видов и карьер.
3. Добавить минимальный attack/damage roller.
4. Добавить базовую автоматизацию активных талантов и героических способностей.
5. Улучшить генератор NPC под RoT.

### P2 — удобство и публичный запуск

1. Реальные URL/deep links.
2. Email provider и публично пригодный password reset.
3. E2E smoke tests.
4. Backup/monitoring/rollback для VPS.
5. Документация для пользователей и мастера.

---

## 2. Общие принципы реализации

### 2.1. Не копировать полный текст книг

В публичной части проекта нельзя хранить дословные тексты правил из книг. Для встроенного контента использовать:

- русские названия;
- английские названия для маппинга;
- числовые параметры;
- структуру правил;
- короткое paraphrase/safe-описание;
- ссылку на источник: книга, раздел, страница.

Полные приватные описания, если нужны, должны оставаться в private-content и не попадать в public-safe pipeline.

### 2.2. Правила должны становиться данными

Если правило влияет на расчёты, фильтрацию, генерацию, броски или отображение, оно не должно оставаться только строкой.

Плохой вариант:

```csharp
Properties = "Точное 1, Оборонительное 2"
```

Лучший вариант:

```json
[
  { "qualityCode": "accurate", "rating": 1 },
  { "qualityCode": "defensive", "rating": 2 }
]
```

### 2.3. Не пытаться автоматизировать всё сразу

Genesys допускает много нарративных и условных эффектов. Поэтому нужно разделить эффекты на уровни автоматизации:

- `PassiveNumeric` — можно считать автоматически.
- `ActivationCost` — можно показать, но эффект применяет игрок/мастер.
- `TimedEffect` — можно повесить на сцену/персонажа с длительностью.
- `ManualEffect` — только текст/подсказка.
- `RequiresGMDecision` — эффект требует решения мастера.

---

## 3. Задачи

## GF-001. Синхронизировать документацию с кодом

### Проблема

Документация частично устарела. Например, в старых документах может быть указано, что refresh tokens/session rotation не реализованы, хотя в коде уже есть `RefreshToken`, `RefreshTokenService`, refresh cookie и тесты.

### Что сделать

Обновить:

- `README.md`
- `docs/current-state.md`
- `docs/feature-roadmap.md`
- `docs/mvp-ux-account-readiness.md`
- `docs/api.md`, если там есть устаревшие endpoint-описания.

### Нужно отразить

- Refresh token flow уже реализован или частично реализован.
- Password reset endpoints есть, но email delivery пока stub/logging.
- Game Table и encounters уже реализованы.
- Magic Action Builder уже реализован.
- Полный printable character sheet ещё не реализован.
- Import/export персонажа ещё не реализован.
- XP/audit history ещё не реализована.
- Active talents / heroic abilities не автоматизированы полностью.
- Weapon attack stats пока в основном описательные.

### Definition of Done

- Документы не противоречат коду.
- В `docs/current-state.md` есть актуальные разделы: Implemented, Partially implemented, Not implemented, Technical risks, Domain gaps.
- В `docs/feature-roadmap.md` задачи распределены по MVP/Beta/1.0/Future.
- В README кратко указано актуальное состояние проекта.

---

## GF-002. Импорт/экспорт персонажа в JSON

### Цель

Позволить пользователю выгрузить персонажа в JSON и загрузить обратно.

### Зачем

- резервные копии;
- перенос персонажа между аккаунтами;
- отладка;
- обмен с мастером;
- подготовка тестовых данных;
- будущая совместимость с внешними форматами.

### Backend

Добавить endpoints:

```text
GET  /api/characters/{id}/export
POST /api/characters/import
```

Опционально:

```text
POST /api/characters/import/preview
```

### Формат JSON

Минимальный формат:

```json
{
  "format": "genesysforge.character.v1",
  "exportedAt": "2026-06-24T00:00:00Z",
  "character": {
    "name": "Name",
    "system": "RealmsOfTerrinoth",
    "archetypeCode": "rot.species.human",
    "careerCode": "rot.career.warrior",
    "characteristics": {
      "brawn": 2,
      "agility": 2,
      "intellect": 2,
      "cunning": 2,
      "willpower": 2,
      "presence": 2
    },
    "totalXp": 100,
    "spentXp": 80,
    "money": 250,
    "isCreationPhase": false,
    "skills": [],
    "talents": [],
    "items": [],
    "heroicAbilityCode": null,
    "heroicUpgradeRank": 0,
    "notes": []
  }
}
```

### Важные правила

- Не экспортировать `OwnerUserId`.
- Не экспортировать internal database ids как основной ключ. Использовать stable `Code`, `Name`, `NameRu`.
- При импорте built-in content маппить по `Code`.
- Если встроенный контент не найден, пытаться маппить по `System + Name`.
- Custom content либо импортировать вместе с персонажем, либо помечать как unresolved reference.
- Импорт не должен перезаписывать существующего персонажа без явного действия.
- Добавить `formatVersion`.

### Frontend

Добавить кнопки:

- “Экспорт JSON” в листе персонажа.
- “Импорт персонажа” на странице списка персонажей.
- Preview перед импортом: имя, система, архетип, карьера, XP, предупреждения.

### Тесты

Backend:

- export returns valid JSON;
- import creates new character;
- import rejects invalid format;
- import handles missing references;
- user cannot export another user’s character.

Frontend:

- кнопка экспорта скачивает файл;
- импорт показывает preview;
- ошибки импорта отображаются пользователю.

### Definition of Done

- Пользователь может экспортировать персонажа и импортировать его обратно.
- Импортированный персонаж открывается и имеет те же основные значения.
- Нет зависимости от старых database ids.

---

## GF-003. Полный printable/PDF-friendly character sheet

### Цель

Сделать страницу печати полного листа персонажа.

### Что должно быть на листе

1. Основная информация:
   - имя;
   - система;
   - архетип/вид;
   - карьера;
   - текущий XP / потраченный XP;
   - деньги.

2. Характеристики:
   - Brawn / Мощь;
   - Agility / Ловкость;
   - Intellect / Интеллект;
   - Cunning / Хитрость;
   - Willpower / Воля;
   - Presence / Харизма.

3. Производные параметры:
   - wounds;
   - strain;
   - soak;
   - melee defense;
   - ranged defense;
   - encumbrance threshold;
   - current encumbrance.

4. Навыки:
   - название RU/EN;
   - карьерный;
   - ранг;
   - характеристика;
   - dice pool.

5. Таланты:
   - tier;
   - ranked/unranked;
   - activation;
   - краткий эффект.

6. Героическая способность:
   - название;
   - activation cost;
   - duration;
   - frequency;
   - current upgrade rank;
   - эффект.

7. Инвентарь:
   - equipped;
   - backpack;
   - quantity;
   - encumbrance;
   - combat stats for weapons;
   - armor effects.

8. Заметки персонажа.

### UI

Добавить:

```text
/characters/:characterId/print
```

или кнопку “Печать листа” в SheetPage.

### CSS

Добавить print styles:

```css
@media print {
  .topbar, .no-print { display: none; }
  .sheet-print { page-break-inside: avoid; }
}
```

### Definition of Done

- Лист удобно печатается через browser print.
- На A4/Letter информация не ломается.
- Навигация и кнопки не печатаются.
- Пользователь может сохранить лист в PDF через системный print dialog.

---

## GF-004. XP history / audit log

### Цель

Добавить историю изменений XP и важных изменений листа.

### Зачем

Genesys-персонаж развивается через XP. Сейчас есть `TotalXp` и `SpentXp`, но нет понятной истории: за что выдали XP, что купили, что отменили.

### Новые сущности

```csharp
public class CharacterAuditEntry
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public CharacterAuditAction Action { get; set; }
    public string Summary { get; set; } = "";
    public int? XpDelta { get; set; }
    public int TotalXpAfter { get; set; }
    public int SpentXpAfter { get; set; }

    public string DataJson { get; set; } = "";
}
```

Enum:

```csharp
public enum CharacterAuditAction
{
    XpAwarded,
    CharacteristicBought,
    CharacteristicRefunded,
    SkillRankBought,
    SkillRankRefunded,
    TalentBought,
    TalentRefunded,
    ItemBought,
    ItemSold,
    ItemRemoved,
    HeroicAbilityChanged,
    CreationCompleted,
    ManualEdit
}
```

### Backend

Добавить:

```text
GET  /api/characters/{id}/audit
POST /api/characters/{id}/xp-awards
```

При покупке/возврате характеристик, навыков, талантов и предметов автоматически писать audit entry.

### Frontend

Добавить вкладку “История”:

- список операций;
- дата;
- тип;
- описание;
- изменение XP;
- состояние после операции.

### Правила

- Audit log не должен ломать существующие операции.
- Ошибка записи audit должна приводить к откату основной операции, если операция и audit в одной транзакции.
- Для ручного изменения `TotalXp` нужно писать entry `ManualEdit` или `XpAwarded`.

### Definition of Done

- Все XP операции попадают в историю.
- Видно, почему `SpentXp` изменился.
- Можно отследить покупку и refund.

---

## GF-005. Структурировать свойства предметов и эффекты заклинаний

### Проблема

Сейчас свойства предметов часто хранятся строкой. Это мешает:

- фильтрации;
- тултипам;
- валидации;
- импорту NPC;
- attack roller;
- автоматизации эффектов.

### Новые справочники

```csharp
public class QualityDef
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string NameEn { get; set; } = "";
    public string NameRu { get; set; } = "";
    public QualityKind Kind { get; set; } // ItemQuality, SpellAdditionalEffect
    public bool HasRating { get; set; }
    public string DefaultActivationCost { get; set; } = "";
    public string Description { get; set; } = "";
    public string SafeDescription { get; set; } = "";
    public string Source { get; set; } = "";
}

public class ItemQualityValue
{
    public Guid Id { get; set; }
    public Guid ItemDefId { get; set; }
    public Guid QualityDefId { get; set; }
    public int? Rating { get; set; }
}
```

Для spell effects можно либо использовать тот же `QualityDef`, либо отдельный `SpellEffectDef`.

### Миграция

- Сохранить старое поле `Properties` временно.
- Добавить новое поле/таблицу `ItemQualityValues`.
- Написать parser для старых строк:
  - `Точное 1`
  - `Оборонительное 2`
  - `Pierce 3`
  - `Burn 2`
- На UI показывать новое структурное представление, но fallback на старую строку оставить.

### Frontend

- Tooltip по свойству.
- Фильтр по свойству.
- Отображение rating отдельно.
- Иконка/бейдж для активных свойств.
- В форме custom item добавить селектор свойства + rating.

### Definition of Done

- У предметов есть структурированные свойства.
- Старые предметы не ломаются.
- В UI видны tooltips.
- Можно фильтровать предметы по свойствам.
- В дальнейшем attack roller может использовать эти свойства.

---

## GF-006. Расширить модели архетипов/видов

### Проблема

`ArchetypeDef` хранит характеристики, пороги, XP, описание и source. Но видовые способности, стартовые навыки и выборы не выделены как данные.

### Что добавить

```csharp
public class ArchetypeAbilityDef
{
    public Guid Id { get; set; }
    public Guid ArchetypeId { get; set; }
    public string Code { get; set; } = "";
    public string NameRu { get; set; } = "";
    public string NameEn { get; set; } = "";
    public string Description { get; set; } = "";
    public string SafeDescription { get; set; } = "";
    public ArchetypeAbilityAutomationKind AutomationKind { get; set; }
}

public class ArchetypeStartingSkill
{
    public Guid Id { get; set; }
    public Guid ArchetypeId { get; set; }
    public string SkillName { get; set; } = "";
    public int FreeRanks { get; set; }
    public bool IsChoice { get; set; }
    public string ChoiceGroup { get; set; } = "";
}
```

### Примеры данных

- фиксированный стартовый ранг навыка;
- выбор одного навыка из списка;
- special ability;
- special resistance;
- special movement;
- unusual wound/strain rule.

### Frontend

На выборе вида показывать:

- характеристики;
- wounds/strain/xp;
- стартовые навыки;
- способности вида;
- предупреждения по выборам.

### Definition of Done

- Видовые способности не спрятаны только в описании.
- Стартовые навыки вида можно применять автоматически при создании.
- Для будущих видов не нужно писать логику в коде.

---

## GF-007. Расширить модели карьер

### Проблема

`CareerDef` сейчас хранит список карьерных навыков, но не хранит структурно:

- стартовое снаряжение RoT;
- варианты выбора;
- специальные правила;
- ограничения магических навыков;
- подсказки по созданию.

### Что добавить

```csharp
public class CareerStartingGear
{
    public Guid Id { get; set; }
    public Guid CareerId { get; set; }
    public string ItemCode { get; set; } = "";
    public string ItemNameFallback { get; set; } = "";
    public int Quantity { get; set; }
    public bool IsChoice { get; set; }
    public string ChoiceGroup { get; set; } = "";
}

public class CareerRule
{
    public Guid Id { get; set; }
    public Guid CareerId { get; set; }
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public CareerRuleKind Kind { get; set; }
}
```

### Создание персонажа

На этапе создания добавить:

- выбор бесплатных карьерных рангов;
- выбор стартового снаряжения, если система RoT;
- применение стартового снаряжения в инвентарь;
- валидацию количества выбранных опций.

### Definition of Done

- RoT-карьеры могут выдавать стартовое снаряжение.
- Стартовое снаряжение появляется в инвентаре персонажа.
- Правила выбора отображаются в UI.
- Карьерные навыки остаются data-driven.

---

## GF-008. Структурировать оружие и атаки NPC

### Проблема

`Npc.Equipment` сейчас `List<string>`. Этого недостаточно для автоматического боя, карточек NPC и импорта adversaries.

### Новые сущности

```csharp
public class NpcAttack
{
    public Guid Id { get; set; }
    public Guid NpcId { get; set; }
    public string Name { get; set; } = "";
    public string SkillName { get; set; } = "";
    public string Damage { get; set; } = "";
    public int? Critical { get; set; }
    public string RangeBand { get; set; } = "";
    public string Notes { get; set; } = "";
    public List<NpcAttackQuality> Qualities { get; set; } = [];
}

public class NpcAttackQuality
{
    public Guid Id { get; set; }
    public Guid NpcAttackId { get; set; }
    public string QualityCode { get; set; } = "";
    public string NameRu { get; set; } = "";
    public int? Rating { get; set; }
}
```

### UI

В форме NPC:

- отдельная секция “Атаки”;
- добавить/редактировать атаку;
- skill dropdown;
- damage;
- crit;
- range;
- qualities selector.

В карточке NPC:

- выводить атаки как статблок;
- показывать свойства как бейджи с tooltip.

### Import

При импорте из внешних JSON:

- `weapons[]` маппить в `NpcAttack`;
- `gear[]` маппить в свободное снаряжение;
- `qualities[]` парсить в структурированные свойства.

### Definition of Done

- NPC может иметь несколько структурированных атак.
- Encounter/Game Table видит атаки NPC.
- Карточка NPC пригодна для игры за столом.
- Старое свободное equipment поле можно оставить для небоевого снаряжения.

---

## GF-009. Улучшить валидацию NPC

### Текущее состояние

Валидация уже проверяет:

- имя;
- характеристики 1..6;
- wound threshold > 0;
- soak/defense неотрицательные;
- strain threshold неотрицательный;
- skill ranks 0..5;
- nemesis имеет strain threshold.

### Добавить правила

1. Minion:
   - strain threshold должен быть null;
   - не должен иметь слишком много индивидуальных активных способностей;
   - навыки должны восприниматься как group skills.

2. Rival:
   - может иметь strain threshold, но это опционально;
   - strain может использоваться только если задан.

3. Nemesis:
   - strain threshold обязателен;
   - рекомендуется минимум один defensive/social/combat hook;
   - Adversary talent желательно предупреждать, но не требовать.

4. Defense:
   - warning если melee/ranged defense > 4;
   - error если defense > 6 без override.

5. Skills:
   - magic skill в RoT должен соответствовать доступным magical actions;
   - ranks > 5 запрещены.

6. Attacks:
   - у атаки должен быть skill;
   - damage обязателен;
   - crit должен быть положительным числом или пустым;
   - range обязателен;
   - qualities должны существовать в справочнике или быть custom.

### Тип результата

Сделать validation result не только exception, но и список warnings:

```csharp
public class NpcValidationResult
{
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
```

### Definition of Done

- Ошибки блокируют сохранение.
- Warnings показываются пользователю, но не блокируют.
- QuickDraft NPC создаёт валидные NPC без errors.

---

## GF-010. Улучшить NPC Draft Generator под Realms of Terrinoth

### Цель

Сделать генератор NPC полезным для мастера RoT, а не только общим черновиком.

### Добавить шаблоны

Роли:

- Brute;
- Skirmisher;
- Archer;
- Caster;
- Leader;
- Social;
- Support;
- Monster;
- Undead;
- Beast;
- Dragon;
- Demon;
- Construct;
- Minion Swarm.

### Добавить параметры

```text
setting: Core / RealmsOfTerrinoth
kind: Minion / Rival / Nemesis
role
powerLevel
combatStyle
creatureTags
magicSkill
environment
```

### Для RoT

Если `System = RealmsOfTerrinoth`:

- использовать навыки RoT:
  - Melee (Light)
  - Melee (Heavy)
  - Ranged
  - Runes
  - Verse
  - Knowledge (Lore)
- не использовать Core-only skills вроде Computers, Driving, Operating;
- для magic NPC добавлять подходящий magic skill;
- для undead добавлять tags/abilities: нежить, сопротивления, terror-like effect, если нужно;
- для beast/monster добавлять natural weapons.

### Definition of Done

- Генератор создаёт разные NPC для RoT и Core.
- RoT NPC не получают неуместные Core-навыки.
- Генератор создаёт структурированные `NpcAttack`.
- Генератор добавляет теги и предупреждения.

---

## GF-011. Минимальный attack/damage roller

### Цель

Добавить первый рабочий слой боевых бросков.

### Scope v1

Не пытаться полностью автоматизировать Genesys combat. Сделать:

- выбрать персонажа или NPC;
- выбрать оружие/атаку;
- показать skill + characteristic;
- собрать базовый dice pool;
- вручную добавить difficulty/boost/setback/upgrade;
- выполнить бросок или подготовить pool;
- вручную ввести net successes/advantages/threat/triumph/despair;
- посчитать базовый damage;
- показать доступные качества для активации.

### Backend или frontend?

Для v1 можно сделать frontend-only calculator, если броски не должны сохраняться.

Для campaign/Game Table лучше добавить backend-сущность:

```csharp
public class RollLogEntry
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid? ActorCharacterId { get; set; }
    public Guid? ActorNpcId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string PoolJson { get; set; } = "";
    public string ResultJson { get; set; } = "";
    public string Summary { get; set; } = "";
}
```

### Definition of Done

- Можно выбрать оружие и увидеть расчёт базового урона.
- Свойства оружия показываются рядом с ценой активации.
- Roller не претендует на автоматическое принятие всех решений за мастера.

---

## GF-012. Автоматизация талантов и героических способностей

### Проблема

Таланты и героические способности в основном отображаются как текст. Пассивные числовые эффекты частично автоматизированы, но активные эффекты нет.

### Подход

Ввести систему “effect descriptors”:

```csharp
public class RuleEffectDef
{
    public string Code { get; set; } = "";
    public RuleEffectSource Source { get; set; } // Talent, HeroicAbility, Item, Spell
    public RuleEffectTiming Timing { get; set; } // Passive, Action, Maneuver, Incidental, OutOfTurn
    public RuleEffectAutomation Automation { get; set; } // Automatic, Prompt, Manual
    public string Target { get; set; } = "";
    public string DataJson { get; set; } = "";
}
```

### Начать с простого

Автоматизировать:

- +wounds threshold;
- +strain threshold;
- +soak;
- +melee defense;
- +ranged defense;
- +encumbrance threshold;
- heal wounds;
- heal strain;
- spend story points;
- add boost/setback to next check;
- temporary effect duration.

Не автоматизировать в v1:

- сложные нарративные эффекты;
- эффекты с решением мастера;
- эффекты, меняющие сцену без числового результата.

### UI

Для активных талантов/героик:

- кнопка “Активировать”;
- показать стоимость;
- применить автоматические части;
- создать timed effect, если есть duration;
- записать в audit/log.

### Definition of Done

- Простые активные эффекты можно нажать и применить.
- Сложные эффекты показываются как manual prompt.
- Пользователь видит, что именно было применено автоматически.

---

## GF-013. Реальные URL/deep links

### Цель

Сделать нормальную URL-навигацию.

### Маршруты

```text
/login
/register
/characters
/characters/:characterId
/characters/:characterId/print
/campaigns
/campaigns/:campaignId
/campaigns/:campaignId/encounters/:encounterId
/campaigns/:campaignId/table
/npcs
/npcs/:npcId
/magic
```

### Варианты реализации

1. Минимальный custom router на `history.pushState`.
2. React Router, если можно добавить зависимость.

### Требования

- Refresh страницы сохраняет текущий экран.
- Back/Forward работают.
- После login пользователь возвращается на изначальный URL.
- Если entity не найдена — показывать нормальный 404/empty state.
- Нельзя показывать данные без авторизации.

### Definition of Done

- Ссылку на персонажа/NPC/кампанию можно скопировать и открыть.
- Browser refresh не сбрасывает пользователя на список.
- Навигация не хранится только в React state.

---

## GF-014. Email provider и password reset для публичного запуска

### Текущее состояние

Password reset endpoints есть, но отправка писем — stub/logging.

### Что сделать

Выбрать provider:

- Resend;
- Mailgun;
- SendGrid;
- SMTP.

Добавить интерфейс:

```csharp
public class SmtpEmailSender : IEmailSender
{
    public Task SendPasswordResetAsync(string email, string rawToken, CancellationToken ct);
}
```

Конфигурация:

```text
Email__Provider
Email__From
Email__Smtp__Host
Email__Smtp__Port
Email__Smtp__Username
Email__Smtp__Password
```

### Security

- reset request всегда возвращает 204;
- токен хранить только hash;
- expiry 15–60 минут;
- single-use;
- rate limiting;
- logout/revoke sessions after password reset.

### Frontend

- Forgot password screen;
- Reset password screen;
- success/error states;
- no account enumeration.

### Definition of Done

- Пользователь может восстановить пароль без доступа к логам сервера.
- Reset token одноразовый.
- Повторное использование токена невозможно.
- Есть тесты на expiry/reuse/invalid token.

---

## GF-015. E2E smoke tests

### Цель

Проверить основные пользовательские сценарии браузером.

### Инструмент

Playwright.

### Минимальный набор сценариев

1. Register → create character → open sheet.
2. Buy skill rank → refund.
3. Buy talent → refund.
4. Add item → equip → derived stats changed.
5. Create campaign → join/add character.
6. Create NPC → duplicate → add to encounter.
7. Create encounter → send to Game Table.
8. Open Magic Builder → build action → print/copy.
9. Export character → import character.
10. Password reset request screen.

### CI

Добавить job:

```yaml
e2e:
  runs-on: ubuntu-latest
  steps:
    - checkout
    - setup node
    - docker compose up -d --build
    - npx playwright install --with-deps
    - npm run test:e2e
```

### Definition of Done

- E2E можно запускать локально.
- E2E запускается в CI или отдельным manual workflow.
- Есть хотя бы smoke coverage главных flows.

---

## GF-016. Production hardening

### Что добавить

1. Backup automation:
   - ежедневный `pg_dump`;
   - хранение архивов;
   - инструкция restore.

2. Monitoring:
   - health endpoint для API;
   - health check DB;
   - basic uptime monitor;
   - structured logs.

3. Security:
   - rate limiting для auth endpoints;
   - security headers;
   - CORS только нужные origin;
   - Secure cookies в production;
   - JWT key length validation на старте;
   - secret rotation notes.

4. Deploy:
   - rollback по previous image tag;
   - release checklist;
   - changelog.

5. PublicSafe stack:
   - отдельный public hostname;
   - ContentMode=PublicSafe;
   - убедиться, что private-content не попадает в public image.

### Definition of Done

- Есть инструкция backup/restore.
- Есть инструкция rollback.
- Health checks покрывают API и DB.
- PublicSafe deployment не содержит private content.
- Auth endpoints защищены от простого brute force.

---

## GF-017. User-facing help pages

### Цель

Добавить справку без нарушения авторских прав.

### Разделы

- Как создать персонажа.
- Как работают карьерные навыки.
- Как покупать навыки/таланты.
- Как работает инвентарь.
- Как пользоваться магией.
- Как создавать NPC.
- Как строить encounter.
- Как пользоваться Game Table.
- Что означает PublicSafe/PrivateFull контент.

### Формат

Можно начать с Markdown в `docs/user-guide/`, потом отобразить в UI.

### Definition of Done

- Новый пользователь может понять базовый flow без чтения README.
- Help pages не копируют официальный текст книг.
- В UI есть ссылка “Справка”.

---

## 4. Рекомендуемый порядок выполнения

### Этап 1 — стабилизация

1. GF-001 Docs sync.
2. GF-013 URL/deep links.
3. GF-002 Character import/export.
4. GF-003 Printable character sheet.

### Этап 2 — правила как данные

1. GF-005 Structured qualities.
2. GF-006 Archetype/species structured rules.
3. GF-007 Career structured rules.
4. GF-008 NPC attacks.

### Этап 3 — игровые операции

1. GF-004 XP/audit log.
2. GF-011 Attack/damage roller.
3. GF-012 Talent/heroic automation.
4. GF-010 Better NPC generator.

### Этап 4 — production/public

1. GF-014 Email provider/password reset.
2. GF-015 E2E tests.
3. GF-016 Production hardening.
4. GF-017 User-facing help.

---

## 5. Что можно отложить

Эти вещи не нужны до стабильной Beta:

- marketplace/community sharing;
- полноценный offline-first;
- CRDT/real-time co-editing character sheets;
- поддержка всех возможных Genesys setting;
- полный официальный rules compendium;
- автоматизация всех талантов и всех магических эффектов;
- мобильное приложение;
- интеграция с Foundry/Roll20.

---

## 6. Первый набор задач для Claude/Codex

Ниже набор задач, которые можно давать AI-агенту по одной.

### Task A — обновить docs/current-state.md

```text
Обнови docs/current-state.md так, чтобы он соответствовал текущему коду. Проверь refresh token flow, password reset, Game Table, encounters, magic builder, content packs, print cards. Удали устаревшие утверждения, что refresh tokens не реализованы, если код показывает обратное. Не меняй бизнес-логику.
```

### Task B — добавить export character endpoint

```text
Добавь GET /api/characters/{id}/export. Endpoint должен возвращать JSON формата genesysforge.character.v1 без OwnerUserId и без зависимости от database ids. Built-in references должны экспортироваться по Code, fallback по Name. Добавь API tests.
```

### Task C — добавить import character preview

```text
Добавь POST /api/characters/import/preview. Endpoint принимает JSON export-файл, валидирует format/version, пытается сопоставить archetype/career/talents/items/heroic ability и возвращает preview с warnings/errors. Ничего не сохраняет в БД.
```

### Task D — structured item qualities

```text
Добавь справочник QualityDef и связь ItemDef -> ItemQualityValue. Сохрани backward compatibility со старым ItemDef.Properties. Добавь parser для строк свойств. Покажи свойства в UI как бейджи с tooltip.
```

### Task E — NPC structured attacks

```text
Добавь NpcAttack и NpcAttackQuality. Перенеси боевые строки NPC из Equipment в отдельный список атак, но оставь Equipment для обычного снаряжения. Обнови Npc DTO, endpoints, UI и print card.
```

### Task F — printable character sheet

```text
Добавь страницу печати персонажа /characters/:id/print или кнопку Print Sheet. Используй существующий CharacterSheet DTO. Добавь print CSS, чтобы topbar/buttons не печатались. Лист должен включать характеристики, навыки, таланты, инвентарь, героику, derived stats и заметки.
```

---

## 7. Definition of Done для ближайшей Beta

Beta можно считать готовой, когда:

- документация синхронизирована с кодом;
- персонажа можно экспортировать/импортировать;
- персонажа можно печатать;
- есть история XP/важных операций;
- свойства предметов структурированы;
- NPC имеют структурированные атаки;
- есть базовый attack/damage roller;
- основные flows покрыты E2E smoke tests;
- password reset работает через реальный email provider;
- есть backup/restore инструкция для VPS;
- PublicSafe режим проверен и не содержит private content.
