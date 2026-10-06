# AI Context

GenesysForge is a web app for Genesys Core and Realms of Terrinoth character sheets. Current repo already contains a working backend, frontend, tests, Docker and CI. Read this file with `AGENTS.md` before implementing tasks.

## Current stack

Backend: .NET 10, ASP.NET Core Minimal API, EF Core 10, Npgsql/PostgreSQL 17, xUnit, JWT Bearer auth. Frontend: React 19, TypeScript 6, Vite 8, Vitest, React Testing Library. Infra: Docker compose (`postgres`, `api`, `web`), nginx frontend container, GitHub Actions.

## Structure

Backend solution: `backend/GenesysForge.slnx`. Projects: `GenesysForge.Domain`, `GenesysForge.Application`, `GenesysForge.Infrastructure`, `GenesysForge.Api`. Tests: `backend/tests/GenesysForge.Domain.Tests`, `backend/tests/GenesysForge.Api.Tests`.

Frontend lives in `frontend/src`: `api/client.ts`, `api/types.ts`, `pages`, `components`, `utils`, `auth.tsx`, `router.ts`. Routing uses a lightweight History-API router (`router.ts`) with deep links for characters/campaigns/npcs/magic; not every sub-view has its own URL yet.

Dependency direction: `Api -> Infrastructure -> Application -> Domain`. Domain must stay free of EF/HTTP/DI.

## Implemented

JWT register/login, character CRUD, reference data, Genesys Core and Realms of Terrinoth systems, creation phase, XP spending for characteristics/skills/talents, refunds during creation, talent pyramid, ranked talents, Terrinoth heroic ability assignment and upgrades, inventory/equipment, money, item sale, derived stats, character notes, custom skills/talents/items/heroic abilities scoped by owner, campaigns with join code and notes, NPC/adversary library, encounters, Game Table, content packs, magic reference/action builder, print cards, EF migrations, idempotent seed data, CI and VPS deploy workflow.

Reference content model: every reference def (`SkillDef`/`TalentDef`/`ItemDef`/`ArchetypeDef`/`CareerDef`/`HeroicAbilityDef`, plus `SpellDef`) carries `Code` (stable key), `NameRu`, `Name` (original/EN), `Description` (full/private), `SafeDescription` (public) and `Source` via `IContentDef`. Two seed pipelines are selected by `ContentMode` (`Content:Mode`, default `PrivateFull`). PublicSafe clears full descriptions; built-in talents, heroic abilities, Power upgrades and Secondary Effects expose names and book/page references only (RU/EN prose is empty). Other definitions retain safe paraphrases. Private talent/heroic prose lives in the conditional `private-content/rule-text.ru.json` resource, excluded by `/p:IncludePrivateContent=false` and the public Docker target. PublicSafe and PrivateFull keep the same calculation rules. Reseed updates built-ins (including retired legacy prose) while preserving IDs and custom content. Three legacy Core talents have unconfirmed pages; see [public-safe-release.md](public-safe-release.md). Public and private deployments use separate databases.

Talents carry a `Setting` (`[Flags] GenesysSetting`) and are data-driven: built-in talents come from the embedded `Persistence/SeedContent/talents.catalog.json` catalog (`TalentCatalog`), generated from source CSVs (structure + reworked descriptions). Reference filtering: Genesys Core lists `Any`-setting talents; Realms of Terrinoth lists `Any` + `Fantasy`; a character's own custom talents always show. `_books/` (source PDFs/CSVs) is gitignored and must never be committed.

Also implemented: Google sign-in (disabled until `Auth:Google:ClientId` is set), refresh-token rotation with `HttpOnly` cookie, self-service password reset (e-mail stubbed to log), URL deep links, and SignalR real-time campaign/Game-Table events. Partially implemented: deep links for every sub-view, UI validation, frontend component test coverage, mechanical talent/heroic ability effects, production operations, real e-mail delivery for password reset. Implemented as well: shareable character-sheet links, JSON character import/export, printable character sheets/cards, Chromium E2E smoke and versioned `/api/v1` routes with legacy aliases. Full manual/visual and cross-browser acceptance is still pending.

