# Domain Model

## Scope

Документ описывает фактически найденную доменную модель backend-кода. Если правило не найдено в коде, оно отмечено отдельно.

## Enums

- `GameSystem`: `GenesysCore`, `RealmsOfTerrinoth`.
- `CharacteristicType`: `Brawn`, `Agility`, `Intellect`, `Cunning`, `Willpower`, `Presence`.
- `SkillKind`: general/combat/social/knowledge/magic categories.
- `ItemKind`: `Weapon`, `Armor`, `Gear`.
- `ItemState`: `Equipped`, `Carried`, `Backpack`.

## Entities found in code

### User

Fields: `Id`, `Email`, `DisplayName`, `PasswordHash`, `CreatedAt`.

Rules:

- Email is unique.
- Password is stored as hash.

### SkillDef

Fields: `Id`, `System`, `Name`, `Characteristic`, `Kind`, `OwnerUserId`.

Rules:

- `OwnerUserId == null` means built-in skill.
- `OwnerUserId != null` means custom skill owned by a user.

### TalentDef

Fields: `Id`, `System`, `Name`, `Tier`, `IsRanked`, `Description`, `Activation`, `WoundBonus`, `StrainBonus`, `SoakBonus`, `MeleeDefenseBonus`, `RangedDefenseBonus`, `OwnerUserId`.

Rules:

- Tier must be 1..5 in domain/application validation.
- Passive numeric bonuses are applied per purchased rank.
- Text fields must not contain copied official book text.

### ItemDef

Fields: `Id`, `System`, `Name`, `Kind`, `Encumbrance`, `SoakBonus`, `MeleeDefense`, `RangedDefense`, `EncumbranceThresholdBonus`, `Description`, `Price`, `Rarity`, `OwnerUserId`.

Rules:

- Equipped item bonuses affect derived stats.
- Description is informational only in current mechanics.

### HeroicAbilityDef

Fields: `Id`, content-model fields (`Code`, `Name`, `NameRu`, `Description`, `SafeDescription`, `Source`),
the activation card (`Requirement`, `ActivationCost`, `Activation`, `Duration`, `Frequency`, `Notes`),
`OwnerUserId`, and `Upgrades`.

Built-in abilities (Realms of Terrinoth) are loaded from the embedded `heroics.catalog.json` catalog
(`HeroicCatalog`), generated from the user CSV by `_books/_heroic_abilities/gen-heroics-catalog.mjs`.

### HeroicAbilityUpgradeDef

A purchasable upgrade of a heroic ability. Fields: `Id`, `HeroicAbilityDefId`, `Level`
(`HeroicUpgradeLevel`: `Improved`=1, `Supreme`=2), `Cost` (1 and 2 ability points), `Description`, `Notes`.

Rules:

- Assignable to Realms of Terrinoth characters; Genesys Core assignment is rejected.
- Upgrades are bought with **ability points**: 1 starting point + 1 per 50 XP earned after creation
  (`1 + max(0, TotalXp − archetype.StartingXp) / 50`).
- Ranks are sequential and cumulative: `Supreme` requires `Improved` first; `HeroicUpgradeRank`
  (0/1/2) on the character records the highest purchased level. Lowering the rank refunds points.
- Changing or clearing the selected ability resets `HeroicUpgradeRank` to 0.
- Custom abilities carry no upgrades.

### SpellDef

Fields: `Id`, `System`, `MagicSkill`, `Kind` (`SpellEntryKind`: `Effect`/`AdditionalEffect`), `ParentEffect`, `NameRu`, `NameEn`, `Difficulty`, `Description` (full/private paraphrase), `SafeDescription` (copyright-safe public text), `Source` (book/section reference), `SortOrder`, `OwnerUserId`.

Rules:

- Reference-only content; not attached to a character sheet.
- `MagicSkill` set differs per system: Arcana/Divine/Primal for both; Runes/Verse added for Realms of Terrinoth.
- Base effects (`Kind=Effect`) are available only to specific magic skills (availability matrix), seeded one row per (system, skill).
- Additional effects (`Kind=AdditionalEffect`) modify one base effect, referenced by `ParentEffect` (= base effect `NameEn`); they are skill-agnostic.
- `Description` is served in full/private content mode; `SafeDescription` + `Source` are the copyright-safe public surface (forward-compatible with the planned `ContentMode` switch).
- No book text is stored — only structure, numbers and original paraphrases.

