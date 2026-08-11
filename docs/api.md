# API

Current API is an ASP.NET Core Minimal API. The documented contract is available under `/api/v1`; legacy `/api/*` routes remain as a backwards-compatible alias for the current frontend and existing clients. Route headings below keep the existing `/api/...` templates for readability; each listed `/api/...` route is also available as `/api/v1/...` and OpenAPI emits the versioned form. Authentication uses JWT Bearer except for auth and health endpoints.

OpenAPI is available at `/openapi/v1.json`. Interactive Scalar API docs are available at `/api/docs`.

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
Optional query params:

- `characterId` — include/omit homebrew packs according to this character's pack toggles.
- `campaignId` — include/omit homebrew packs according to this campaign's pack toggles.

Response: `ReferenceResponse`:

- `archetypes`
- `careers`
- `skills`
- `talents`
- `items`
- `heroicAbilities`
- `attachments` — item attachments (ROT-EQP-ATT-01)
- `mounts` — purchasable transport profiles (mounts and vehicles) with their statblocks
  (ROT-MOUNT-ITEM-01, ROT-TRANSPORT-01)

The built-in Realms of Terrinoth `items` set is allowlisted: fantasy catalog rows plus the two
Core-sourced gear entries explicitly present in the approved RoT manifest (`backpack`, `rope`).
Other Core item rows remain available from the Genesys Core reference and are retired in RoT.
The active RoT reference has exactly 116 built-in item rows, including nine service rows that the
shop handles as operations rather than inventory.

The response includes built-in content plus visible custom content owned by the current user. Imported
homebrew-pack content is visible when the pack is enabled by default or enabled for the supplied
`characterId`/`campaignId`.

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

### `X-Return-Sheet` (all `/api/characters/**` edits)

Optional request header. On any edit under `/api/characters/` that would answer `204 No Content`,
sending `X-Return-Sheet: 1` makes the response `200 OK` carrying the updated `CharacterSheetDto`
instead — the same object a plain `GET /api/characters/{id}` returns, built by the same handler.

It exists because the client rereads the sheet after every edit anyway, and that second request
costs a full round-trip (250–500 ms on the deployment even over a warm connection).

The header is opt-in and changes nothing else: without it every route answers exactly as before.
Routes that already have a response body (`201 Created` on purchases, share links) are never
touched, and if the sheet cannot be built after a successful write — deleting the character, for
instance — the original `204` is returned rather than turning a successful edit into an error.

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
- items; every inventory entry carries `description`, `safeDescription`, and optional
  `descriptionEn`, so clients can use the same language/content-mode fallback as the reference
  catalogue.

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

### `POST /api/characters/{id}/duplicate`

Protected. Creates a full owned copy of the character, including sheet state, inventory, critical injuries,
motivation/background fields and notes. Response: `201 Created` with `{ "id": "..." }`.

### `POST /api/characters/{id}/share`

Protected. Creates an opaque public read-only share token for an owned character. The raw token is returned
only in this response; the database stores only a SHA-256 hash.

Response: `CharacterShareResponse`:

- `token`
- `path` — frontend path `/share/{token}`.

### `DELETE /api/characters/{id}/share`

Protected. Revokes all active public share links for the owned character. Response: `204`.

### `GET /api/share/{token}`

Public. Returns `CharacterSheetDto` for a valid non-revoked share token. No auth is required. The response is
read-only from the frontend perspective; character notes are not exposed by this public endpoint.

### `POST /api/characters/{id}/complete-creation`

Protected. Ends creation phase. Response: `204`.

### `GET /api/characters/{id}/export`

Protected (owner only). Returns the character as a portable JSON document
(`CharacterExportDto`, current format `genesysforge.character.v7` — v7 adds the signature weapon's
Improved choice and its free Supreme attachment, ROT-HA-05; v6 added the base attachment by code;
v5 added per-item transport cargo and traction; v4 added mounts; v1–v6 are still accepted on import).
Cargo and
traction reference transport by its index in the `mounts` list, never by id. References to reference content use the
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
ability during creation resets every purchased upgrade. After creation the selected ability is immutable.
Completing creation for a Realms of Terrinoth character without a selected Heroic Ability is rejected.

