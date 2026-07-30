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

Fields: `Id`, `System`, `Name`, `Kind`, `Encumbrance`, `SoakBonus`, `MeleeDefense`, `RangedDefense`, `EncumbranceThresholdBonus`, `Description`, nullable `Price`/`Rarity`, `Purchasable`, `Sellable`, `OwnerUserId`.

Rules:

- Equipped item bonuses affect derived stats.
- Description is informational only in current mechanics.
- `RuneboundShardRules` recognizes the exact 17 built-in shard codes and exposes their
  activation profile, Runes-only implement effects, attack damage bonus, strain reduction
  and final-difficulty reduction as typed data.
- A shard can be the sole implement of a Runes action only when Runes is a career skill with
  at least one rank. It does not substitute for ordinary implements of other magic skills.

### HeroicAbilityDef

Fields: `Id`, content-model fields (`Code`, `Name`, `NameRu`, `Description`, `SafeDescription`, `Source`),
the activation card (`Requirement`, `ActivationCost`, `Activation`, `Duration`, `Frequency`, `Notes`),
`OwnerUserId`, and `Upgrades`.

Built-in abilities (Realms of Terrinoth) are loaded from the embedded `heroics.catalog.json` catalog
(`HeroicCatalog`), generated from the user CSV by `_books/_heroic_abilities/gen-heroics-catalog.mjs`.

### Heroic Ability upgrades

`HeroicAbilityUpgradeDef` stores the ability-specific **Power** upgrades. Fields: `Id`, `HeroicAbilityDefId`, `Level`
(`HeroicUpgradeLevel`: `Improved`=1, `Supreme`=2), `Cost` (1 and 2 ability points), `Description`, `Notes`.

Rules:

- Assignable to Realms of Terrinoth characters; Genesys Core assignment is rejected.
- A Realms of Terrinoth character cannot complete creation until a Heroic Ability is selected.
- There is no free starting ability point. Total points are
  `max(0, TotalXp − archetype.StartingXp) / 50`; additional XP granted to an experienced character
  during creation therefore counts, while species starting XP does not.
- Power is sequential and cumulative: Improved costs 1; Supreme requires Improved and costs 2 more.
  `HeroicUpgradeRank` remains the persisted 0/1/2 Power rank for backward compatibility.
- `HeroicDurationRanks` costs 1 per rank and adds one turn per rank; it is repeatable.
- `HeroicFrequencyRanks` costs 2 per rank and adds one use per session per rank; it is repeatable.
- `HeroicStoryUpgrade` costs 1, is purchased once, and reduces activation to one Story Point.
- Up to two different `HeroicSecondaryEffectDef` rows can be selected through
  `CharacterHeroicSecondaryEffect`; each costs 1.
- Purchases can be corrected during `IsCreationPhase`. After creation they are permanent and can only
  increase. The selected Heroic Ability also cannot be changed after creation.
- Changing/clearing the ability during creation resets all its upgrades.
- Custom primary abilities have no built-in Power definitions, but can use the universal upgrades.
- On Game Table, PC activation spends 2 player Story Points (1 with Story), flips them to the GM pool,
  and increments `GameParticipant.HeroicAbilityUses`; Frequency sets the session limit.

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

Fields: `Id`, `System`, `Name`, six characteristics, `WoundBase`, `StrainBase`, `StartingXp`, `Description`, `Retired`, `Abilities`, `StartingSkills`.

Rules:

- Used as character starting characteristics and XP source.
- `Abilities` (`ArchetypeAbilityDef`: `Code`, `NameRu`, `NameEn`, `SafeDescription`, `AutomationKind`) are the species abilities as data, shown when picking a species. `AutomationKind` is a classification tag only — effect execution is U-18.
- `StartingSkills` (`ArchetypeStartingSkill`: `SkillName`, `NameRu`, `FreeRanks`, `IsChoice`, `ChoiceGroup`, `ChoiceCount`) drive starting skill ranks at creation. Fixed entries are auto-applied as free ranks (merging with career free ranks); choice entries (e.g. `any-noncareer`, pick N) are picked by the player and validated server-side (count, distinct, non-career).

### CareerDef

Fields: `Id`, `System`, `Name`, `Description`, `CareerSkillNames`, `StartingMoneyFixed`, `StartingMoneyDice`, `StartingGear`, `Rules`.

