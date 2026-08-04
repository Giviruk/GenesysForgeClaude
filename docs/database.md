# Database

Current database is PostgreSQL via EF Core. Tests can use EF Core InMemory database when `UseInMemoryDatabase` is configured.

## DbContext

`AppDbContext` is in `backend/src/GenesysForge.Infrastructure/Persistence/AppDbContext.cs`.

DbSets:

- `Users`
- `SkillDefs`
- `TalentDefs`
- `ItemDefs`
- `HeroicAbilityDefs`
- `HeroicAbilityUpgradeDefs`
- `ArchetypeDefs`
- `ArchetypeAbilityDefs`
- `ArchetypeStartingSkills`
- `CareerDefs`
- `CareerStartingGears`
- `CareerRules`
- `Characters`
- `CharacterShareTokens`
- `CharacterSkills`
- `CharacterTalents`
- `CharacterItems`
- `SpellDefs`
- `ContentPacks`
- `ContentPackEntries`
- `HomebrewPacks`
- `HomebrewPackCharacters`
- `HomebrewPackCampaigns`
- `RollLogEntries`

## Tables and purpose

### Users

Stores application users.

Important fields:

- `Id`
- `Email`
- `DisplayName`
- `PasswordHash`
- `CreatedAt`

Indexes:

- unique `Email`.

### PasswordResetTokens

Single-use hashed password reset tokens.

Indexes:

- non-unique `TokenHash`.
- non-unique `UserId`.
- non-unique `(UserId, ExpiresAt, UsedAt)` for active/expired token cleanup and account-scoped lookup.

### RefreshTokens

Hashed refresh tokens with rotation families.

Indexes:

- unique `TokenHash`.
- non-unique `FamilyId`.
- non-unique `UserId`.
- non-unique `(UserId, ExpiresAt, RevokedAt)` for session cleanup and active-token queries.

### Content model (shared reference fields)

All built-in reference entities (`SkillDefs`, `TalentDefs`, `ItemDefs`, `ArchetypeDefs`, `CareerDefs`, `HeroicAbilityDefs`) implement `IContentDef` and carry a shared content model:

- `Code` — stable key for built-in content (e.g. `gc.talent.parry`, `rot.item.plate-armor`); empty for custom content. Key for the private-content description overlay. `varchar(80)`.
- `Name` — original/English name (also the seed idempotency key).
- `NameRu` — Russian name. `varchar(160)`.
- `Description` — full (private) paraphrase; emitted only in `ContentMode.PrivateFull`, cleared in `PublicSafe`.
- `SafeDescription` — copyright-safe public text.
- `Source` — book/section reference, available in both modes. `varchar(160)`.

Visibility is governed by `OwnerUserId` (null = built-in, non-null = custom), optional `HomebrewPackId`
for imported user packs, and the seed `ContentMode`. `SpellDefs` already carried
`NameRu`/`Description`/`SafeDescription`/`Source` (see below).

### SkillDefs

Built-in and custom skill definitions.

Fields include `System`, content-model fields, `Characteristic`, `Kind`, `OwnerUserId`, `HomebrewPackId`.

Indexes include non-unique `HomebrewPackId` and `(System, OwnerUserId)` for built-in/custom reference visibility lookups.

### TalentDefs

Built-in and custom talent definitions.

Fields include `System`, content-model fields, `Tier`, `IsRanked`, `Category`, `Setting`, `Activation`, passive bonus fields, `OwnerUserId`, `HomebrewPackId`.

Indexes include non-unique `HomebrewPackId` and `(System, OwnerUserId)` for built-in/custom reference visibility lookups.

`Setting` is a `[Flags] GenesysSetting` (`Any`, `Fantasy`, `Steampunk`, `WeirdWar`, `ModernDay`, `ScienceFiction`, `SpaceOpera`) controlling which game systems list the talent: Genesys Core shows only `Any`; Realms of Terrinoth shows `Any` + `Fantasy`. Custom talents (owned) are always visible to their owner regardless of setting.

`Category` is a `TalentCategory` enum (`General`, `Social`, `Combat`, `Magic`) used by the frontend talent filter. It does not affect XP cost, pyramid validation or purchase rules.

Built-in talents are not hand-written in `SeedData`; they are loaded from the embedded catalog `Persistence/SeedContent/talents.catalog.json` (see `TalentCatalog`). The catalog is generated from the source CSVs (structure + reworked Russian descriptions, not book text). Each catalog entry is expanded into `TalentDef` rows per system by its setting (`Any` → both systems, `Fantasy` → Realms of Terrinoth only) and carries a structural `category` tag.

ROT-TAL-01 makes the catalogue authoritative for talent metadata, not just for names: the seed also syncs `Tier`, `IsRanked`, `Activation`/`ActivationEn`, `CanUseOutOfTurn`, `Retired` and `CareerSkillNames` onto existing rows. The active RoT scope is exactly 112 talents. A catalogue entry may carry `retiredIn: ["RealmsOfTerrinoth"]`, which retires only the RoT row and leaves the Genesys Core one active — an entry wrongly attributed to the RoT PC catalogue is never deleted globally, and historical ownership keeps working because the row survives.

`ActivationEn` holds the stable English timing and `CanUseOutOfTurn` marks `Out-of-turn Incidental` as its own timing rather than a plain Incidental. `Shapeshifter (Improved)` is out-of-turn only through its own trigger, so it carries `Triggered Incidental`. `CareerSkillNames` (ROT-TAL-04) lists the skills a talent turns into career skills while owned; `CareerSkillResolver` unions them with career and species grants.

### ItemDefs

Built-in and custom item definitions.

Fields include `System`, content-model fields, `Kind`, `Encumbrance`, `SoakBonus`, `MeleeDefense`, `RangedDefense`, `EncumbranceThresholdBonus`, nullable `Price`/`Rarity`, `Purchasable`, `Sellable`, `OwnerUserId`, `HomebrewPackId`.

The 17 built-in Realms of Terrinoth runebound shards have no ordinary listed economy:
`Price = null`, `Rarity = null`, `Purchasable = false`, `Sellable = false`. Their typed
implement and activation profiles are defined by `RuneboundShardRules`, keyed by the
suffix of `ItemDef.Code`.

Indexes include non-unique `HomebrewPackId` and `(System, OwnerUserId)` for built-in/custom reference visibility lookups.

### HeroicAbilityDefs

Built-in and custom heroic abilities.

Fields include content-model fields, the activation card (`Requirement`, `ActivationCost`, `Activation`,
`Duration`, `Frequency`, `Notes`), `OwnerUserId`, `HomebrewPackId` (no `System` — heroic abilities are Realms of Terrinoth only).

Indexes include non-unique `HomebrewPackId` and `OwnerUserId`.

### HeroicAbilityUpgradeDefs

