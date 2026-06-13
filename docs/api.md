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

Known errors:

- `409` for duplicate email.
- `401`/`400` depending on application/domain validation.

### `POST /api/auth/login`

Public. Request: `LoginRequest`.

Fields:

- `email`
- `password`

Response: `AuthResponse`.

Known errors:

- `401` for wrong credentials.

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

Protected. Request: `SetHeroicAbilityRequest` with nullable `heroicAbilityId`.

Known limitation: heroic abilities are intended for Realms of Terrinoth; Genesys Core assignment is rejected by application rules.

## Inventory

### `POST /api/characters/{id}/items`

Protected. Request: `AddItemRequest`.

Fields:

- `itemDefId`
- `quantity`
- `state`

Response: `201 Created` with `{ "id": "..." }`.

### `PATCH /api/characters/{id}/items/{itemId}`

Protected. Request: `UpdateItemRequest`.

Fields:

- `state`
- `quantity`

Response: `204`.

### `DELETE /api/characters/{id}/items/{itemId}`

Protected. Removes item instance. Response: `204`.

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