Rules:

- Career skill names mark matching `CharacterSkill` rows as career skills.
- During character creation, selected free career skills get `FreeRanks`.
- `StartingMoney*` set starting `Money` at creation: `StartingMoneyFixed` + a roll of `StartingMoneyDice` (`NdM`, e.g. `1d100`) — RoT careers only.
- `StartingGear` (`CareerStartingGear`) is granted at creation: fixed rows auto-added to inventory; choice slots (`ChoiceGroup`, options by `ChoiceOption`) resolved by the player's `CareerGearChoice` (lenient — an unselected slot is skipped, not blocked).
- `Rules` (`CareerRule`: `Kind`, `Description`) are advisory career notes shown in the UI (not automated).

### Character

Fields: `Id`, `OwnerUserId`, `Name`, `System`, `ArchetypeId`, `CareerId`, six characteristics,
`TotalXp`, `SpentXp`, `IsCreationPhase`, `WoundsCurrent`, `StrainCurrent`, `Money`,
`HeroicAbilityId`, Power/Duration/Frequency/Story upgrade state, selected secondary effects,
`CreatedAt`, `Skills`, `Talents`, `Items`.

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

Fields: `Id`, `CharacterId`, `ItemDefId`, `Quantity`, `State`, item-instance state and
Lesser Rune configuration (`ShardActivationChoice`, `ShardEffectAction`,
`ShardEffectChoice`, `ShardConfigured`).

Rules:

- State controls whether bonuses apply.
- Quantity affects encumbrance load.
- Runebound shards are always individual instances (`Quantity = 1`); they are granted by the
  GM rather than bought or sold.
- Lesser Rune configuration is immutable and follows the instance through duplicate and
  character v3 export/import.

### MountDef / CharacterMount

Fields (`MountDef`): `Code`, `Name`, `NameRu`, `TransportKind` (Mount or Vehicle), `MovementMode`
(Ground/Flight/Wheeled), `RequiresTraction`, `Kind` (Minion or Rival), six characteristics, `Soak`,
`WoundThreshold`, `StrainThreshold` (null for mounts; a vehicle reads it as a system threshold),
`MeleeDefense`, `RangedDefense`, `Silhouette`, `Capacity`, `Price` (null = priceless), `Rarity`,
`IncludedGear`, `RequiresRidingCheck`, plus `Skills`, `Abilities` and `Attacks`.

Fields (`CharacterMount`): `CharacterId`, `MountDefId`, `Name` (nickname), `Provenance`,
`WoundsCurrent`, `IsActive`, `Notes`, `DrawnByMountId`.

Rules (`MountRules`, ROT-MOUNT-ITEM-01 / ROT-TRANSPORT-01):

- Transport is not an item: it has no encumbrance and never adds to the owner's carried weight.
  Buying one creates a `CharacterMount`, never a `CharacterItem`.
- `Capacity` from the profile wins over the generic `5 + Brawn` rule; a profile without its own
  number falls back to the generic rule. Installed saddlebags add their threshold bonus on top.
- Wounds are clamped to `0..WoundThreshold`; at the threshold the transport is out of action.
- Cargo is per item (`CharacterItem.CarriedByMountId`), and its load is the sum of weight × quantity.
  The "ten zero-weight items make one point" rule is not applied to cargo: it is about what a
  character carries on their person.
- Installed gear (`IsInstalledOnMount`: barding, saddlebags) does not occupy capacity — it changes
  the transport's capacity and protection. Because such rows are excluded from the owner's equipped
  gear, barding never protects the rider; that follows from the exclusion rather than from a rule.
- A vehicle with `RequiresTraction` and no draft animal simply does not move: it is not deleted and
  its cargo does not move to the owner. Only a self-moving mount can draw one.
- Cargo above capacity is refused on an explicit move but kept and flagged when it arrives from
  import — the GM decides what to do with an existing overload.
- Purchase accepts the same payment modes as items (free grant, haggled percent, own price with a
  reason) and sale the same three modes; the server computes every sum.
- Transport carrying cargo cannot be sold until it is unloaded. Deleting it instead returns the cargo
  to the owner, so nothing is ever left without an owner.

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
- Character has many mounts; CharacterMount references MountDef (ROT-MOUNT-ITEM-01).

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
