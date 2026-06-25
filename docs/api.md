# API

Current API is an ASP.NET Core Minimal API under `/api`. Authentication uses JWT Bearer except for auth and health endpoints.

## Auth

### `POST /api/auth/register`

Public. Request: `RegisterRequest`.

Fields:

- `email`
- `password`
- `displayName`

Response: `AuthResponse` with `token`, `userId`, `email`, `displayName`. Registration logs the user
in immediately (no email confirmation step).

Known errors:

- `409` for duplicate email.
- `401`/`400` depending on application/domain validation.

### `POST /api/auth/login`

Public. Request: `LoginRequest`.

Fields:

- `email`
- `password`

Response: `AuthResponse` (a short-lived access JWT, default 30 min via `Jwt:AccessLifetimeMinutes`).
On success a long-lived **refresh token** is also set as an `HttpOnly` `SameSite=Lax` cookie
(`gf_refresh`, path `/api/auth`, `Secure` on HTTPS). `register` sets the same cookie.

Known errors:

- `401` for wrong credentials.

### `POST /api/auth/password-reset/request`

Public. Request: `PasswordResetRequestRequest` (`email`).

Always returns `204` regardless of whether the account exists (no user enumeration).
If the account exists, a single-use reset token (1 hour TTL) is stored hashed and a reset
link is sent. The email provider is not selected yet, so the link is written to the API log
(`LoggingEmailSender`); base address is `App:BaseUrl`.

### `POST /api/auth/password-reset/confirm`

Public. Request: `PasswordResetConfirmRequest` (`token`, `newPassword`).

Sets the new password and invalidates the token (single-use). Returns `204`.

Known errors:

- `400` for an invalid/expired/used token or a password shorter than 6 characters.

### `GET /api/auth/providers`

Public. Returns `AuthProvidersResponse` with `googleClientId` (null when Google sign-in is not
configured). The frontend uses it to decide whether to render the Google button.

### `POST /api/auth/google`

Public. Request: `GoogleSignInRequest` (`idToken` from Google Identity Services).

Validates the Google ID token against Google's JWKS and the configured `Auth:Google:ClientId`. Links
the Google identity to an existing user by **verified** email when one exists, otherwise creates a new
account; returns the usual `AuthResponse` (the frontend auth context is unchanged). Uniqueness is
enforced on (`provider`, `providerUserId`).

**OAuth decision:** Google sign-in is **optional and deferred** for the private MVP. It is fully
implemented but disabled until `Auth:Google:ClientId` (env `GOOGLE_CLIENT_ID`) is set with a Google
Cloud OAuth client. Email/password remains the primary method.

Known errors:

- `400` when Google sign-in is not configured, the token is invalid, or the email is not verified.

### `POST /api/auth/refresh`

Public (auth via the refresh cookie). Rotates the refresh token and returns a fresh `AuthResponse`.

- Each refresh issues a new refresh token in the same family and revokes the previous one.
- Presenting an already-rotated (revoked) token is treated as compromise: the whole family is
  revoked and the client must sign in again.
- `401` when the cookie is missing, invalid, expired, or the family was revoked.

### `POST /api/auth/logout`

Public (auth via the refresh cookie). Revokes the current refresh-token family and clears the
cookie. Returns `204`.

## Realtime (SignalR)

Hub at `/hubs/campaign`. Authenticated with the same JWT (passed as `access_token` query for the
WebSocket). Clients call `SubscribeCampaign(campaignId)` / `UnsubscribeCampaign(campaignId)`; the
subscribe is rejected with a `HubException` unless the user is the GM or a member of the campaign,
so outsiders cannot receive a campaign's events.

Server-sent events (thin invalidation signals — clients refetch the affected REST resource):

- `GameTableChanged(campaignId)` — after any Game Table mutation or an encounter sent to the table.
- `CampaignChanged(campaignId)` — after membership/notes changes.

REST stays the source of truth; events only tell clients what to refetch.

## Reference