### ArchetypeDef

Fields: `Id`, `System`, `Name`, six characteristics, `WoundBase`, `StrainBase`, `StartingXp`, `Description`.

Rules:

- Used as character starting characteristics and XP source.

### CareerDef

Fields: `Id`, `System`, `Name`, `Description`, `CareerSkillNames`.

Rules:

- Career skill names mark matching `CharacterSkill` rows as career skills.
- During character creation, selected free career skills get `FreeRanks`.

### Character

Fields: `Id`, `OwnerUserId`, `Name`, `System`, `ArchetypeId`, `CareerId`, six characteristics, `TotalXp`, `SpentXp`, `IsCreationPhase`, `WoundsCurrent`, `StrainCurrent`, `Money`, `HeroicAbilityId`, `HeroicUpgradeRank`, `CreatedAt`, `Skills`, `Talents`, `Items`.

Rules:

- `AvailableXp = TotalXp - SpentXp`.
- Creation phase gates characteristic upgrades and refund operations.
- Character access is owner-scoped in application handlers.

### CharacterSkill

Fields: `Id`, `CharacterId`, `SkillDefId`, `Ranks`, `IsCareer`, `FreeRanks`.

Rules:

- Unique per character/skill def.
- Rank cap: 2 during creation, 5 overall.
- Free ranks cannot be refunded.

### CharacterTalent

Fields: `Id`, `CharacterId`, `TalentDefId`, `Ranks`.

Rules:

- Unique per character/talent def.
- Ranked talents increment `Ranks`.
- Unranked talents cannot be bought twice.

### CharacterItem

Fields: `Id`, `CharacterId`, `ItemDefId`, `Quantity`, `State`.

Rules:

- State controls whether bonuses apply.
- Quantity affects encumbrance load.

## Value objects found in code

- `CharacteristicsSet`
- `DicePool`
- `DerivedStats`
- `PurchaseResult`
- `SkillInput`
- `SkillComputed`
- `TalentInput`
- `ItemInput`

## Relationships

- User owns characters by `Character.OwnerUserId`.
- User owns custom content by nullable `OwnerUserId`.
- Character references one archetype and one career.
- Character optionally references one heroic ability.
- Character has many skills, talents and items.
- CharacterSkill references SkillDef.
- CharacterTalent references TalentDef.
- CharacterItem references ItemDef.

## Business rules implemented in code

- Dice pool: `proficiency = min(characteristic, ranks)`, `ability = max(characteristic, ranks) - proficiency`.
- Characteristic upgrade cost: `10 * newValue`.
- Characteristic upgrades are allowed only during creation.
- Max characteristic at creation: 5.
- General max characteristic constant exists: 6.
- Skill rank cost: `newRank * 5 + 5 if non-career`.
- Skill max rank at creation: 2.
- Skill max rank overall: 5.
- Talent cost: `tier * 5`.
- Ranked talent effective tier: `min(baseTier + ranksAlreadyOwned, 5)`.
- Talent pyramid must remain valid after buy/refund.
- Wound threshold: archetype wound base + Brawn + talent bonuses.
- Strain threshold: archetype strain base + Willpower + talent bonuses.
- Soak: Brawn + equipped armor soak + talent soak bonuses.
- Defense from items uses max equipped item defense, then adds talent defense bonuses.
- Encumbrance threshold: `5 + Brawn + equipped item threshold bonuses`.
- Equipped armor encumbrance: `max(0, armorEncumbrance - 3)`.
- Encumbered when load exceeds threshold.
- Custom content is user-scoped.

## Business rules assumed future feature

- Full automation of active talent effects.
- Structured heroic ability mechanics.
- Structured weapon attack/damage resolution.
- Character sharing and campaign membership.
- XP history/audit log.

## Domain decisions to clarify

- Whether current seed descriptions are legally safe.
- Whether `HeroicAbilityDef` should include `System`.
- Whether custom heroic abilities should be restricted to Terrinoth or can exist globally but only be assigned to Terrinoth characters.
- Whether `CareerSkillNames` should be normalized into a table.
- Whether database constraints should duplicate domain constraints.

