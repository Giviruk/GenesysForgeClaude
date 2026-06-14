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
- Infrastructure: `docker-compose.yml`, backend/frontend Dockerfiles, GitHub Actions CI.

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
- Equipped item effects on soak, defense and encumbrance.
- Derived stats calculation.
- Custom skills, talents, items and heroic abilities.
- Custom content visibility scoped by `OwnerUserId`.
- EF Core migrations and automatic `Database.Migrate()` on startup.
- Idempotent seed application for built-in content.
- Docker compose for PostgreSQL + API + web.
- CI for backend build/test and frontend lint/test/build.

## Partially implemented

- Frontend routing: implemented as local React state, not URL/browser routing.
- State management: implemented with React state/context, no external store.
- API documentation: OpenAPI is enabled, but detailed hand-written API docs were missing before this documentation pass.
- Validation: domain/application validation exists; frontend validation is basic HTML/form/state validation.
- Copyright policy: documented now, but current seed still contains paraphrased descriptions that should be reviewed manually for legal safety.
- Frontend tests: utilities and API client are tested; broad component/flow coverage is limited.
- Production readiness: Docker exists, but observability, backup automation and secret rotation are minimal.

## Not implemented yet

- Refresh tokens or session rotation.
- Password reset / email confirmation.
- Campaigns, parties, sharing, GM roles.
- Real URL routing with deep links.
- Import/export character files.
- PDF/print character sheet.
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
- Some database constraints are enforced in domain/application rather than database check constraints.

## Domain gaps

- Dedication talent is represented as a description, but no dedicated post-creation characteristic-increase workflow is implemented.
- Talent effects are limited to numeric passive bonuses plus text; active effects are not mechanically automated.
- Weapon attack stats are mostly descriptive; no attack/damage roller is implemented.
- Heroic abilities are selectable, but their effects are not mechanically automated.
- Custom content does not include sharing or versioning.