### `GET /api/reference/{system}`

Protected. `system` is parsed case-insensitively into `GameSystem`.

Response: `ReferenceResponse`:

- `archetypes`
- `careers`
- `skills`
- `talents`
- `items`
- `heroicAbilities`

The response includes built-in content plus custom content owned by the current user.

Known errors:

- `400` for unknown system.
- `401` without token.

## Magic / Spells

### `GET /api/spells/{system}`

Protected. `system` is parsed case-insensitively into `GameSystem`.

Response: `List<SpellDto>` ordered by `MagicSkill`, `Kind`, `SortOrder`, `NameRu`. Each item:

- `id`
- `magicSkill` — Arcana/Divine/Primal (plus Runes/Verse for Realms of Terrinoth); empty for additional effects;
- `kind` — `effect` (базовый эффект-направление) or `additionalEffect` (модификатор сложности);
- `parentEffect` — for additional effects, the `nameEn` code of the base effect they modify (empty for base effects);
- `nameRu`, `nameEn`;
- `difficulty` — display string (base difficulty for effects, `+N` for modifiers);
- `description` — full (private) paraphrase;
- `safeDescription` — copyright-safe public text;
- `source` — book/section reference;
- `isCustom`.

The response includes built-in entries plus spells owned by the current user.

Known errors:

- `400` for unknown system.
- `401` without token.

## Characters

### `GET /api/characters/`

Protected. Returns `List<CharacterListItemDto>`.

### `POST /api/characters/`

Protected. Request: `CreateCharacterRequest`.

Fields:

- `name`
- `system`
- `archetypeId`
- `careerId`
- `freeCareerSkillNames`

Response: `201 Created` with `{ "id": "..." }`.

### `GET /api/characters/{id}`

Protected. Returns `CharacterSheetDto`:

- identity and system;
- archetype/career;
- characteristics dictionary;
- XP fields;
- creation phase flag;
- current wounds/strain;
- derived stats;
- skills with dice pools;
- talents and tier counts;
- heroic ability;
- items.

### `PATCH /api/characters/{id}`

Protected. Request: `UpdateCharacterRequest`.

Currently used by frontend for:

- `name`
- `totalXp`
- `woundsCurrent`
- `strainCurrent`
- `money`

Response: `204`.

### `DELETE /api/characters/{id}`

Protected. Deletes owned character. Response: `204`.

### `POST /api/characters/{id}/complete-creation`

Protected. Ends creation phase. Response: `204`.

### `GET /api/characters/{id}/export`

Protected (owner only). Returns the character as a portable JSON document
(`CharacterExportDto`, format `genesysforge.character.v1`). References to reference content use the
stable `Code` + `Name` instead of internal ids; `OwnerUserId` and database ids are not included.
Exporting a character you do not own returns `400` ("персонаж не найден").

### `POST /api/characters/import`

Protected. Body: a `CharacterExportDto` (the exported JSON). Always creates a **new** character owned
by the caller — it never overwrites an existing one. Returns `201 Created` with
`ImportCharacterResult` (`characterId`, `name`, `warnings`).

Resolution rules:

- Archetype/career are resolved by `Code` (fallback `System` + `Name`). If unresolved, the import is
  rejected with `400`.
- Skills/talents/items/heroic ability are resolved by `Code` for built-in content and by `Name`
  within the caller's scope (built-in or the caller's own custom) otherwise. Unresolved entries are
  skipped and reported in `warnings` (they do not block the import).
- An unknown `format` is rejected with `400`.

### `POST /api/characters/import/preview`

Protected. Same body as import. Resolves references **without** creating anything and returns
`ImportPreviewDto` (`name`, `system`, `archetypeName`, `careerName`, `totalXp`, `spentXp`,
`skillCount`, `talentCount`, `itemCount`, `noteCount`, `warnings`). Used by the frontend to show a
confirmation preview before importing. Returns `400` on unknown format or unresolved archetype/career.

## Character progression