Known limitation: heroic abilities are intended for Realms of Terrinoth; Genesys Core assignment is rejected by application rules.

### `PUT /api/characters/{id}/heroic-upgrade`

Protected. Request: `SetHeroicUpgradeRankRequest` with `rank` (0/1/2). Sets the purchased upgrade rank of
the selected ability's Power. This legacy-compatible endpoint validates the complete ability-point budget.

### `PUT /api/characters/{id}/heroic-upgrades`

Protected. Request: `SetHeroicUpgradesRequest`:
`powerRank`, `durationRanks`, `frequencyRanks`, `story`, `secondaryEffectIds`.

Validates the RoT costs (Power 1+2, Duration 1/rank, Frequency 2/rank, Story 1, Secondary Effect 1),
at most two different secondary effects, and the available budget: one ability point per complete 50 XP
above species starting XP, with no starting point. Reductions are allowed only during creation; later
purchases are permanent.

The sheet retains `heroicUpgradeRank` as a compatible Power alias and exposes
`heroicUpgradePointsTotal`, `heroicUpgradePointsSpent`, plus structured `heroicUpgrades`.
The reference response exposes the standard `heroicSecondaryEffects`.
Game Table activation verifies that a PC uses its selected ability, spends/flips the effective Story
Point cost, and enforces `1 + frequencyRanks` activations per session.

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

### `POST /api/characters/{id}/mounts`

Protected (ROT-MOUNT-ITEM-01, ROT-TRANSPORT-01). Request: `BuyMountRequest` — `mountDefId`, plus the optional payment
fields `free`, `pricePercent` (50–200 in steps of 25), `priceOverride` with a required
`overrideReason`, and `name` (nickname).

Creates a transport instance, never a `CharacterItem`: a mount or wagon has its own statblock, so
it has no encumbrance and does not touch the owner's carried weight. Always one per call. The server
computes the sum from the catalog price (ROT-ECO-01) and spends the creation budget before the wallet.
Priceless profiles (`price: null`) can only be granted or given an explicit GM price.
Reason codes: `mount.not_found`, `mount.priceless`, `mount.retired`,
`trade.override_reason_required`, `trade.purchase_mode_ambiguous`, `character.funds.insufficient`.
Response: `201 Created` with `{ "id": "..." }`.

### `PATCH /api/characters/{id}/mounts/{mountId}`

Protected. Request: `UpdateMountRequest` — all fields optional: `name`, `woundsCurrent`, `isActive`,
`notes`, `drawnByMountId`, `clearDrawnBy`. Wounds are clamped to the profile threshold. Cargo is not
changed here — it moves through the item location endpoint below.

Traction: `drawnByMountId` hitches a draft animal, `clearDrawnBy: true` unhitches (a `null`
`drawnByMountId` changes nothing). Only a self-moving mount can draw, only one vehicle at a time, and
only a vehicle that needs traction accepts it. Reason codes: `mount.traction_not_applicable`,
`mount.traction_self`, `mount.traction_invalid`, `mount.traction_busy`.
Response: `204`.

### `POST /api/characters/{id}/mounts/{mountId}/sell`

Protected. Request: `SellMountRequest` — one of `netSuccesses` (25/50/75 % by the book),
`percent` (0–100) or `priceOverride` with `overrideReason`; optional `conditionMultiplier` with
`conditionReason`. The server computes the proceeds; during creation they restore the purchase budget
first. Transport with cargo or installed gear is rejected until it is unloaded
(`mount.load_not_empty`). Selling a draft animal unhitches whatever it was pulling instead of leaving
a dangling link.
Response: `204`.

### `DELETE /api/characters/{id}/mounts/{mountId}`

Protected. Removes the transport with no proceeds (died, released, entered by mistake) and records it
in character history. Its cargo and installed gear are not deleted: they return to the owner and
count towards their encumbrance again. Response: `204`.

