# Current State

Документ фиксирует фактическое состояние репозитория на момент анализа. Это не roadmap и не список желаемой функциональности.

## Найденные проекты и стек

- Backend solution: `backend/GenesysForge.slnx`.
- Backend projects:
  - `GenesysForge.Domain`
  - `GenesysForge.Application`
  - `GenesysForge.Infrastructure`
  - `GenesysForge.Api`
- Backend tests:
  - `GenesysForge.Domain.Tests`
  - `GenesysForge.Api.Tests`
- Frontend: `frontend`, React + TypeScript + Vite.
- Frontend tests: Vitest test files in `frontend/src/api` and `frontend/src/utils`.
- Infrastructure: `docker-compose.yml`, `docker-compose.prod.yml`, backend/frontend Dockerfiles, GitHub Actions CI and deploy workflow.

## Implemented

- User registration and login with JWT.
- Protected API endpoints via JWT Bearer.
- Character list, create, read, update, delete.
- Game systems: `GenesysCore`, `RealmsOfTerrinoth`.
- Built-in reference data for skills, archetypes, careers, talents, items, heroic abilities.
- Magic/spell reference: spell effects and additional-effect modifiers per magic skill, with system differences (Runes/Verse for Terrinoth), Russian names, safe descriptions and source references; browsable in a frontend "Магия" tab with a magic-skill dropdown.
- Character creation phase with XP spending restrictions.
- Characteristic buy/refund during creation.
- Skill rank buy/refund with career/non-career XP cost and creation cap.
- Talent buy/refund with ranked/unranked handling and talent pyramid validation.
- Realms of Terrinoth heroic ability assignment.
- Inventory add/update/delete.
- Money tracking, paid item purchase and item sale.
- Equipped item effects on soak, defense and encumbrance.
- Derived stats calculation.
- Character notes.
- Custom skills, talents, items and heroic abilities.
- Custom content visibility scoped by `OwnerUserId`.
- Campaigns: GM-owned campaigns, join code, character membership and campaign notes with private/public visibility.
- NPC/adversary library with filters, deterministic quick draft, duplication and ownership checks.
- Encounter builder for campaign scenes, participants, hidden/defeated flags, print view and send-to-table flow.
- Game Table / GM cockpit: active campaign session, story points, participants, initiative slots, next turn, reset and end session.
- Campaign Handbook / Content Packs with campaign-scoped entries.
- Magic Action Builder with difficulty calculation, character magic dice pool, print card and Markdown copy.
- Print preview/cards for NPCs, encounters, magic actions, items and talents through browser print.
- EF Core migrations and automatic `Database.Migrate()` on startup.
- Content model on all reference entities (`Code`, `NameRu`, `Name`/original, `Description` full, `SafeDescription`, `Source`) via `IContentDef`.
- Talents have a `Setting` (`[Flags] GenesysSetting`) and are data-driven from an embedded catalog (`talents.catalog.json`, ~120 entries). Reference listing filters by setting: Genesys Core → `Any` only; Realms of Terrinoth → `Any` + `Fantasy`. Russian names (`NameRu`) shown in the talents UI.
- Two idempotent seed pipelines selected by `ContentMode` (`Content:Mode` config): `PrivateFull` (full descriptions, overlaid from `private-content/*.ru.json`) and `PublicSafe` (no full descriptions; keeps Russian names, safe descriptions and source references). Public set is structurally complete on its own; pipelines stay isolated.
- Idempotent seed application for built-in content.
- Docker compose for PostgreSQL + API + web.
- CI for backend build/test and frontend lint/test/build.

## Partially implemented

- Frontend routing: implemented as local React state, not URL/browser routing.
- State management: implemented with React state/context, no external store.
- API documentation: OpenAPI is enabled; hand-written docs cover the main routes but should be kept in sync after endpoint changes.
- Validation: domain/application validation exists; frontend validation is basic HTML/form/state validation.
- Copyright policy: documented now, but current seed still contains paraphrased descriptions that should be reviewed manually for legal safety.
- Frontend tests: utilities and API client are tested; broad component/flow coverage is limited.
- Production readiness: Docker, VPS compose and deploy workflow exist, but observability, backup automation and secret rotation are minimal.

## Not implemented yet

- Refresh tokens or session rotation.
- Password reset (endpoints exist; email delivery is stubbed to logs).
- Real URL routing with deep links.
- Shareable character sheets by URL.
- Import/export character files.
- Full printable character sheet; current print support is card/material oriented.
- Audit log or XP history.
- Full E2E browser test suite.
- Role-based administration.
- Multi-language/i18n system.
- Full official rules compendium. This may also be legally out of scope.

## Technical risks

- Seed content legal review is important because descriptions must remain original/paraphrased.
- Startup applies migrations automatically; convenient for small deployments, but should be reviewed for production operations.
- API currently exposes pre-1.0 unversioned routes.
- Frontend navigation state is lost on refresh because there is no URL route for a specific character.
- No refresh token flow; expired JWT causes logout.
- Password loss requires manual/operator intervention because self-service reset is not implemented.
- Some database constraints are enforced in domain/application rather than database check constraints.

## Domain gaps

- Dedication (`Повышение`) increases a player-chosen characteristic by 1 per rank: `TalentDef.GrantsCharacteristic` flags such talents, the buy flow requires picking a characteristic (capped at 5, no repeats per talent), and refund reverts the increase. The choices are stored per rank in `CharacterTalent.GrantedCharacteristics`.
- Talent effects are limited to numeric passive bonuses plus text; active effects are not mechanically automated.
- Weapon attack stats are mostly descriptive; no attack/damage roller is implemented.
- Heroic abilities are selectable, but their effects are not mechanically automated.
- Custom content does not include sharing or versioning.