### `POST /api/characters/{id}/characteristics/{type}/buy`

Protected. Buys one characteristic increase during creation. `type` is case-insensitive `CharacteristicType`.

### `POST /api/characters/{id}/characteristics/{type}/refund`

Protected. Refunds the last characteristic increase during creation.

### `POST /api/characters/{id}/skills/{skillDefId}/buy-rank`

Protected. Buys one skill rank.

### `POST /api/characters/{id}/skills/{skillDefId}/refund-rank`

Protected. Refunds one skill rank during creation if it is not a free rank.

### `POST /api/characters/{id}/talents/buy`

Protected. Request: `BuyTalentRequest` with `talentDefId`. Buys one talent rank.

### `POST /api/characters/{id}/talents/refund`

Protected. Request: `BuyTalentRequest` with `talentDefId`. Refunds one talent rank during creation if pyramid remains valid.

### `PUT /api/characters/{id}/heroic-ability`

Protected. Request: `SetHeroicAbilityRequest` with nullable `heroicAbilityId`. Changing or clearing the
ability resets the purchased upgrade rank to 0.

Known limitation: heroic abilities are intended for Realms of Terrinoth; Genesys Core assignment is rejected by application rules.

### `PUT /api/characters/{id}/heroic-upgrade`

Protected. Request: `SetHeroicUpgradeRankRequest` with `rank` (0/1/2). Sets the purchased upgrade rank of
the selected ability. Validates that ranks are sequential (Supreme requires Improved) and that the
cumulative cost fits the available ability points (1 starting + 1 per 50 XP earned after creation).
Lowering the rank refunds points. The sheet DTO exposes `heroicUpgradeRank`, `heroicUpgradePointsTotal`
and `heroicUpgradePointsSpent`.

## Inventory

### `POST /api/characters/{id}/items`

Protected. Request: `AddItemRequest`.

Fields:

- `itemDefId`
- `quantity`
- `state`
- `cost` optional; when present the backend charges character money

Response: `201 Created` with `{ "id": "..." }`.

### `PATCH /api/characters/{id}/items/{itemId}`

Protected. Request: `UpdateItemRequest`.

Fields:

- `state`
- `quantity`

Response: `204`.

### `DELETE /api/characters/{id}/items/{itemId}`

Protected. Removes item instance. Response: `204`.

### `POST /api/characters/{id}/items/{itemId}/sell`

Protected. Request: `SellItemRequest` with `quantity` and `proceeds`. Removes or decreases item quantity and adds proceeds to character money. Response: `204`.

## Character notes

All routes are protected and scoped to the character owner.

```text
GET    /api/characters/{id}/notes/
POST   /api/characters/{id}/notes/
PUT    /api/characters/{id}/notes/{noteId}
DELETE /api/characters/{id}/notes/{noteId}
```

Create/update use `SaveCharacterNoteRequest` with `title` and `body`. Responses return `CharacterNoteDto`; delete returns `204`.

## Custom content

All routes are protected and scoped to current user.

```text
POST   /api/custom/skills
PUT    /api/custom/skills/{id}
DELETE /api/custom/skills/{id}

POST   /api/custom/talents
PUT    /api/custom/talents/{id}
DELETE /api/custom/talents/{id}

POST   /api/custom/items
PUT    /api/custom/items/{id}
DELETE /api/custom/items/{id}

POST   /api/custom/heroic-abilities
PUT    /api/custom/heroic-abilities/{id}
DELETE /api/custom/heroic-abilities/{id}
```

Create/update responses return the created/updated DTO. Delete responses return `204`.

Known limitation: delete is blocked by handlers when content is used by a character.

## Campaigns

All routes are protected.

```text
GET    /api/campaigns/
POST   /api/campaigns/
GET    /api/campaigns/{id}
POST   /api/campaigns/join
DELETE /api/campaigns/{id}/characters/{characterId}

POST   /api/campaigns/{id}/notes
PUT    /api/campaigns/{id}/notes/{noteId}
DELETE /api/campaigns/{id}/notes/{noteId}
```

