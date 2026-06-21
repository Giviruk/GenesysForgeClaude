# AI Context

GenesysForge is a web app for Genesys Core and Realms of Terrinoth character sheets. Current repo already contains a working backend, frontend, tests, Docker and CI. Read this file with `AGENTS.md` before implementing tasks.

## Current stack

Backend: .NET 10, ASP.NET Core Minimal API, EF Core 10, Npgsql/PostgreSQL 17, xUnit, JWT Bearer auth. Frontend: React 19, TypeScript 6, Vite 8, Vitest, React Testing Library. Infra: Docker compose (`postgres`, `api`, `web`), nginx frontend container, GitHub Actions.

## Structure

Backend solution: `backend/GenesysForge.slnx`. Projects: `GenesysForge.Domain`, `GenesysForge.Application`, `GenesysForge.Infrastructure`, `GenesysForge.Api`. Tests: `backend/tests/GenesysForge.Domain.Tests`, `backend/tests/GenesysForge.Api.Tests`.

Frontend lives in `frontend/src`: `api/client.ts`, `api/types.ts`, `pages`, `components`, `utils`, `auth.tsx`. Routing is local React state, not URL routing.

Dependency direction: `Api -> Infrastructure -> Application -> Domain`. Domain must stay free of EF/HTTP/DI.

## Implemented

JWT register/login, character CRUD, reference data, Genesys Core and Realms of Terrinoth systems, creation phase, XP spending for characteristics/skills/talents, refunds during creation, talent pyramid, ranked talents, Terrinoth heroic ability assignment and upgrades, inventory/equipment, money, item sale, derived stats, character notes, custom skills/talents/items/heroic abilities scoped by owner, campaigns with join code and notes, NPC/adversary library, encounters, Game Table, content packs, magic reference/action builder, print cards, EF migrations, idempotent seed data, CI and VPS deploy workflow.

Reference content model: every reference def (`SkillDef`/`TalentDef`/`ItemDef`/`ArchetypeDef`/`CareerDef`/`HeroicAbilityDef`, plus `SpellDef`) carries `Code` (stable key), `NameRu`, `Name` (original/EN), `Description` (full/private), `SafeDescription` (public) and `Source` via `IContentDef`. Two seed pipelines are selected by `ContentMode` (`Content:Mode` config, default `PrivateFull`): `PrivateFull` fills full descriptions (overlaid from embedded `private-content/*.ru.json`); `PublicSafe` clears full descriptions and keeps only Russian names, safe descriptions and sources. Both are idempotent and isolated; the public set is structurally complete without private data. This prepares for the later `AppMode` Private/Public split (not implemented yet). `private-content/` holds own paraphrases (not book text) and must be removed before the repo is opened publicly.

Talents carry a `Setting` (`[Flags] GenesysSetting`) and are data-driven: built-in talents come from the embedded `Persistence/SeedContent/talents.catalog.json` catalog (`TalentCatalog`), generated from source CSVs (structure + reworked descriptions). Reference filtering: Genesys Core lists `Any`-setting talents; Realms of Terrinoth lists `Any` + `Fantasy`; a character's own custom talents always show. `_books/` (source PDFs/CSVs) is gitignored and must never be committed.

Partially implemented: frontend routing/state, UI validation, frontend component test coverage, mechanical talent/heroic ability effects, production operations. Not implemented yet: refresh tokens/session rotation, password reset, shareable deep links, import/export character files, full printable character sheet, E2E tests, API versioning.

## Core entities

`User`; `SkillDef`; `TalentDef`; `ItemDef`; `HeroicAbilityDef`; `ArchetypeDef`; `CareerDef`; `SpellDef`; `Character`; `CharacterSkill`; `CharacterTalent`; `CharacterItem`; `CharacterNote`; `Campaign`; `CampaignCharacter`; `CampaignNote`; `Npc`; `NpcSkill`; `NpcAbility`; `Encounter`; `EncounterParticipant`; `GameSession`; `GameParticipant`; `InitiativeSlot`; `ContentPack`; `ContentPackEntry`. `OwnerUserId = null` means built-in reference content; non-null means custom content owned by one user.

## Key rules

Available XP = `TotalXp - SpentXp`. Dice pool: proficiency `min(characteristic, ranks)`, ability `max(characteristic, ranks) - proficiency`. Characteristic upgrade: creation only, cost `10 * newValue`, max creation value 5. Skill rank: max 2 during creation, max 5 overall, cost `5 * newRank` plus 5 if non-career. Free career skill ranks are not refundable.

Talent cost = `5 * effectiveTier`. Ranked effective tier = `min(baseTier + ranksAlreadyOwned, 5)`. Unranked talents can be bought once. Talent pyramid must remain valid after buy/refund: lower tier counts must be strictly greater than the tier above when upper tier exists.

Derived stats: wounds = archetype wound base + Brawn + talent bonuses; strain = archetype strain base + Willpower + talent bonuses; soak = Brawn + equipped armor soak + talent bonuses; item defense does not stack, use max equipped item defense then add talent defense; encumbrance threshold = `5 + Brawn + equipped item threshold bonuses`; equipped armor load = `max(0, encumbrance - 3) * quantity`; encumbered when load > threshold.

Heroic abilities are for Realms of Terrinoth characters; Genesys Core assignment is rejected.

## API

Public: `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/health`. Protected: `GET /api/reference/{system}`, `/api/spells/{system}`, `/api/characters/*`, `/api/custom/*`, `/api/campaigns/*`, `/api/npcs/*`, `/api/encounters/*`, `/api/content-packs/*`. Error body: `{ "message": "..." }`. Known exception mapping: `DomainRuleException -> 400`, `ConflictException -> 409`, `UnauthorizedException -> 401`. API is unversioned.

## Commands

Full stack: `docker compose up -d --build`. Backend: `dotnet run --project backend/src/GenesysForge.Api`; tests: `dotnet test backend/GenesysForge.slnx`. Frontend: `cd frontend; npm install; npm run dev`; checks: `npm run lint; npm test; npm run build`.

## Constraints

Do not add copyrighted book text. Do not store original descriptions of talents, abilities, items, archetypes or careers. Use structural data, numeric parameters and original/paraphrased short descriptions only. For documentation-only tasks, do not change application code, migrations, dependencies, Docker or workflows. If information is absent from code, write `Not found in current codebase`; mark assumptions as `Assumption`.

