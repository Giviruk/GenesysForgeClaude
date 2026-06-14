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
- `ArchetypeDefs`
- `CareerDefs`
- `Characters`
- `CharacterSkills`
- `CharacterTalents`
- `CharacterItems`
- `SpellDefs`

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

### Content model (shared reference fields)

All built-in reference entities (`SkillDefs`, `TalentDefs`, `ItemDefs`, `ArchetypeDefs`, `CareerDefs`, `HeroicAbilityDefs`) implement `IContentDef` and carry a shared content model:

- `Code` — stable key for built-in content (e.g. `gc.talent.parry`, `rot.item.plate-armor`); empty for custom content. Key for the private-content description overlay. `varchar(80)`.
- `Name` — original/English name (also the seed idempotency key).
- `NameRu` — Russian name. `varchar(160)`.
- `Description` — full (private) paraphrase; emitted only in `ContentMode.PrivateFull`, cleared in `PublicSafe`.
- `SafeDescription` — copyright-safe public text.
- `Source` — book/section reference, available in both modes. `varchar(160)`.

Visibility is governed by `OwnerUserId` (null = built-in, non-null = custom) plus the seed `ContentMode`. `SpellDefs` already carried `NameRu`/`Description`/`SafeDescription`/`Source` (see below).

### SkillDefs

Built-in and custom skill definitions.

Fields include `System`, content-model fields, `Characteristic`, `Kind`, `OwnerUserId`.

### TalentDefs

Built-in and custom talent definitions.

Fields include `System`, content-model fields, `Tier`, `IsRanked`, `Setting`, `Activation`, passive bonus fields and `OwnerUserId`.

`Setting` is a `[Flags] GenesysSetting` (`Any`, `Fantasy`, `Steampunk`, `WeirdWar`, `ModernDay`, `ScienceFiction`, `SpaceOpera`) controlling which game systems list the talent: Genesys Core shows only `Any`; Realms of Terrinoth shows `Any` + `Fantasy`. Custom talents (owned) are always visible to their owner regardless of setting.

Built-in talents are not hand-written in `SeedData`; they are loaded from the embedded catalog `Persistence/SeedContent/talents.catalog.json` (see `TalentCatalog`). The catalog is generated from the source CSVs (structure + reworked Russian descriptions, not book text). Each catalog entry is expanded into `TalentDef` rows per system by its setting (`Any` → both systems, `Fantasy` → Realms of Terrinoth only).

### ItemDefs

Built-in and custom item definitions.

Fields include `System`, content-model fields, `Kind`, `Encumbrance`, `SoakBonus`, `MeleeDefense`, `RangedDefense`, `EncumbranceThresholdBonus`, `Price`, `Rarity`, `OwnerUserId`.

### HeroicAbilityDefs

Built-in and custom heroic abilities.

Fields include content-model fields and `OwnerUserId` (no `System` — heroic abilities are Realms of Terrinoth only).

### ArchetypeDefs

Built-in archetypes/species.

Fields include `System`, content-model fields, six characteristics, wound/strain bases and starting XP.

### CareerDefs

Built-in careers.

Fields include `System`, content-model fields and `CareerSkillNames`.

### Characters

Owned character sheets.

Fields include owner, name, system, archetype, career, six characteristics, total/spent XP, creation phase, current wounds/strain, optional heroic ability and created timestamp.

Relationships:

- `Archetype` restrict delete.
- `Career` restrict delete.
- `HeroicAbility` set null on delete.
- skills/talents/items cascade delete from character.

### CharacterSkills

Skill state for a character.

Fields: `CharacterId`, `SkillDefId`, `Ranks`, `IsCareer`, `FreeRanks`.

Indexes:

- unique `(CharacterId, SkillDefId)`.

### CharacterTalents

Talent state for a character.

Fields: `CharacterId`, `TalentDefId`, `Ranks`.

Indexes:

- unique `(CharacterId, TalentDefId)`.

### CharacterItems

