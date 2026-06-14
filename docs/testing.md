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
- heroic ability restrictions;
- characteristic purchase restriction after creation;
- unknown characteristic error;
- foreign character access;
- custom skill/talent/item/heroic ability scenarios.

Frontend tests cover:

- API client behavior, including token/unauthorized handling.
- Pyramid/talent helper utilities.

## Critical gaps

Not implemented yet:

- Full component tests for pages and tabs.
- E2E tests for auth -> create character -> edit sheet.
- Visual/regression tests.
- Tests around production nginx config.
- Tests around migration application against real PostgreSQL in CI.

Partially implemented:

- Custom content is covered through API tests, but frontend custom content UI is not deeply tested.
- Auth behavior is covered by API and API client tests, but not by AuthPage component tests.

## Recommendations

- Add React Testing Library tests for `AuthPage`, `CharactersPage`, `SheetPage`.
- Add browser smoke test for the main flow once dev server workflow is stable.
- Add PostgreSQL-backed integration check for migrations before 1.0.
- For every domain bug, add a failing domain test first when feasible.
- For every public API shape change, add or update API tests and frontend types.

