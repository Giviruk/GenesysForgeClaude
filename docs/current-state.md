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
- Google sign-in (`POST /api/auth/google`) validating Google ID tokens against Google's JWKS, linking by verified email; fully implemented but disabled until `Auth:Google:ClientId` is configured. `GET /api/auth/providers` advertises availability to the frontend.
- Refresh tokens with rotation: short-lived access JWT plus a long-lived `HttpOnly` refresh cookie (`gf_refresh`); `POST /api/auth/refresh` rotates the token, reuse of a rotated token revokes the whole family, `POST /api/auth/logout` revokes the current family.
- Self-service password reset endpoints (`POST /api/auth/password-reset/request` / `/confirm`): hashed single-use token with TTL, no user enumeration; e-mail delivery is still a log stub (`LoggingEmailSender`).
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
- URL routing / deep links via a lightweight History-API router (`frontend/src/router.ts`): `/characters/:id`, `/campaigns/:id`, `/npcs/:id` and `/magic` survive refresh (SPA fallback in nginx/Vite); login returns to the intended URL (`session.ts`), and a session-expired message is shown on `401`.
- Real-time campaign updates over SignalR (hub `/hubs/campaign`): thin `GameTableChanged` / `CampaignChanged` invalidation events authorized by campaign membership; REST stays the source of truth.
- Inventory shop search/filter matching name/description/properties with a dedicated tag picker.
- Character JSON export/import (`genesysforge.character.v1`): `GET /api/characters/{id}/export`, `POST /api/characters/import` (always creates a new character) and `POST /api/characters/import/preview`. References resolve by `Code` (fallback `System`+`Name`); unresolved custom content is skipped with warnings. Frontend: «Экспорт JSON» on the sheet and «Импорт JSON» with a preview dialog on the character list.
- Full printable character sheet: «Печать листа» on the sheet opens a print-friendly document (identity, characteristics, derived stats, skills with dice pools, talents, heroic ability, inventory by state, notes) via the shared `PrintPreview` overlay; `@media print` (A4) hides the app chrome and avoids mid-block page breaks for browser print / save-to-PDF.
- EF Core migrations and automatic `Database.Migrate()` on startup.
- Content model on all reference entities (`Code`, `NameRu`, `Name`/original, `Description` full, `SafeDescription`, `Source`) via `IContentDef`.
- Talents have a `Setting` (`[Flags] GenesysSetting`) and are data-driven from an embedded catalog (`talents.catalog.json`, ~120 entries). Reference listing filters by setting: Genesys Core → `Any` only; Realms of Terrinoth → `Any` + `Fantasy`. Russian names (`NameRu`) shown in the talents UI.
- Two idempotent seed pipelines selected by `ContentMode` (`Content:Mode` config): `PrivateFull` (full descriptions, overlaid from `private-content/*.ru.json`) and `PublicSafe` (no full descriptions; keeps Russian names, safe descriptions and source references). Public set is structurally complete on its own; pipelines stay isolated.
- Idempotent seed application for built-in content.
- Docker compose for PostgreSQL + API + web.
- CI for backend build/test and frontend lint/test/build.
- Production auth rate limiting by client IP; production startup rejects weak/default JWT keys
  and non-HTTPS/missing CORS origins.
- Production refresh cookies are always `Secure`; forwarded HTTPS/IP headers are handled behind
  Caddy/nginx.
- Structured JSON console/request logging and DB-aware `/api/health`.
- Separate PrivateFull and PublicSafe production API/database/web stacks; the public API image is
  published without embedded `private-content` resources.
- Automated daily PostgreSQL dumps plus documented backup/restore/rollback/release procedures.

## Partially implemented

- Frontend routing: top-level areas and entity detail are URL-backed (History API), but not every sub-view has a deep link yet (e.g. the printable sheet, Game Table and encounter sub-routes are still reached from within the campaign/character view).
- Password reset: endpoints, hashed single-use tokens and frontend forgot/reset screens exist, but real e-mail delivery is stubbed to the API log (`LoggingEmailSender`) until a provider is configured.
- State management: implemented with React state/context, no external store.
- API documentation: OpenAPI is enabled; hand-written docs cover the main routes but should be kept in sync after endpoint changes.
- Validation: domain/application validation exists; frontend validation is basic HTML/form/state validation.
- Copyright policy: documented now, but current seed still contains paraphrased descriptions that should be reviewed manually for legal safety.
- Frontend tests: utilities and API client are tested; broad component/flow coverage is limited.
- Production readiness: Docker, dual private/public VPS compose, deploy workflow, JSON logs,
  health checks and backup automation exist; off-host backup monitoring and secret rotation remain
  operator responsibilities.

## Not implemented yet

- Real e-mail delivery for password reset (provider not selected; links are written to the log).
- Deep links for every sub-view (printable sheet, Game Table, encounter detail).
- Shareable character sheets by URL.
- Audit log or XP history.
- Full E2E browser test suite.
- Role-based administration.
- Multi-language/i18n system.
- Full official rules compendium. This may also be legally out of scope.

## Technical risks

- Seed content legal review is important because descriptions must remain original/paraphrased.
- Startup applies migrations automatically; convenient for small deployments, but should be reviewed for production operations.
- API currently exposes pre-1.0 unversioned routes.
- Password reset is implemented end-to-end except real e-mail delivery; until a provider is configured the reset link only reaches the API log, so production recovery still depends on operator access to logs.
- Refresh-token rotation works, but the access token is kept in `localStorage`; this is a deliberate trade-off that should be revisited before broad public use.
- Some database constraints are enforced in domain/application rather than database check constraints.

## Domain gaps

- Dedication (`Повышение`) increases a player-chosen characteristic by 1 per rank: `TalentDef.GrantsCharacteristic` flags such talents, the buy flow requires picking a characteristic (capped at 5, no repeats per talent), and refund reverts the increase. The choices are stored per rank in `CharacterTalent.GrantedCharacteristics`.
- Talent effects are limited to numeric passive bonuses plus text; active effects are not mechanically automated.
- Weapon attack stats are mostly descriptive; no attack/damage roller is implemented.
- Heroic abilities are selectable, but their effects are not mechanically automated.
- Custom content does not include sharing or versioning.