Inventory item instances.

Fields: `CharacterId`, `ItemDefId`, `Quantity`, `State`.

### SpellDefs

Built-in and custom magic reference entries (spell effects and additional-effect modifiers).

Fields: `Id`, `System`, `MagicSkill` (Arcana/Divine/Primal, plus Runes/Verse for Terrinoth; empty for additional effects), `Kind` (`Effect`/`AdditionalEffect`), `ParentEffect` (for additional effects — the `NameEn` code of the base effect they modify; empty for base effects), `NameRu`, `NameEn`, `Difficulty`, `Description` (full/private paraphrase), `SafeDescription` (copyright-safe public text), `Source` (book/section reference), `SortOrder`, `OwnerUserId`.

Base effects are seeded once per (system, magic skill) only for skills where the effect is available (availability matrix); additional effects are seeded once per (system, base effect) and are skill-agnostic.

Indexes:

- non-unique `(System, MagicSkill, Kind)`.

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

Startup behavior:

- `InitializeDatabase()` calls `Database.Migrate()` for relational databases.
- The content mode is read from configuration (`Content:Mode`, default `PrivateFull`) into `ContentOptions`.
- Then `SeedData.Apply(db, mode)` is executed.

## Seed data and content modes

`SeedData.cs` inserts built-in skills, archetypes, careers, talents, items, heroic abilities and spell/magic reference entries with the full content model (`Code`, `NameRu`, `SafeDescription`, `Source`). Talents are loaded from the embedded `talents.catalog.json` catalog (`TalentCatalog`), the rest are defined in `SeedData.cs`.

Two seed pipelines are selected by `ContentMode` (param of `SeedData.Apply`, from `Content:Mode` config):

- `PrivateFull` — full content. `Description` is filled from the private description overlay (`PrivateContentStore`, see below); where no overlay exists it falls back to `SafeDescription` so it is never empty. Spell `Description` (a safe paraphrase baked in code) is kept.
- `PublicSafe` — copyright-safe. `Description` is cleared for every built-in entry (including spells); only `NameRu`, `SafeDescription` and `Source` remain. The public set is structurally complete (same `Code` set as private) so the public app is fully functional without any private data.

Common behavior (both modes):

- idempotent by built-in `(System, Name)` or heroic ability `Name`;
- ignores custom content where `OwnerUserId != null`;
- adds missing built-in entries without recreating the database.

Pipeline isolation: a database is seeded with a single mode; the two pipelines never mix in one run. Switching modes on an existing database only backfills missing rows (it does not rewrite existing descriptions) — re-seed a fresh database to change content mode.

### Private content overlay (`PrivateContentStore`)

Full private descriptions live in `private-content/genesys-core.ru.json` and `private-content/realms-of-terrinoth.ru.json` (map of stable `Code` → full description). They are own paraphrases, **not** official book text, and are embedded into the Infrastructure assembly as resources (`WithCulture=false` so the `.ru` suffix is not mistaken for a culture). `PrivateContentStore.Load()` reads them; in `PublicSafe` they are not used.

⚠️ Before opening the repository publicly, delete `private-content/` (or move it to external private storage) and rebuild. The csproj glob tolerates the files being absent, and the public app runs in `ContentMode=PublicSafe`, which does not need them. See `private-content/README.md`.

Legal risk:

- Seed descriptions (code and private-content files) must be kept as original/paraphrased content, not copied official text.

## Constraints currently configured in code

- `Users.Email` unique.
- Character references to archetype/career use restrict delete.
- Character reference to heroic ability uses set null.
- Character child collections cascade delete.
- Character skill `(CharacterId, SkillDefId)` unique.
- Character talent `(CharacterId, TalentDefId)` unique.

## Not found in current codebase

- Explicit database check constraints for XP ranges, tier ranges, ranks and quantity.
- Explicit indexes for common owner/system filters beyond configured unique indexes.
- Database-level ownership enforcement; ownership is application-level.

