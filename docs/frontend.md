# Frontend

## Stack

- React 19
- TypeScript 6
- Vite 8
- Vitest
- React Testing Library
- CSS in `frontend/src/index.css`

## Folder structure

```text
frontend/src/
  api/client.ts
  api/types.ts
  pages/AuthPage.tsx
  pages/CharactersPage.tsx
  pages/SheetPage.tsx
  components/SheetTab.tsx
  components/TalentsTab.tsx
  components/InventoryTab.tsx
  components/CustomTab.tsx
  components/DicePoolView.tsx
  utils/
  auth.tsx
  auth-context.ts
  App.tsx
```

## Pages

- `AuthPage` — login/register form.
- `CharactersPage` — list of characters and create-character modal.
- `SheetPage` — selected character sheet with tabs.

## Components

- `SheetTab` — characteristics, skills, wounds/strain and core sheet operations.
- `TalentsTab` — talents, pyramid and talent purchase/refund.
- `InventoryTab` — items, quantities, equipment state.
- `CustomTab` — create/update/delete custom content.
- `DicePoolView` — displays ability/proficiency pool.

## Routing

Partially implemented.

There is no `react-router` or browser URL routing in current code. Navigation is controlled by state in `App.tsx`:

- no token -> `AuthPage`;
- token and no selected character -> `CharactersPage`;
- token and selected character id -> `SheetPage`.

Known issue: refreshing the browser loses selected character screen because the selection is not encoded in URL.

## API work

All backend calls are centralized in `frontend/src/api/client.ts`.

The client handles:

- token storage in `localStorage`;
- `Authorization: Bearer` header;
- JSON request/response;
- `ApiError`;
- clearing token and notifying auth provider on `401` with existing token.

## Auth flow

`AuthProvider` reads token from local storage, exposes `login`, `register`, `logout` and resets session on unauthorized callback. `AuthPage` calls context methods. Logout clears token and returns to auth screen.

## State management

- React context for auth.
- Local `useState`/`useEffect` for pages and forms.
- No external state library.
- Data is refreshed after mutations by calling backend again.

## Forms

Current forms:

- login/register;
- create character;
- XP edit;
- custom content forms;
- inventory add/update controls;
- talent/skill/characteristic action controls.

Validation is basic and split between HTML required fields, local guards and backend validation.

## Tests

Found:

- API client tests.
- utility tests for talent/pyramid helpers.

Critical gaps:

- AuthPage component tests.
- CharactersPage create flow tests.
- SheetPage tab interaction tests.
- Inventory and custom content component tests.
- Browser/E2E smoke tests.

## Known issues

- Text in terminal output may appear mojibaked on some Windows code pages, but source files are intended as UTF-8.
- No URL deep linking.
- No global data cache.
- UI behavior depends on full refresh after mutations.