Purchasable Power upgrades (Improved/Supreme) of a heroic ability. Fields: `Id`, `HeroicAbilityDefId` (FK,
cascade delete), `Level`, `Cost`, `Description`, `Notes`. The character's `HeroicUpgradeRank` (0/1/2)
records the highest purchased level.

### HeroicSecondaryEffectDefs

Standard universal secondary effects for RoT heroic abilities. Fields: content-model fields (`Code`,
localized names, private/safe descriptions, source). `Code` is unique. PrivateFull descriptions are
extended original paraphrases; PublicSafe clears `Description` and retains `SafeDescription`.

### ArchetypeDefs

Built-in archetypes/species. Loaded from the embedded catalog `Persistence/SeedContent/archetypes.catalog.json` (see `ArchetypeCatalog`), generated by `_books/gen-archetypes-catalog.mjs` from `genesys_rot_core_archetypes_ru.csv` (setting `Any` → Genesys Core, `Fantasy` → Realms of Terrinoth). Seeded via upsert by `Code` (`SeedData.SeedOrUpdateArchetypes`): existing rows are synced to the catalog, and built-in species no longer present in the catalog are marked `Retired`.

Fields include `System`, content-model fields, six characteristics, wound/strain bases, starting XP, `OwnerUserId`, `HomebrewPackId`, and `Retired`.

Indexes include non-unique `OwnerUserId`, `HomebrewPackId`, and `(System, OwnerUserId)`.

`Retired` archetypes stay in the table (existing characters reference them by FK) but are excluded from the reference endpoint, so they are not offered when creating a character.

Two child collections carry the structured species data parsed from the catalog (U-12), replaced wholesale on upsert when they drift from the catalog:

### ArchetypeAbilityDefs

Structured species abilities (formerly free text in `SafeDescription`). Fields: `ArchetypeId`, `Code`, `NameRu`, `NameEn`, `SafeDescription`, `AutomationKind` (UI classification), and the executable rule metadata added by ROT-SPECIES-01: `RuleKind`, `RuleValue`, `RuleParameters`, `UsesPerScope`, `UseScope`, `StoryPointCost`. Cascade delete from archetype; indexed by `ArchetypeId`.

`RuleKind` is a `SpeciesAbilityRuleKind` and is the only source of an ability's mechanics — deriving the effect from `Code`, a name or the description text is forbidden. `RuleValue` carries the single number a rule needs (Dark Vision removes 2, Battle Rage adds 2 damage, Nimble sets Defence to 1, Small sets silhouette to 0) and `RuleParameters` the named qualifiers (`source=darkness`, `enc=1;rarity=4;requireQuality=Limited Ammo 1`, `options=…` for a choice ability). `UseScope` (`None`/`Encounter`/`Session`) says where `UsesPerScope` resets.

`ArchetypeDefs.Silhouette` is 1 for every RoT species and 0 for both gnomes; the `Small` ability overrides it through the same typed rule rather than a special case. `Characters.SpeciesAbilityChoiceCode` stores the mandatory, irreversible Half-Catfolk pick (Claws or Fleet of Paw); until it is set, the sheet reports `SpeciesChoiceIncomplete` and that ability contributes nothing — the server never picks for the player.

### ArchetypeStartingSkills

Species starting skills applied at character creation. Fields: `ArchetypeId`, `SkillName` (English canonical, matches `SkillDef.Name`), `NameRu`, `FreeRanks`, `IsChoice`, `ChoiceGroup`, `ChoiceCount`, `GrantsCareerSkill`. Fixed entries (`IsChoice = false`) are auto-applied as free ranks; choice entries (e.g. `any-noncareer`, pick N) are resolved by the creation picker. Cascade delete from archetype; indexed by `ArchetypeId`.

`GrantsCareerSkill` (ROT-CRE-01) marks a grant that additionally makes the skill a career skill. In the built-in RoT catalog exactly two rows carry it: Deep Elf `Knowledge (Forbidden)` and Highborn Elf `Divine`. The effective career-skill set is resolved by `CareerSkillResolver` as career ∪ species grants ∪ talent grants, deduplicated by `SkillDefId`; the stored `CharacterSkills.IsCareer` flag is a cache, not the source of truth.

### CareerDefs

Built-in careers.

Fields include `System`, content-model fields, `OwnerUserId`, `HomebrewPackId`, `CareerSkillNames`, and starting money (`StartingMoneyFixed`, `StartingMoneyDice` like `1d100`). Money/gear/rules are distributed onto built-in careers from the embedded `career-extras.catalog.json` (`CareerExtrasCatalog`, generated by `_books/gen-career-extras-catalog.mjs`) via the idempotent `SeedData.SeedCareerExtras` — only the 9 RoT careers with starting gear get extras (Core careers have none).

Indexes include non-unique `OwnerUserId`, `HomebrewPackId`, and `(System, OwnerUserId)`.

### CareerStartingGear

Career starting equipment granted at character creation (U-13). Fields: `CareerId`, `ItemCode` (bare slug; matches the suffix of `ItemDef.Code` = `{sys}.item.{ItemCode}`), `ItemNameFallback` (RU label), `Quantity`, `IsChoice`, `ChoiceGroup`, `ChoiceOption`. Fixed rows are auto-added to inventory; choice slots group rows by `ChoiceGroup`, and one selectable bundle = all rows sharing a `ChoiceOption`. Cascade delete from career; indexed by `CareerId`.

These rows are only granted in `CareerPackage` mode, and only as a whole: `CareerPackageResolver`
requires exactly one valid option per group, rejects missing/unknown/duplicated groups with a machine
`reasonCode`, and merges repeated item codes into one line (ROT-CRE-03). Two catalog corrections live
here: Scout's first group is exactly `Bow` **or** `Light Spear` with `Leather Armor` as a separate
fixed row, so neither branch yields two suits of armour (ROT-CRE-04); and `Traveling Gear` is stored
as its six real items — Backpack, Bedroll, Rope, Flint and Steel, Torches (3), empty Waterskin —
rather than the invented `Adventuring Pack` bundle, which is now `Retired` (ROT-CLEAN-3.7).

### CareerRule

Structured career rules/notes (U-13). Fields: `CareerId`, `Code`, `Kind` (`Advisory`/`SkillSubstitution`), `Description`. Cascade delete from career; indexed by `CareerId`.

### Characters

Owned character sheets.

Fields include owner, name, system, archetype, career, six characteristics, total/spent XP, creation phase,
current wounds/strain, optional heroic ability, `HeroicUpgradeRank` (Power), `HeroicDurationRanks`,
`HeroicFrequencyRanks`, `HeroicStoryUpgrade`, and created timestamp.

