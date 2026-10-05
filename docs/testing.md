# Testing

## Test projects found

Backend:

- `backend/tests/GenesysForge.Domain.Tests`
- `backend/tests/GenesysForge.Api.Tests`

Frontend:

- `frontend/src/api/client.test.ts`
- `frontend/src/utils/pyramid.test.ts`
- `frontend/src/utils/talentBonuses.test.ts`

## Commands

Backend:

```powershell
dotnet test backend/GenesysForge.slnx
```

Frontend:

```powershell
cd frontend
npm test
npm run lint
npm run build
```

CI runs backend restore/build/test and frontend install/lint/test/build.

## Covered areas

Domain tests cover:

- dice pool rules;
- XP costs;
- talent pyramid;
- ranked talent effective tiers;
- derived stats;
- item load;
- passive talent bonuses;
- purchase/refund validators.

API tests cover:

- register/login;
- duplicate email;
- wrong password;
- protected endpoint without token;
- character creation and sheet response;
- skill rank purchase and creation cap;
- talent pyramid via API;
- ranked talent passive bonuses;
- Terrinoth-specific content;
- inventory recalculation;
- heroic ability restrictions, 50-XP point thresholds, all upgrade-category costs and permanence;
- characteristic purchase restriction after creation;
- unknown characteristic error;
- foreign character access;
- custom skill/talent/item/heroic ability scenarios;
- character notes;
- campaign ownership, join code, members and campaign notes;
- NPC CRUD, filters, quick draft, duplicate and ownership checks;
- Game Table session, participants, initiative slots and visibility rules;
- spells and content seed modes.

Frontend tests cover:

- API client behavior, including token/unauthorized handling.
- Pyramid/talent helper utilities.
- Magic labels/difficulty helper utilities.

## Critical gaps

Not implemented yet:

- Full component tests for pages and tabs.
- Complete E2E coverage beyond the implemented auth/character/talent/equipment smoke.
- Full campaign/Game Table realtime matrix beyond the implemented campaign/join/NPC/encounter smoke.
- Visual/regression tests.
- Tests around production nginx config.
- Migration coverage for rich user graphs; CI already runs migrations against a non-empty catalog in PostgreSQL 17, in both content modes.

Partially implemented:

- Custom content is covered through API tests, but frontend custom content UI is not deeply tested.
- Auth behavior is covered by API and API client tests, but not by AuthPage component tests.

## Recommendations

- Add React Testing Library tests for `AuthPage`, `CharactersPage`, `SheetPage`.
- Add browser smoke test for the main flow once dev server workflow is stable.
- Add PostgreSQL-backed integration check for migrations before 1.0.
- For every domain bug, add a failing domain test first when feasible.
- For every public API shape change, add or update API tests and frontend types.

## PublicSafe acceptance

See [public-safe-release.md](public-safe-release.md) for the content policy and actual validation results, and [public-version-test-plan.md](public-version-test-plan.md) for all 373 planned cases. `PublicSafeTests` cover book-only HTTP output, reseed/custom isolation, structural parity and a no-private-resources artifact. CI runs the public artifact filter with `VERIFY_PUBLIC_ARTIFACT=1` and `/p:IncludePrivateContent=false`. E2E runs both private and public Docker targets; public-specific cases check references, cache privacy, release identity, OpenAPI and manifest. Public local compose override: `docker-compose.public-test.yml`. These checks do not replace production load, Safari/iOS or the complete manual acceptance matrix.