## Core entities

`User`; `SkillDef`; `TalentDef`; `ItemDef`; `HeroicAbilityDef`; `ArchetypeDef`; `CareerDef`; `SpellDef`; `Character`; `CharacterSkill`; `CharacterTalent`; `CharacterItem`; `CharacterNote`; `Campaign`; `CampaignCharacter`; `CampaignNote`; `CampaignChronicleChapter`; `CampaignChronicleRevision`; `Npc`; `NpcSkill`; `NpcAbility`; `Encounter`; `EncounterParticipant`; `GameSession`; `GameParticipant`; `InitiativeSlot`. `OwnerUserId = null` means built-in reference content; non-null means custom content owned by one user.

## Key rules

Available XP = `TotalXp - SpentXp`. Dice pool: proficiency `min(characteristic, ranks)`, ability `max(characteristic, ranks) - proficiency`. Characteristic upgrade: creation only, cost `10 * newValue`, max creation value 5. Skill rank: max 2 during creation, max 5 overall, cost `5 * newRank` plus 5 if non-career. Free career skill ranks are not refundable.

Talent cost = `5 * effectiveTier`. Ranked effective tier = `min(baseTier + ranksAlreadyOwned, 5)`. Unranked talents can be bought once. Talent pyramid must remain valid after buy/refund: lower tier counts must be strictly greater than the tier above when upper tier exists.

Derived stats: wounds = archetype wound base + Brawn + talent bonuses; strain = archetype strain base + Willpower + talent bonuses; soak = Brawn + equipped armor soak + talent bonuses; item defense does not stack, use max equipped item defense then add talent defense; encumbrance threshold = `5 + Brawn + equipped item threshold bonuses`; equipped armor load = `max(0, encumbrance - 3) * quantity`; encumbered when load > threshold.

Heroic abilities are for Realms of Terrinoth characters; Genesys Core assignment is rejected.
They start with 0 ability points and gain 1 per complete 50 XP above species starting XP. Supported
upgrades are Power (1 then 2 points), repeatable Duration (1), repeatable Frequency (2), Story (1 once),
and up to two different Secondary Effects (1 each). Purchases are permanent after creation.

## API

Public: `POST /api/v1/auth/register`, `/login`, `/google`, `/password-reset/request`, `/password-reset/confirm`, `/refresh`, `/logout`, `GET /api/v1/auth/providers`, `GET /api/v1/health`. Protected: `GET /api/v1/reference/{system}`, `/api/v1/spells/{system}`, `/api/v1/characters/*`, `/api/v1/campaigns/*` (including campaign-scoped custom content), `/api/v1/npcs/*`, `/api/v1/encounters/*`, `/api/v1/content-packs/*`. Legacy `/api/*` aliases still work for compatibility. OpenAPI: `/openapi/v1.json`; Scalar UI: `/api/docs`. Real-time: SignalR hub `/hubs/campaign` (JWT-authenticated, campaign-scoped). Error body: `{ "message": "..." }`. Known exception mapping: `DomainRuleException -> 400`, `ConflictException -> 409`, `UnauthorizedException -> 401`. Full reference: [api.md](api.md).

## Commands

Full stack: `docker compose up -d --build`. Backend: `dotnet run --project backend/src/GenesysForge.Api`; tests: `dotnet test backend/GenesysForge.slnx`. Frontend: `cd frontend; npm install; npm run dev`; checks: `npm run lint; npm test; npm run build`.

## Constraints

Do not add copyrighted book text. Do not store original descriptions of talents, abilities, items, archetypes or careers. Use structural data, numeric parameters and original/paraphrased short descriptions only. For documentation-only tasks, do not change application code, migrations, dependencies, Docker or workflows. If information is absent from code, write `Not found in current codebase`; mark assumptions as `Assumption`.