Threshold snapshot (ROT-CRE-02): `CreationWoundThreshold` and `CreationStrainThreshold` are nullable
and written once, inside the `CompleteCreation` transaction, as species base + the characteristic at
that moment. While they are `null` (creation phase) thresholds are computed dynamically. Once set,
later Brawn/Willpower changes — including `Dedication` — no longer move them; only explicit threshold
effects (`Toughened` +2 WT/rank, `Grit` +1 ST/rank) are added on top, exactly once, by
`CharacterDerived.Compute`, which is the single calculator used by the sheet, list, print, duplicate,
campaign and Game Table. `ThresholdSnapshotProvenance` records where the values came from
(`None`, `CreationCompleted`, `LegacyAuditReconstructed`, `LegacyDerivedFromVisibleTotal`,
`LegacyEstimated`) and `RulesReviewRequired` flags a character whose values were estimated and need a
human check. Import of a pre-v2 export file computes the thresholds deterministically, marks them
`LegacyEstimated` and returns a warning — it never stores a zero or a silent guess.

Heroic identity (ROT-HA-01): a heroic ability has three separate notions — the catalogue primary
effect, the player's own name for it, and its origin. `HeroicCustomName` (1–120 chars) stores the
personal name and never falls back to the effect's display name. `HeroicOriginMode`
(`Standard` / `DoubleStandard` / `Custom`) is stored explicitly so a lost second category cannot be
mistaken for a single-category roll; `HeroicOriginPrimary` / `HeroicOriginSecondary` hold the d10
table categories (enum value = printed face 1–9) and `HeroicOriginNarrative` (≤ 2000 chars) the
player's own text, mandatory for `Custom`. `HeroicOriginRolls` keeps the actual faces as a
comma-separated list (`0,0,4,7`), where `0` is the special "roll twice more" result — it is recorded
for audit and never stored as a final origin. The roll itself is server-side, through the injected
`IDiceRoller`, and is logged as `HeroicOriginRolled`; setting the identity is logged as
`HeroicIdentitySet`. All columns are nullable so pre-ROT-HA-01 characters still load: such a
character reports `HeroicIdentityIncomplete`, stays playable, but cannot buy or edit heroic upgrades
until the owner fills the data in once. New RoT characters cannot complete creation without it, and
after completion the identity is immutable. Duplicate, export v2 and import carry it over; an import
whose identity fields are inconsistent (for example `Custom` together with a table category) drops
the identity with a warning instead of inventing an origin.

Heroic parameters (ROT-HA-02): three primary effects take a parameter chosen together with the
effect. `CharacterHeroicConfigurations` is a one-row-per-character table holding the Paragon skill
(`ParagonSkillDefId` plus a `ParagonSkillName` snapshot, so a later-hidden custom skill produces a
repair warning instead of a silent substitution) and the Sixth Sense subject (≤ 300 chars, a typed
parameter rather than a free character note). `CharacterSignatureWeapons` holds the named weapon —
also one row per character, so a lost weapon and its replacement can never both be active. Only the
choice is stored (`Profile`, `Craftsmanship`, `NarrativeForm`, `FormTraits`, `BaseAttachmentDefId`,
`Improvement`, `SupremeAttachmentDefId`, `IsLost`); the numbers
— skill, damage, crit, range, encumbrance, hard points and qualities — are rebuilt by the server
from `SignatureWeaponProfiles`, so a tampered client cannot invent a weapon. `FormTraits` is the
GM-confirmed flag set that attachment compatibility is resolved against; the profile group flag is
always set server-side, and physically impossible combinations (bladed + blunt, a ranged sword, a
one-handed bow) are rejected. Changing the primary effect during creation deletes both rows in the
same transaction. Completion requires the parameter, after completion it is immutable, and a legacy
character without one reports `HeroicConfigurationIncomplete` and cannot buy heroic upgrades until
the owner picks it once. Export v2 carries the Paragon skill by code and name — never by id, which
does not exist in another account.

`BaseAttachmentDefId` (ROT-HA-02) is the transient base attachment chosen together with the form: a
reference to `AttachmentDefs` rather than a `CharacterAttachments` instance, because it costs nothing,
uses no hard points and is only active together with the heroic ability. Compatibility is resolved by
the same predicates as a normal install (`AttachmentRules.IsCompatible` against `FormTraits`), and an
attachment that would grant a quality the profile already carries is rejected; rarity, price, hard
points, the enchantment install check and the magic-skill rank do not apply to the heroic copy. The
sheet folds its effects into the weapon's damage, crit and qualities. A weapon without one counts as
an incomplete parameter, so characters created before this column pick it once through the same legacy
path. Export v7 carries it by code, and import re-checks compatibility instead of trusting the file.

`Improvement` and `SupremeAttachmentDefId` (ROT-HA-05) hold what the ability's upgrades granted.
`Craftsmanship` is limited at creation to what the ability itself offers — Steel, Dwarven or Elven;
Ancient is the Improved reward and Iron is not on offer at all, so `Improvement` = `Ancient` is what
makes the weapon ancient, replacing the created craftsmanship in every calculation without
overwriting the stored choice (`EffectiveCraftsmanship`). `Improvement` = `Reinforced` is the other
half of that either/or. Supreme adds two hard points and one permanently installed free attachment
of rarity 9 or less that must fit them; unlike the base attachment it does consume hard points.
Both choices are fixed at purchase, and while an upgrade is bought but unchosen the character
reports `SignatureWeaponUpgradeIncomplete` and cannot buy further heroic upgrades.

Starting equipment (ROT-CRE-03): `StartingEquipmentMode` (`StandardMoney` / `CareerPackage`) records
the mutually exclusive mode chosen at creation, and `StartingPurchaseBudget` holds what is left of it.
The modes never mix. `StandardMoney` gives a 500-silver purchase budget plus separate `1d100` pocket
money in `Money`; the budget and the pocket money are deliberately two accounts, so 500 and the roll
are never summed into one balance. `CareerPackage` gives the whole career package and the career's own
money formula instead, with no budget. During the creation phase a purchase draws on the budget first
and the wallet second, and a sale restores the budget before the wallet — otherwise buy-then-sell
would launder the budget into spendable cash. The money roll goes through the injected `IDiceRoller`,
and both the formula and the rolled result are written to the audit log as `CharacterCreated`.

`CharacterItems.Provenance` (`Purchased`, `CareerPackage`, `StartingBudget`, `Imported`) keeps
duplicate, audit and legacy repair from treating granted starting gear as an ordinary purchase.

Relationships:

- `Archetype` restrict delete.
- `Career` restrict delete.
- `HeroicAbility` set null on delete.
- skills/talents/items/secondary heroic effects cascade delete from character.

`CharacterHeroicSecondaryEffects` is the normalized selection table for up to two different standard
secondary effects. It has a unique `(CharacterId, HeroicSecondaryEffectDefId)` index; the two-effect
limit and point budget are application/domain rules.

### CharacterShareTokens

