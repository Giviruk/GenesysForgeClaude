# API

Current API is an ASP.NET Core Minimal API under `/api`. Authentication uses JWT Bearer except for auth and health endpoints.

## Auth

### `POST /api/auth/register`

Public. Request: `RegisterRequest`.

Fields:

- `email`
- `password`
- `displayName`

Response: `AuthResponse` with `token`, `userId`, `email`, `displayName`.

New accounts start with `EmailConfirmed = false`; a confirmation link is "sent" (stubbed to the
API log via `LoggingEmailSender`, base address `App:BaseUrl`). Accounts created before this feature
are treated as confirmed.

Known errors:

- `409` for duplicate email.
- `401`/`400` depending on application/domain validation.

### `POST /api/auth/login`

Public. Request: `LoginRequest`.

Fields:

- `email`
- `password`

Response: `AuthResponse`.

When `Auth:RequireEmailConfirmation` is `true`, unconfirmed users are rejected with `401` until
they confirm. Default is `false` (private MVP does not block login).

Known errors:

- `401` for wrong credentials, or an unconfirmed email when confirmation is required.

### `POST /api/auth/email/confirm`

Public. Request: `ConfirmEmailRequest` (`token`). Marks the email confirmed and invalidates the
token (single-use). Returns `204`.

- `400` for an invalid/expired/used token.

### `POST /api/auth/email/resend`

Public. Request: `ResendEmailConfirmationRequest` (`email`). Always returns `204` (no enumeration);
re-sends a confirmation link only if the account exists and is not yet confirmed.

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

### `GET /api/health`

Public. Returns `{ "status": "ok" }`.

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