### `GET /api/characters/{id}/crafting`

Protected (ROT-CRAFT-01, ROT-ALCH-02, ROT-CRAFT-MAGIC-01). Every crafting, brewing and enchanting
project of the character, newest first. Available to whoever owns the sheet — crafting has no
separate GM mode.

**Resources are description only.** Tools, components and ingredients are free text: the application
neither consumes them nor checks that the character has them, and no project ever touches money.
The component cost is computed and recorded, and that is all it does.

### `POST /api/characters/{id}/crafting/preview`

Protected. Request: `CraftingProjectInput`. Returns the normalized numbers — skill, base and
effective difficulty, base and effective time with its unit (`days`, or `hours` for one brewing
batch), listed and final component cost — plus the symbol-spend table for that kind of work.
Writes nothing.

Difficulty is `ceil(rarity / 2)`, base time `1 + rarity`, and the listed component cost
`ceil(price / 2)` — rounded **up**, unlike sale proceeds. `costPercent` (50…200 in steps of 25) and
`costOverride` are the same two mutually exclusive modes as a purchase (ROT-ECO-01): the fraction
applies to the computed cost and rounds down, an own price replaces it and requires
`costOverrideReason`. `difficultyOverride` and `timeOverride` likewise require their reasons.
Enchanting has no recipe: its base difficulty is Formidable (5) and its listed cost is 0, so both
are meant to be set explicitly.

The work kind fixes the allowed catalog and skill: ordinary items use `Mechanics` (or `Survival`
with `roughSurvival`), the twelve ROT-ALCH-01 consumables use `Alchemy`, and enchanting accepts a
visible magic skill supplied in `skillName` (default `Arcana` for compatible older clients). A
normal item submitted as `Potion`, or a potion submitted as `Item`, is rejected.
Shop service rows (`shopCategory = Service`) are not craftable item recipes: prepared tavern food,
drinks, lodging, hired help and paid travel remain visible as disabled entries in the client and a
direct request is rejected with `crafting.target_not_craftable`.

### `POST /api/characters/{id}/crafting`

Protected. Starts a project from the same body the preview accepts. Response: `201` with
`CreatedInCharacterResponse`. Enchanting additionally requires `baseCharacterItemId` — an item the
character owns that already has the `Superior` quality (`crafting.base_not_superior`) — and an
`intent` agreed in advance (`crafting.intent_required`). A priceless relic cannot be crafted at all
(`crafting.target_priceless`).

### `POST /api/characters/{id}/crafting/{projectId}/resolve`

Protected. Request: `CraftingResolveInput` — net successes and the advantage/threat/triumph/despair
counts, plus the chosen spends. The client rolls in its dice roller and reports the symbols, the
same convention as a sale by check (ROT-ECO-01); the server computes everything else from the table
codes and never trusts a submitted result.

Each spend is validated against the table: the symbol budget, repeatability, one effect per table
row, weapon-only rows, required parameters, the forbidden rating fields (damage, critical, soak,
Defense) and the strictly-lower-rarity rule for a combined dose. A success creates the instance with
`ItemProvenance.Crafted` (or `RoughSurvival`), the crafted deltas to encumbrance, hard points and
qualities, and a `craftNote` holding every choice in words — half the spends are rules the
application does not execute, so they are shown rather than silently dropped. A failure creates
nothing but keeps the project in history. Resolving twice is refused
(`crafting.project_not_draft`).

Enchanting does not create a second item: the agreed ability is written onto the base instance,
which is also returned as the project's result.

### `DELETE /api/characters/{id}/crafting/{projectId}`

Protected. Cancels a project that has not been resolved. A resolved project is history and is not
cancelled (`crafting.project_not_draft`). Response: `204`.

### `PATCH /api/characters/{id}/items/{itemId}/location`

Protected (ROT-TRANSPORT-01). Request: `MoveCargoRequest` — `mountId` (the transport to load onto,
`null` to take the item back to the owner), optional `quantity` (defaults to the whole stack; a
smaller number splits off part of it into a new row with the same instance properties), optional
`install` (put barding or saddlebags onto the transport instead of stowing them as cargo) and
optional `installOverrideReason`.

