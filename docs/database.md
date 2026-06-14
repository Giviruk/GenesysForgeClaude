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

### SkillDefs

Built-in and custom skill definitions.

Fields include `System`, `Name`, `Characteristic`, `Kind`, `OwnerUserId`.

### TalentDefs

Built-in and custom talent definitions.

Fields include `System`, `Name`, `Tier`, `IsRanked`, `Description`, `Activation`, passive bonus fields and `OwnerUserId`.

### ItemDefs

Built-in and custom item definitions.

Fields include `System`, `Name`, `Kind`, `Encumbrance`, `SoakBonus`, `MeleeDefense`, `RangedDefense`, `EncumbranceThresholdBonus`, `Description`, `Price`, `Rarity`, `OwnerUserId`.

### HeroicAbilityDefs

Built-in and custom heroic abilities.

Fields include `Name`, `Description`, `OwnerUserId`.

### ArchetypeDefs

Built-in archetypes/species.

Fields include system, name, six characteristics, wound/strain bases, starting XP and description.

### CareerDefs

Built-in careers.

Fields include system, name, description and `CareerSkillNames`.

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

Fields: `Id`, `System`, `MagicSkill` (Arcana/Divine/Primal, plus Runes/Verse for Terrinoth), `Kind` (`Effect`/`AdditionalEffect`), `NameRu`, `NameEn`, `Difficulty`, `Description` (full/private paraphrase), `SafeDescription` (copyright-safe public text), `Source` (book/section reference), `SortOrder`, `OwnerUserId`.

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

Startup behavior:

- `InitializeDatabase()` calls `Database.Migrate()` for relational databases.
- Then `SeedData.Apply(db)` is executed.

## Seed data

`SeedData.cs` inserts built-in skills, archetypes, careers, talents, items, heroic abilities and spell/magic reference entries.

Current seed behavior:

- idempotent by built-in `(System, Name)` or heroic ability `Name`;
- ignores custom content where `OwnerUserId != null`;
- adds missing built-in entries without recreating the database.

Legal risk:

- Seed descriptions must be manually kept as original/paraphrased content, not copied official text.

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