Opaque tokens for public read-only character-sheet sharing (U-24). The raw token is returned only once by
`POST /api/characters/{id}/share`; the database stores only its SHA-256 hex hash.

Fields: `Id`, `CharacterId`, `TokenHash`, `CreatedAt`, `RevokedAt`.

Indexes:

- unique `TokenHash`.
- non-unique `(CharacterId, RevokedAt)` for revoking active links.
- non-unique `(CharacterId, CreatedAt)` for token listing/audit ordering.

Relationships:

- FK to `Characters` with cascade delete.

### CharacterSkills

Skill state for a character.

Fields: `CharacterId`, `SkillDefId`, `Ranks`, `IsCareer`, `FreeRanks`.

Indexes:

- unique `(CharacterId, SkillDefId)`.

### CharacterTalentChoices

Player choices saved per talent rank (ROT-TAL-03). Fields: `CharacterTalentId`, `RankIndex`, `Kind` (`Characteristic`/`Skill`/`SpellConfiguration`/`AnimalCompanion`), `Value`, `DisplayName`. Cascade delete from the talent; indexed by `CharacterTalentId`.

`Value` is the stable identifier (a `CharacteristicType` name, a canonical skill name, a serialized spell configuration or a companion id) and `DisplayName` is only a snapshot for the sheet — renaming reference content must not change what was chosen. `TalentChoiceSchemas` defines cardinality per rank, cross-rank distinctness and allowed skill kinds, and validates everything before XP is spent. Refunding a rank removes that rank's choices in the same transaction. `CharacterTalents.NeedsChoice` marks a legacy talent whose required choice is missing: its effect is blocked until a human fixes it, without paying XP again.

### CharacterTalents

Talent state for a character.

Fields: `CharacterId`, `TalentDefId`, `Ranks`.

Indexes:

- unique `(CharacterId, TalentDefId)`.

### CharacterItems

Inventory item instances.

Fields: `CharacterId`, `ItemDefId`, `Quantity`, `State`, implement material/configuration,
damage/craftsmanship state, the Lesser Rune fields `ShardActivationChoice`, `ShardEffectAction`,
`ShardEffectChoice`, `ShardConfigured`, and the transport link `CarriedByMountId` /
`IsInstalledOnMount` (ROT-TRANSPORT-01).

Runebound shards are non-stackable instances (`Quantity = 1`). Lesser Rune configuration
belongs to the instance and is immutable after it is first saved.