Barding is meant for a war mount: installing it on any other profile is refused
(`cargo.barding_requires_override`) until `installOverrideReason` carries the GM's reason, which is
then written into character history (ROT-MOUNT-NPC-01). The sheet reports both halves of that rule
so the client never parses catalog codes: `CharacterItemDto.isBarding` and
`CharacterMountDto.requiresGmApprovalForBarding`.

One atomic command in both directions: ownership and capacity are checked before anything is written,
so a half-moved item cannot happen. Cargo on a transport leaves the owner's encumbrance and equipped
gear entirely. Every move is written to character history as `CargoMoved`.
Reason codes: `item.not_found`, `mount.not_found`, `cargo.already_there`, `cargo.not_mount_gear`,
`cargo.quantity_invalid`, `cargo.quantity_exceeds_stack`, `cargo.capacity_exceeded`,
`cargo.barding_requires_override`.
Response: `204`.

### `PUT /api/characters/{id}/items/{itemId}/damage-state`

Protected (GEN-EQP-DMG-01). Request: `SetItemDamageStateRequest` with `state`
(`undamaged` | `minor` | `moderate` | `major` | `destroyed`) and optional `reason`.

The damage state belongs to the instance and is changed explicitly — a Sunder result and
in-fiction damage both come through this route, because the app does not resolve Sunder itself.
`major`/`destroyed` drop soak, defense, container threshold bonus and attachment effects while
keeping weight and contents, and clear the active-armor selection. Response: `204`.

### `POST /api/characters/{id}/items/{itemId}/repair`

Protected (GEN-EQP-DMG-01). Request: `RepairItemRequest` — all fields optional: `free`,
`netAdvantages` (10 % off the material cost each), `costOverride` with a required
`overrideReason`.

No check is rolled (owner decision): the server charges materials and sets the state back to
`undamaged`. Material cost is 25/50/100 % of the *instance* price (craftsmanship included, trade
markup and attachment prices excluded), rounded up to a whole coin. `undamaged` and `destroyed`
are rejected. The sheet carries the same numbers up front in `items[].repair`. Response: `204`.

### `PUT /api/characters/{id}/items/{itemId}/implement`

Protected (ROT-MAG-IMP-01). Request: `SetImplementConfigurationRequest` with `effectCodes`
(English additional-effect codes) and an optional `overrideReason`.

A Magic Tome takes up to two effects (their printed increases usually total no more than 3; going
over needs the reason), a Magic Wand exactly one effect whose printed increase is `+1`. Until the
GM configures it, the instance keeps its ordinary stats but grants no free effect
(`items[].implement.pending`). Effect increases come from the spell reference, not from the
request. Response: `204`.

### `PUT /api/characters/{id}/items/{itemId}/lesser-rune`

Protected (ROT-MAG-11). Request: `SetLesserRuneConfigurationRequest` with
`activationDescription`, `actionCode` and `effectCode`. The target must be an owned,
unconfigured Lesser Rune. The description is 3–500 characters; the chosen additional
effect must exist for the requested Runes action, be available to Runes and have a printed
difficulty increase of exactly `+1`.

The selection is permanent. It is preserved by character duplicate and v3 export/import.
Response: `204`.

Reference `items[].shard` carries the structural runebound-shard passport. Character sheet
`items[].shard` additionally carries Lesser Rune instance choices and `pending`. Shards have
nullable `price`/`rarity`, cannot use the ordinary purchase/sale routes, and may only be
granted free in a quantity of one.

### `POST /api/characters/{id}/services`

Protected (GEN-SHOP-01). Request: `BuyServiceRequest` with `itemDefId`, `quantity` and `free`.
The definition must be a service visible to the character owner and belong to the character's
game system. A paid request charges the creation purchase budget first when applicable, then
money; a free request charges neither.