Campaign creation uses `CreateCampaignRequest` with `name` and `description`. Join uses `JoinCampaignRequest` with `joinCode` and `characterId`. A GM receives `joinCode` in campaign detail; players do not. Campaign notes use `SaveCampaignNoteRequest` with `title`, `body` and `isPrivate`; private notes are GM-only.

## NPCs / adversaries

All routes are protected and scoped by ownership/campaign visibility.

```text
GET    /api/npcs/
GET    /api/npcs/{id}
POST   /api/npcs/
POST   /api/npcs/quick-draft
POST   /api/npcs/{id}/duplicate
PUT    /api/npcs/{id}
DELETE /api/npcs/{id}
```

List supports optional query filters used by the frontend: `search`, `system`, `kind`, `role`, `campaignId`, `tag`, `sort`. Create/update use `NpcInput`. Quick draft uses `QuickDraftRequest` and is deterministic for the same request.

## Game Table

All routes are protected under a campaign.

```text
GET    /api/campaigns/{campaignId}/session/
POST   /api/campaigns/{campaignId}/session/
PATCH  /api/campaigns/{campaignId}/session/
POST   /api/campaigns/{campaignId}/session/reset
POST   /api/campaigns/{campaignId}/session/next-turn
DELETE /api/campaigns/{campaignId}/session/

POST   /api/campaigns/{campaignId}/session/participants
PATCH  /api/campaigns/{campaignId}/session/participants/{participantId}
DELETE /api/campaigns/{campaignId}/session/participants/{participantId}

POST   /api/campaigns/{campaignId}/session/slots
PATCH  /api/campaigns/{campaignId}/session/slots/{slotId}
DELETE /api/campaigns/{campaignId}/session/slots/{slotId}
```

`GET` returns `204` when there is no active session. GMs can create/reset/end scenes and manage participants/slots. Player edits are limited by campaign membership and session settings.

## Encounters

All routes are protected. Campaign-scoped list/create routes require access to the campaign; mutation routes enforce GM ownership through the encounter's campaign.

```text
GET    /api/campaigns/{campaignId}/encounters/
POST   /api/campaigns/{campaignId}/encounters/

GET    /api/encounters/{id}
PUT    /api/encounters/{id}
DELETE /api/encounters/{id}
POST   /api/encounters/{id}/participants
POST   /api/encounters/{id}/participants/characters
PATCH  /api/encounters/{id}/participants/{participantId}
DELETE /api/encounters/{id}/participants/{participantId}
POST   /api/encounters/{id}/send-to-table
```

List supports optional `search`, `type` and `tag` query filters. `send-to-table` uses `SendToTableRequest` with mode `replace` or `append`.

## Content packs

All routes are protected and campaign-scoped through ownership/access checks.

```text
GET    /api/campaigns/{campaignId}/content-packs/
POST   /api/campaigns/{campaignId}/content-packs/

GET    /api/content-packs/{id}
PATCH  /api/content-packs/{id}
DELETE /api/content-packs/{id}
POST   /api/content-packs/{id}/entries
PUT    /api/content-packs/{id}/entries/{entryId}
DELETE /api/content-packs/{id}/entries/{entryId}
```

Content packs are campaign handbook containers. Entries are typed by `ContentEntryType` and can be public to campaign members or GM-only depending on pack visibility and access rules.

## Health

`GET /api/health` checks both API availability and database connectivity.

- `200`: `{ "status": "ok", "database": "ok" }`
- `503`: `{ "status": "degraded", "database": "unavailable" }`

Auth endpoints return `429` when the configured per-IP rate limit is exceeded.

## Error model

Known exceptions are mapped centrally:

- `DomainRuleException` -> `400`
- `ConflictException` -> `409`
- `UnauthorizedException` -> `401`

Error response DTO:

```json
{ "message": "..." }
```

## Versioning

Not implemented yet. Current API is unversioned.