`CarriedByMountId` (nullable, `SetNull`) means the row sits on a transport rather than on the
character: such rows are excluded from the owner's encumbrance and from their equipped gear, which
is why barding on a mount never protects the rider. `IsInstalledOnMount` separates installed gear
(barding, saddlebags — changes the transport's capacity and protection) from ordinary cargo
(occupies capacity). Deleting a transport does not delete its cargo: the link is nulled and the
items return to the owner.

### MountDefs / MountSkills / MountAbilities / MountAttacks

Purchasable transport profiles — mounts and vehicles (ROT-MOUNT-ITEM-01, ROT-TRANSPORT-01). Neither
is a piece of gear: both have a statblock, a damage threshold and a cargo capacity, so they are their
own content type rather than rows in `ItemDefs`. `TransportKind` (`Mount`/`Vehicle`) separates the
two; a vehicle has no characteristics, skills or attacks, and its `StrainThreshold` reads as a system
threshold. `MovementMode` (`Ground`/`Flight`/`Wheeled`) and `RequiresTraction` describe how it moves —
the book gives these profiles no numeric speed, so the card shows the mode instead.

`MountDefs` fields: `System`, `Code` (`rot.mount.<bare>`), `Name`, `NameRu`, `Kind` (`NpcKind` —
Minion or Rival), the six characteristics, `Soak`, `WoundThreshold`, `StrainThreshold` (null for
Minion/Rival — they have none), `MeleeDefense`, `RangedDefense`, `Silhouette`, `Capacity`, `Price`
(null = priceless), `Rarity`, `IncludedGear` (primitive collection of gear codes),
`RequiresRidingCheck`, the content-model description fields, `OwnerUserId`, `HomebrewPackId`,
`Retired`.

`Capacity` is the book number and wins over the generic `5 + Brawn` rule (`MountRules.Capacity`);
a custom profile with no capacity falls back to the generic rule.

Child tables cascade from the profile: `MountSkills` (`Name`, `Ranks`, `IsGroupSkill` — Minion group
skills carry rank 0), `MountAbilities` (`Name`, `NameRu`, descriptions), `MountAttacks` (`SkillName`,
`Damage`, `Critical`, `Range`, `QualityCodes` as a primitive collection — no book profile has a rated
quality).

### CharacterMounts

Transport instances owned by a character — mounts and vehicles. Fields: `CharacterId`, `MountDefId`,
`Name` (nickname; empty means the profile name is shown), `Provenance`, `WoundsCurrent`, `IsActive`,
`Notes`, `DrawnByMountId`, `CreatedAt`.

Cascades from `Characters`; the profile link is `Restrict`, so a referenced profile cannot be
deleted out from under an instance. `DrawnByMountId` is a self-reference to the draft animal pulling
a vehicle (`SetNull`): selling the animal leaves the wagon in place without a dangling link, and a
wagon with no traction simply does not move — it is not deleted and its cargo does not move to the
owner. Transport has no encumbrance of its own and never contributes to the owner's carried weight;
its cargo lives in `CharacterItems.CarriedByMountId`.

`CarriedLoad` was dropped in `RotTransport01TransportSection`. It held an undescribed load number
with no items behind it, and there was nothing to migrate it into — the owner accepted the loss
explicitly. Load is now computed from the cargo rows (`MountRules.CargoLoad`), and capacity is the
profile number plus any installed saddlebags.

### SpellDefs

Built-in and custom magic reference entries (spell effects and additional-effect modifiers).

Fields: `Id`, `System`, `MagicSkill` (Arcana/Divine/Primal, plus Runes/Verse for Terrinoth; empty for additional effects), `Kind` (`Effect`/`AdditionalEffect`), `ParentEffect` (for additional effects — the `NameEn` code of the base effect they modify; empty for base effects), `NameRu`, `NameEn`, `Difficulty`, `Description` (full/private paraphrase), `SafeDescription` (copyright-safe public text), `Source` (book/section reference), `SortOrder`, `OwnerUserId`.

Structural rule fields (ROT-MAG-01), all fed from the domain matrix `MagicMatrix` rather than parsed out of descriptions:

- `AllowedSkills` — comma-separated magic skills the entry is available to, already narrowed to the skills of its system. For a base effect it is the matrix row of the action; for an additional effect it is the parent action's row narrowed by the effect's own restriction.
- `RestrictedSkill` — the single skill an additional effect is limited to (empty when unrestricted); the holy icon discount is computed from it.
- `DifficultyIncrease` — the number behind the printed `Difficulty` string.
- `Repeatable` — the effect may be added to one spell several times (keyed by action + effect, so a same-named effect of another action does not inherit it).
- `Exclusions` — comma-separated effect codes that cannot be combined with this one.
- `Resolution` — how the entry applies: `OnSuccess`, `PassiveQuality`, `ActivatedQuality`, `AdvantageSpend`, `StoryPoint`, `Narrative`, `Parameter`.
- `IsOptional` — the entry comes from the optional Expanded Player's Guide.

Base effects are seeded once per (system, magic skill) only for skills where the effect is available (availability matrix); additional effects are seeded once per (system, base effect) and are skill-agnostic.

The seed is authoritative for built-in entries: descriptions and the structural fields are synced into already-seeded databases, and a built-in row that is no longer in the catalog is deleted (custom entries with `OwnerUserId` are never touched).

Indexes:

- non-unique `(System, MagicSkill, Kind)`.
- non-unique `(System, OwnerUserId)` for built-in/custom spell visibility lookups.

### QualityDefs

Reference catalog of item/spell qualities (U-10, GF-005). System-agnostic — one definition per quality, referenced by items of both systems. Seeded from the embedded `SeedContent/qualities.catalog.json` (generated from `_books/_qualities/genesys_rot_item_and_spell_qualities.csv`).

Fields: `Id`, `Code` (unique slug of `NameEn`), `NameEn`, `NameRu`, `Kind` (`QualityKind`: ItemQuality/SpellAdditionalEffect — currently all ItemQuality), `IsActive` (active vs passive), `HasRating`, `ActivationCost`, `Category`, `Description` (full/private), `SafeDescription`, `Source`. Dual-mode content like other defs (PublicSafe clears `Description`).

`ActivationCost` is `varchar(400)` because some structured activation descriptions exceed 160 characters.

Indexes: unique `Code`.

### ItemQualityValues

Structural link between an item and a quality with an optional rating (U-10). Back-filled from `ItemDef.Properties` strings of built-in items by `SeedData.BackfillItemQualities` (idempotent; the `Properties` string is kept as fallback). Custom items currently carry qualities via the `Properties` string only.

Fields: `Id`, `ItemDefId`, `QualityDefId`, `Rating` (nullable).

Indexes: non-unique `ItemDefId`, `QualityDefId`. Cascade FKs to `ItemDefs` and `QualityDefs`.

### Npcs

User-owned, campaign-linked, or built-in adversaries.

Fields include owner/campaign visibility, `System`, `Kind`, `Role`, characteristics, derived combat stats, `Silhouette`, `Tactics`, free-text talent/equipment arrays and `Tags` (`text[]` in PostgreSQL).

`Code` is the stable seed key of a built-in adversary (empty for user NPCs); it — not the display name — identifies the row, so an official profile can be renamed without creating a duplicate (ROT-BEST-01). `Retired` marks a built-in that left the active bestiary of its system (the nine Haunted City profiles, ROT-CLEAN-3.6): it is filtered out of the library list, but stays reachable by id for existing encounters and duplicates. Duplicating a built-in produces user content with an empty `Code` and `Retired = false`.

Indexes:

- non-unique `OwnerUserId`, `CampaignId`, `IsBuiltIn`, `Code`.
- non-unique `(System, Kind, Role)` for adversary library filters.
- non-unique `(System, OwnerUserId)` and `(System, Visibility)` for scoped visible-library queries.
- PostgreSQL GIN index `IX_Npcs_Tags_Gin` on `Tags` for `Tags.Contains(tag)` filters.

### CraftingSpendDefs / CraftingProjects / CraftingProjectSpends

Crafting, brewing and enchanting (ROT-CRAFT-01, ROT-ALCH-02, ROT-CRAFT-MAGIC-01).

`CraftingSpendDefs` holds both symbol-spend tables as content rows (`IContentDef`, so the usual
PublicSafe projection and code-based seed sync apply). One row per *effect*, not per table row:
a book row offers several mutually exclusive effects for one price, and `RowCode` groups them.
`Effect = Descriptive` marks a spend the application does not execute — round counters, dose
durations, «a boost die on the next check» and GM decisions. Unique `Code`; index on
`(Table, SortOrder)`.

`CraftingProjects` records what a project was made of: target snapshot (name, price, rarity), skill,
base and effective difficulty/time with their override reasons, the component cost with its
percent/override mode, free-text `Requirements`, the roll symbols the client reported, the chosen
spends and the resulting `CharacterItemId`. `BaseCharacterItemId` is the enchanted instance —
enchanting upgrades an existing item rather than creating one. Indexes on `CharacterId` and
`(CharacterId, Status)`.

### NpcAttacks / NpcAttackQualities

Structural combat attacks for NPCs (U-14, GF-008 / Audit §5), replacing combat strings previously embedded in `Npc.Equipment` (non-combat gear stays in `Equipment`).

`NpcAttacks` fields: `Id`, `NpcId`, `Name`, `SkillName` (English roll skill, e.g. `Melee (Heavy)`), `Damage` (`+N` melee bonus or absolute), `Critical`, `RangeBand` (Russian label), `Notes`, `SourceWeapon`. Cascade FK to `Npcs`; non-unique `NpcId` index. `SourceWeapon` is the equipment label a weapon-derived attack was generated from (empty = manual/custom); the NPC editor keeps such attacks in sync with weapons in `Equipment` while preserving custom ones.

`NpcAttackQualities` fields: `Id`, `NpcAttackId`, `QualityDefId` (nullable — null for custom), `QualityCode`, `NameRu`, `Rating` (nullable). Reuses the U-10 `QualityDefs` catalog: codes are resolved to `QualityDefId` and canonical `NameRu` on save (`NpcMapper.ResolveAttackQualitiesAsync`); unmatched codes stay custom. Cascade FK to `NpcAttacks`; `SetNull` FK to `QualityDefs` (deleting a catalog quality keeps the attack with denormalized fields). Non-unique `NpcAttackId`, `QualityDefId` indexes.

Back-filled from `Npc.Equipment` combat strings by `SeedData.BackfillNpcAttacks` (idempotent; NPCs that already have attacks are skipped). Only lines with a damage/crit marker are parsed into attacks (`NpcEquipmentParser`); unparsed lines stay in `Equipment`. The QuickDraft generator emits structured attacks directly from catalog weapons.

### RuleTableEntries

Reference rule tables (U-11, GF-005 / Audit §8): difficulty ladder, Advantage/Threat/Triumph/Despair symbol spends, range bands, and the d100 critical-injury table. System-agnostic — one definition per rule, used by both systems. Seeded from the embedded `SeedContent/rules.catalog.json` (generated by `_books/gen-rules-catalog.mjs` from the `genesys_*` CSVs; RU paraphrases of mechanics, not book text — public-safe by construction, so no PublicSafe clearing).

Fields: `Id`, `Kind` (`RuleTableKind` enum: Difficulty/SymbolSpend/RangeBand/CriticalInjury), `Code` (stable slug), `NameRu`, `NameEn`, `GroupRu` (severity/situation), `SortOrder`, `RollRange` (d100 band for crits), `SymbolCost`, `Body` (effect paraphrase), `Notes`, `Source`, `SourcePage`, `SearchText` (denormalized lowercase for search).

Indexes: unique `Code`, non-unique `(Kind, SortOrder)`. Dedup on seed is by `Code`.

### CharacterAuditEntries

Per-character history / audit log (U-09). A row is written in the same transaction as the operation it records (buy/refund of characteristics/skills/talents, item add/sell/remove, creation completed, manual XP edit, XP award), so it reflects the post-operation state.

Fields: `Id`, `CharacterId`, `UserId`, `CreatedAt`, `Action` (`CharacterAuditAction` enum), `Summary` (human-readable), `XpDelta` (nullable — change in *available* XP; negative for purchases, positive for refunds/awards; null for non-XP actions), `TotalXpAfter`, `SpentXpAfter`, `DataJson` (structured detail).

Indexes:

- non-unique `(CharacterId, CreatedAt)`.
- FK to `Characters` with cascade delete.

### HomebrewPacks

User-owned portable homebrew JSON packs (U-26), separate from campaign handbook `ContentPacks`.

Fields: `Id`, `OwnerUserId`, `Name`, `Description`, `System`, nullable `ShareTokenHash`, `IsShared`,
`IsEnabledByDefault`, `CreatedAt`, `UpdatedAt`.

Indexes:

- non-unique `OwnerUserId`.
- unique nullable `ShareTokenHash` for shared import tokens.

Imported pack content is stored in the normal custom reference tables through nullable `HomebrewPackId`
columns on `SkillDefs`, `TalentDefs`, `ItemDefs`, `HeroicAbilityDefs`, `ArchetypeDefs`, `CareerDefs`.
Reference visibility includes pack content only when the pack is enabled by default or enabled through
the character/campaign toggle tables.

### HomebrewPackCharacters / HomebrewPackCampaigns

Per-character and per-campaign pack toggles.

Fields:

- `HomebrewPackCharacters`: `Id`, `HomebrewPackId`, `CharacterId`, `IsEnabled`, `UpdatedAt`.
- `HomebrewPackCampaigns`: `Id`, `HomebrewPackId`, `CampaignId`, `IsEnabled`, `UpdatedAt`.

Indexes:

- unique `(HomebrewPackId, CharacterId)`.
- unique `(HomebrewPackId, CampaignId)`.
- cascade FKs to the pack and target character/campaign.

### RollLogEntries

Game Table dice-roll log (U-08). The roll outcome is computed on the client (Genesys narrative dice); the row stores it for history and realtime display to other table participants.

Fields: `Id`, `CampaignId`, `SessionId` (nullable — the active scene at roll time, if any), `ActorUserId`, `ActorName` (display name snapshot), `Label` (what was rolled, optional), `PoolJson` (dice pool snapshot), `ResultJson` (net symbols snapshot), `Summary` (short human-readable result), `IsSecret` (GM-only roll; honored only for the GM), `CreatedAt`.

Indexes:

- non-unique `(CampaignId, CreatedAt)`.

## Migrations

Migration folder:

`backend/src/GenesysForge.Infrastructure/Persistence/Migrations`

Found migrations:

- `20260612172325_InitialCreate`
- `20260613194614_AddCharacterNotes`
- `20260613195341_AddCampaigns`
- `20260614082314_AddSpells` — creates `SpellDefs` table with `(System, MagicSkill, Kind)` index.
- `20260614102018_AddSpellParentEffect` — adds `ParentEffect` column; clears built-in spell rows so the idempotent seed rebuilds them in the new structure (custom content untouched).
- `20260614105225_AddContentModel` — adds content-model columns (`Code`, `NameRu`, `Description`, `SafeDescription`, `Source`) to the six reference def tables. Non-destructive (only `AddColumn`, default `""`).
- `20260614143200_AddTalentSetting` — adds `Setting` (int flags) to `TalentDefs`. Non-destructive; default `1` (`Any`) so pre-existing talents stay visible. `CharacterTalents` reference talents via cascade, so the table is not recreated — correct per-talent settings come from a fresh seed.
- `20260625182741_AddRollLog` — creates `RollLogEntries` table (Game Table dice-roll log, U-08) with `(CampaignId, CreatedAt)` index. Non-destructive (only `CreateTable`).
- `20260625185307_AddCharacterAudit` — creates `CharacterAuditEntries` table (character XP/audit log, U-09) with `(CharacterId, CreatedAt)` index and cascade FK to `Characters`. Non-destructive (only `CreateTable`).
- `20260625193055_AddItemQualities` — creates `QualityDefs` (unique `Code`) and `ItemQualityValues` (cascade FKs to `ItemDefs`/`QualityDefs`) for structural item qualities (U-10). Non-destructive (only `CreateTable`); `ItemDef.Properties` retained.
- `20260625210000_ExpandQualityActivationCost` — expands `QualityDefs.ActivationCost` from 160 to 400 characters so startup seed accepts the full catalog.
- `20260626091551_AddRuleTables` — creates `RuleTableEntries` table (rule reference tables, U-11) with unique `Code` and `(Kind, SortOrder)` index. Non-destructive (only `CreateTable`).
- `20260626134851_AddArchetypeRetired` — adds `Retired` (bool, default false) to `ArchetypeDefs` so built-in species replaced by the detailed Terrinoth roster are hidden from selection while preserved for existing characters. Non-destructive (only `AddColumn`).
- `20260626152610_AddArchetypeAbilitiesAndStartingSkills` — creates `ArchetypeAbilityDefs` and `ArchetypeStartingSkills` tables (structured species abilities/starting skills, U-12) with cascade FKs to `ArchetypeDefs` and `ArchetypeId` indexes. Non-destructive (only `CreateTable`).
- `20260626211746_AddCareerStartingGearAndRules` — adds `StartingMoneyFixed`/`StartingMoneyDice` to `CareerDefs` and creates `CareerStartingGears` and `CareerRules` tables (career starting gear/rules, U-13) with cascade FKs to `CareerDefs` and `CareerId` indexes. Non-destructive (`AddColumn` + `CreateTable`).
- `20260626234831_AddNpcAttacks` — creates `NpcAttacks` (cascade FK to `Npcs`) and `NpcAttackQualities` (cascade FK to `NpcAttacks`, `SetNull` FK to `QualityDefs`) for structural NPC attacks (U-14). Non-destructive (only `CreateTable`); `Npc.Equipment` retained, combat strings back-filled into attacks on seed.
- `20260627084602_AddNpcSilhouetteAndTactics` — adds `Silhouette` (`int`, existing rows default `1`) and `Tactics` (`varchar(2000)`) to `Npcs` for adversary creation rules (U-15). Non-destructive (`AddColumn`).
- `20260627153450_AddNpcAttackSourceWeapon` — adds `SourceWeapon` (`varchar(160)`) to `NpcAttacks` to link weapon-derived attacks to inventory weapons (auto-create/sync). Non-destructive (`AddColumn`).
- `20260627224610_AddRuleEffectDefs` — creates `RuleEffectDefs` (cascade FK to `HeroicAbilityDefs`) for structural activation effects (U-18). Non-destructive (only `CreateTable`). New seeds carry effect markup; existing DBs get effects only on reseed of heroics.
- `20260629215609_AddCharacterShareTokens` — creates `CharacterShareTokens` for public read-only character sheet links (U-24), with unique `TokenHash`, `(CharacterId, RevokedAt)` index and cascade FK to `Characters`. Non-destructive (only `CreateTable`).
- `20260630115739_AddCustomArchetypeCareerOwnership` — adds nullable `OwnerUserId` plus indexes to `ArchetypeDefs` and `CareerDefs` so user-owned homebrew archetypes/careers can coexist with built-ins. Non-destructive (`AddColumn` + `CreateIndex`).
- `20260630123634_AddHomebrewPacks` — creates `HomebrewPacks`, `HomebrewPackCharacters`, `HomebrewPackCampaigns`, and adds nullable `HomebrewPackId` indexes to custom-capable reference tables for imported JSON packs. Non-destructive (`CreateTable` + nullable `AddColumn` + `CreateIndex`).
- `20260630182613_AddApiV1Indexes` — adds hot-path indexes for U-27: NPC filters (`System/Kind/Role`, scoped visibility, GIN `Tags`), reference content visibility (`System/OwnerUserId`), and token cleanup/lookups. Non-destructive (`CreateIndex` only).
- `20260701151637_AddTalentCategory` — adds `Category` (`int`, default `0` = `General`) to `TalentDefs` for UI filtering by common/social/combat/magic tags. Non-destructive (`AddColumn` only); built-in category values are provided by the next idempotent seed run from `talents.catalog.json`.
- `20260726114049_CompleteHeroicAbilityProgression` — adds Duration/Frequency/Story state to `Characters`,
  creates the standard secondary-effect catalog and character selection table, and corrects legacy
  `HeroicUpgradeRank` values that depended on the erroneous free starting point. The correction can
  lower an existing Power rank when the character lacks the required XP above species starting XP.
- `20260726115105_TrackHeroicAbilityUses` — adds `HeroicAbilityUses` to Game Table participants so
  once-per-session activation and repeatable Frequency upgrades are enforced for player characters.
- `20260726160924_RotCreationRulesFoundation` — ROT-CRE-01/ROT-CRE-02. Adds
  `ArchetypeStartingSkills.GrantsCareerSkill`, `TalentDefs.CareerSkillNames` (`text[]`), and
  `Characters.CreationWoundThreshold` / `CreationStrainThreshold` / `ThresholdSnapshotProvenance` /
  `RulesReviewRequired`. Non-destructive (only `AddColumn`) plus a one-time backfill of the two
  thresholds for already-completed characters. The backfill is exact rather than a guess: after
  creation the only thing that changes a characteristic is `Dedication`, whose picks are stored per
  rank in `CharacterTalents.GrantedCharacteristics`, so the value at completion is
  `current − dedicationPicks`; the resulting rows are marked `LegacyAuditReconstructed`. Characters
  still in the creation phase get no snapshot and keep computing thresholds dynamically. The
  backfill can lower a displayed threshold for a character who raised Brawn/Willpower after
  creation — that is the rule being fixed, not a regression.
- `20260726163445_RotStartingEquipmentModes` — ROT-CRE-03/04 and ROT-CLEAN-3.7. Adds
  `Characters.StartingEquipmentMode` / `StartingPurchaseBudget`, `CharacterItems.Provenance`, and a
  `Retired` flag to every remaining content table (`SkillDefs`, `TalentDefs`, `ItemDefs`,
  `CareerDefs`, `HeroicAbilityDefs`, `HeroicSecondaryEffectDefs`, `QualityDefs`; `ArchetypeDefs`
  already had one). Non-destructive (only `AddColumn`) and deliberately without a data backfill:
  existing characters already received money under the old rule, so granting them a 500 budget
  retroactively would invent funds, and starting gear cannot be told apart from purchases in
  historical inventories — ROT-CRE-04 explicitly forbids rewriting them.
- `20260726172948_RotSpeciesAbilityRules` — ROT-SPECIES-01. Adds `ArchetypeDefs.Silhouette`,
  typed rule metadata on `ArchetypeAbilityDefs` (`RuleKind`, `RuleValue`, `RuleParameters`,
  `UsesPerScope`, `UseScope`, `StoryPointCost`), and `Characters.SpeciesAbilityChoiceCode`.
  Non-destructive and without a backfill: the rule metadata arrives through the idempotent
  archetype seed, and the species choice is deliberately left empty for legacy Half-Catfolk
  characters — picking Claws or Fleet of Paw for a player is an irreversible decision, so the
  sheet reports `SpeciesChoiceIncomplete` and the ability simply stays unautomated until a human
  resolves it.
- `20260726180958_RotTalentCatalogMetadata` — ROT-TAL-01/ROT-TAL-04 (data half). Adds `TalentDefs.ActivationEn`,
  `CanUseOutOfTurn` and `CareerSkillNames`. Non-destructive (only `AddColumn`); the corrected
  catalogue itself is applied by the idempotent talent seed, which now treats the catalogue as
  authoritative for tier, ranked, activation timing, out-of-turn and `Retired` as well as names and
  descriptions. No talent row is deleted — characters reference them.
- `20260728211444_RotMagicEffectAvailability` — ROT-MAG-01. Adds `SpellDefs.AllowedSkills`,
  `DifficultyIncrease`, `Exclusions`, `Resolution` and `IsOptional`. Non-destructive (only
  `AddColumn`); values arrive with the next idempotent spell seed, which is now authoritative for
  built-in magic entries. That seed also removes the built-in `Attack/Move` row (a duplicate of
  `Manipulative`) and migrates implement configurations that referenced it.
- `20260729070926_RotMagicHasteSwiftSwap` — ROT-MAG-04. No schema change: a one-time data fix that
  swaps the `Haste` and `Swift` codes stored in `CharacterItems.ImplementChoices`. The catalog had
  the two Augment effects under each other's codes, and the GM picked the free effect by name and
  description, so the stored code has to follow the mechanic. The swap lives in the migration
  rather than the idempotent seed, which would flip the values on every run; `Down` is the same
  swap.
- `20260729074616_RotMagicKnowledgeRating` — ROT-MAG-10. Adds `SpellDefs.UsesKnowledgeRating` and
  `RatedQualities`. Non-destructive (only `AddColumn`); values arrive with the next idempotent spell
  seed. The character sheet also gains a computed `knowledgeRating` block (available rating sources
  with ranks) — nothing is stored, it is derived from skills and talents on every read.
- `20260729095218_RotMag11RuneboundShards` — ROT-MAG-11. Makes `ItemDefs.Price` and `Rarity`
  nullable, adds `Purchasable`/`Sellable`, and stores immutable Lesser Rune choices on
  `CharacterItems`. Only the exact 17 built-in shard codes are changed to null/non-tradeable.
  Legacy stacked shard rows keep the original row id at quantity one and are split into individual
  cloned rows, so no owned shard is discarded.

- `20260730095352_RotMountItem01Mounts` — ROT-MOUNT-ITEM-01. Adds `MountDefs`, `MountSkills`,
  `MountAbilities`, `MountAttacks` and `CharacterMounts`. Purely additive — no existing table is
  touched. The four mount rows in `ItemDefs` become retired/non-purchasable through the idempotent
  item seed, and `SeedData.MigrateLegacyMountItems` converts already-owned mount gear rows into
  `CharacterMounts` (quantity N becomes N creatures) with a history entry per character; money is
  not recalculated and no row is silently deleted.
- `20260803153841_RotCraft01CraftingAndAlchemy` — ROT-CRAFT-01 / ROT-ALCH-02 / ROT-CRAFT-MAGIC-01.
  Purely additive: creates `CraftingSpendDefs` (both symbol-spend tables as content rows, unique
  `Code`), `CraftingProjects` (cascade FK to `Characters`, restrict FK to `ItemDefs`) and
  `CraftingProjectSpends` (cascade FK to its project), plus per-instance crafting columns on
  `CharacterItems`: `CraftingProjectId`, `CraftedEncumbrance`, `CraftedHardPoints`,
  `CraftedQualities`, `CraftedFragile`, `CraftNote`. No existing row is touched. Materials, tools
  and ingredients are description only — the project records a computed component cost but never
  charges money and never checks inventory, so there is nothing to reserve or consume.
- `20260803113917_RotBest01NpcCodeAndRetired` — ROT-CLEAN-3.6 / ROT-BEST-01. Adds `Npcs.Code`
  (`varchar(80)`, non-unique index; empty for user NPCs) and `Npcs.Retired` (`bool`, default
  `false`). Purely additive. `SeedBestiary` back-fills `Code` on already-seeded built-ins by the
  legacy `System+Name` key, renames `Goblin (Official)` to `Goblin` through an explicit legacy-name
  map (so the rename cannot create a second row), and syncs `Name`/`Source`/`Retired` by code. No
  row is deleted: the nine Haunted City adversaries stay reachable by id for existing encounters
  and duplicates.

Startup behavior:

- `InitializeDatabase()` calls `Database.Migrate()` for relational databases.
- The content mode is read from configuration (`Content:Mode`, default `PrivateFull`) into `ContentOptions`.
- Then `SeedData.Apply(db, mode)` is executed.

## Seed data and content modes

`SeedData.cs` inserts built-in skills, archetypes, careers, talents, items, heroic abilities, standard
heroic secondary effects and spell/magic reference entries with the full content model (`Code`, `NameRu`,
`SafeDescription`, `Source`). Talents (`talents.catalog.json` / `TalentCatalog`), items
(`items.catalog.json` / `ItemCatalog`), heroic abilities with their Power upgrades
(`heroics.catalog.json` / `HeroicCatalog`), universal heroic secondary effects
(`HeroicSecondaryEffectCatalog`), qualities (`qualities.catalog.json`), rule tables
(`rules.catalog.json`) and archetypes/species (`archetypes.catalog.json` / `ArchetypeCatalog`) are loaded
from embedded catalogs; the rest (skills, careers, spells) are defined in `SeedData.cs`.

Two seed pipelines are selected by `ContentMode` (param of `SeedData.Apply`, from `Content:Mode` config):

- `PrivateFull` — full content. `Description` is filled from the private description overlay (`PrivateContentStore`, see below); where no overlay exists it falls back to `SafeDescription` so it is never empty. Spell `Description` (a safe paraphrase baked in code) is kept.
- `PublicSafe` — copyright-safe. `Description` is cleared for every built-in entry (including spells); only `NameRu`, `SafeDescription` and `Source` remain. The public set is structurally complete (same `Code` set as private) so the public app is fully functional without any private data.

Common behavior (both modes):

- idempotent by built-in `(System, Name)` or heroic ability `Name`;
- ignores custom content where `OwnerUserId != null`;
- adds missing built-in entries without recreating the database.

Pipeline isolation: a database is seeded with a single mode; the two pipelines never mix in one run. Switching modes on an existing database only backfills missing rows (it does not rewrite existing descriptions) — re-seed a fresh database to change content mode.

### Private content overlay (`PrivateContentStore`)

Full private descriptions live in `backend/private-content/genesys-core.ru.json` and `backend/private-content/realms-of-terrinoth.ru.json` (map of stable `Code` → full description). The directory lives under `backend/` so it is inside the Docker build context (`./backend`) and the Dockerfile copies it before `dotnet publish` — otherwise `PrivateFull` in the image would be empty. They are own paraphrases, **not** official book text, and are embedded into the Infrastructure assembly as resources (`WithCulture=false` so the `.ru` suffix is not mistaken for a culture). `PrivateContentStore.Load()` reads them; in `PublicSafe` they are not used.

⚠️ Before opening the repository publicly, delete the `*.ru.json` files (keep the directory itself, otherwise the Dockerfile `COPY` fails) or move them to external private storage, then rebuild. The csproj glob tolerates the files being absent, and the public app runs in `ContentMode=PublicSafe`, which does not need them. See `backend/private-content/README.md`.

Legal risk:

- Seed descriptions (code and private-content files) must be kept as original/paraphrased content, not copied official text.

## Constraints currently configured in code

- `Users.Email` unique.
- Character references to archetype/career use restrict delete.
- Character reference to heroic ability uses set null.
- Character child collections cascade delete.
- Character share tokens cascade delete.
- Custom archetypes/careers use nullable `OwnerUserId`; deletion is blocked in application handlers when referenced by a character.
- Character skill `(CharacterId, SkillDefId)` unique.
- Character talent `(CharacterId, TalentDefId)` unique.

## Not found in current codebase

- Explicit database check constraints for XP ranges, tier ranges, ranks and quantity.
- Database-level ownership enforcement; ownership is application-level.