The operation records `ServiceBought` in the character audit and returns `204`. It never creates
a `CharacterItem`. The ordinary `POST /api/characters/{id}/items` route rejects a service with
reason code `service.not_inventory`, so a client cannot make a service appear in inventory by
bypassing the shop UI.

### `PUT /api/characters/{id}/attachments/{attachmentId}/damage-state` and `POST …/repair`

Protected. Same requests and rules for an attachment's own damage state. A broken attachment
grants no effects but keeps occupying its host's hard point until it is detached. An attachment
with no ordinary price (`price: null`) needs `costOverride` with a reason.

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

POST   /api/custom/archetypes
PUT    /api/custom/archetypes/{id}
DELETE /api/custom/archetypes/{id}

POST   /api/custom/careers
PUT    /api/custom/careers/{id}
DELETE /api/custom/careers/{id}
```

Create/update responses return the created/updated DTO. Delete responses return `204`.

Known limitation: delete is blocked by handlers when content is used by a character.

Custom archetypes use `CreateCustomArchetypeRequest`: system, names, six characteristics, wound/strain bases,
starting XP, copyright-safe description and one optional manual archetype ability. Custom careers use
`CreateCustomCareerRequest`: system, names, description, career skill names and optional starting money. Both are
scoped to `OwnerUserId`; only the owner sees them in reference data and can create characters from them.

## Homebrew JSON packs

All routes are protected. Packs are user-owned and separate from campaign Content Packs.

```text
GET  /api/homebrew-packs/
GET  /api/homebrew-packs/{id}/export
POST /api/homebrew-packs/import
POST /api/homebrew-packs/{id}/share
POST /api/homebrew-packs/shared/{token}/import
PUT  /api/homebrew-packs/{id}/default
PUT  /api/characters/{characterId}/homebrew-packs/{packId}
PUT  /api/campaigns/{campaignId}/homebrew-packs/{packId}
```

Import/export format is `genesysforge.homebrew-pack.v1`. The JSON document contains a pack header
(`name`, `description`, `system`) and optional arrays: `skills`, `talents`, `items`, `heroicAbilities`,
`archetypes`, `careers`. Entries use stable `code` fields where supplied; missing codes are generated
from type/name.

`POST /share` returns a raw token once. `POST /shared/{token}/import` copies the shared pack into the
current user's account; it does not grant edit access to the original pack.

Toggle routes use `HomebrewPackToggleRequest`:

```json
{ "isEnabled": true }
```

Default toggle controls visibility without a reference context. Character/campaign toggles override
visibility for `GET /api/reference/{system}?characterId=...` / `?campaignId=...`.

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
POST   /api/npcs/quick-draft/preview
POST   /api/npcs/{id}/duplicate
PUT    /api/npcs/{id}
DELETE /api/npcs/{id}
```

List supports optional query filters used by the frontend: `search`, `system`, `kind`, `role`, `campaignId`, `tag`, `sort`. Create/update use `NpcInput`. Quick draft uses `QuickDraftRequest` and is deterministic for the same request. `quick-draft/preview` runs the same generator and returns the resulting `NpcDetail` without persisting anything — the quick draft form uses it for live preview.

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

Each `GameParticipant` includes the initial `count`, authoritative derived `remainingCount`, and
nullable `perMemberWoundThreshold`. For a valid minion-group snapshot the individual threshold is
`woundsThreshold / count`; a member is lost only after that threshold is exceeded. The UI rebuilds
group-skill pools from `remainingCount`. These fields are response-only derivations and require no
database columns. GMs can update wounds and strain through the participant `PATCH`; minions have no
separate strain tracker.

Each participant also exposes persistent `boostDice` and `setbackDice` counters. A GM can change
them through the participant `PATCH` (values are clamped to `0..20`). The Game Table adds these dice
to generic participant rolls and to NPC skill/attack pools. Player self-edit permission does not
grant permission to change these counters.

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

API v1 is path-versioned under `/api/v1/*`. Existing `/api/*` endpoints are still served for backwards compatibility, but new integrations should use `/api/v1/*`. The OpenAPI document intentionally emits versioned paths only.
