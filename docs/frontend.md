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
  pages/CampaignsPage.tsx
  pages/NpcsPage.tsx
  pages/MagicPage.tsx
  components/SheetTab.tsx
  components/TalentsTab.tsx
  components/InventoryTab.tsx
  components/CustomTab.tsx
  components/WorkshopTab.tsx
  components/BioTab.tsx
  components/NotesTab.tsx
  components/GameTableTab.tsx
  components/EncountersTab.tsx
  components/MagicBuilder.tsx
  components/print/
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
- `CampaignsPage` — campaigns, join flow, campaign notes, encounters and game table tabs.
- `NpcsPage` — NPC/adversary list, quick draft, create/edit, duplicate and print card.
- `MagicPage` — system-level magic reference and action builder.

## Components

- `SheetTab` — characteristics, skills, wounds/strain and core sheet operations. Its heroic block is a
  read-only summary: the base effect plus only the upgrades already purchased.
- `TalentsTab` — talents, pyramid and talent purchase/refund.
- `HeroicTab` — Realms of Terrinoth only: picking the ability, its personal name and origin
  (ROT-HA-01), the primary-effect parameter (ROT-HA-02) and buying upgrades. The full text of
  not-yet-purchased upgrades and of every secondary effect lives here so the sheet stays readable.
- `InventoryTab` — items, quantities, equipment state.
- `WorkshopTab` — saved Upgrades/Crafting mode. `AttachmentsTab` selects hosts with hard points
  and installs from a spare pool beside the shop; damage states, repair and detachment remain available.
  `CraftingTab` uses target cards, material/cost chips, GM adjustments, server previews and symbol
  steppers with the existing spend budgets and resolution commands.
- `BioTab` — portrait uploader, five-field completion, motivation cards and background word count.
  Its private `NotesTab` supports inline create/edit and a deletion confirmation; campaign bio views
  are read-only and never request or render private notes.
- `TransportTab` — mount/wagon cards with activity switches, wound steppers, traction/cargo chips
  and a stable catalogue. Quantity, barding approval and purchase/sale price controls are preserved.
- `CustomTab` — create/update/delete custom content.
- `GameTableTab` — campaign active scene, participants, story points and initiative slots.
- `EncountersTab` — campaign encounter builder and send-to-table flow.
- `MagicBuilder` — magic action composition, difficulty, dice pool and print/Markdown export.
  Direction and action selection is shared with the clickable `SpellsTab` matrix inside `MagicTab`;
  tools/shards, mandatory/free/repeated effects, Knowledge ratings and configuration keep their rules.
- `components/print/*` — browser print preview and printable cards for NPCs, encounters, magic actions, items and talents.
- `DicePoolView` — displays ability/proficiency pool.

## Character sheet v2.1

Desktop and mobile tabs use the same order: Sheet, Talents, Inventory, Magic; Heroic (Terrinoth
only), Workshop, Transport, Bio and notes, History. The campaign member view has the same order
and shows attachments under its read-only Workshop. Printing keeps its existing sections.

`uiPreferences.ts` migrates saved `attachments`/`crafting` tabs to `workshop` with the matching
mode, and `notes` to `bio`. The selected workshop mode uses
`genesysforge.workshop-mode.<characterId>` (`upgrades`/`craft`). Workshop requests the `base`,
`items` and `attachments` slices; Bio only needs `base`, with notes loaded separately by the owner.

The Heroic activation counter uses `genesysforge.heroic-uses.<characterId>`. A successful activation
consumes one local use; a rejected command consumes none. Frequency sets the available uses;
New session resets them. This browser-local counter is the product owner's confirmed choice and
is independent of the Game Table's server-side participant/session counter.

The new panels stack on narrow screens and release sticky positioning. `layout.spec.ts` covers
both languages at 320, 390, 768, 1280 and 1440 px, including standalone `/magic` and campaign bio privacy.

## Routing

URL-backed via a custom History-API router in `frontend/src/router.ts` (no `react-router` dependency). It exposes `currentPath`, `navigate`, `usePath` and `parseRoute`; nginx and the Vite dev server have an SPA fallback so direct navigation/refresh on any path serves the app.

`App.tsx` derives the view from the parsed route:

- no token -> `AuthPage` (login/register handled by path);
- `/` or `/characters` -> `CharactersPage`; `/characters/:id` -> `SheetPage`;
- `/campaigns[/:id]`, `/npcs[/:id]`, `/magic` -> the matching area;
- unknown path -> "not found" state.

After login the user is returned to the originally requested URL (`session.ts`). Not every sub-view (printable sheet, Game Table, encounter detail) has its own deep link yet.

## API work

All backend calls are centralized in `frontend/src/api/client.ts`.

The client handles:

- token storage in `localStorage`;
- `Authorization: Bearer` header;
- JSON request/response;
- `ApiError`;
- clearing token and notifying auth provider on `401` with existing token.

## Auth flow

`AuthProvider` reads token from local storage, exposes `login`, `register`, `logout` and resets session on unauthorized callback. `AuthPage` calls context methods and renders forgot/reset-password screens plus a Google sign-in button when `GET /api/auth/providers` reports a configured client. A `401` with an existing token clears it and shows a session-expired message while preserving the intended route. Access tokens are silently refreshed via the `gf_refresh` cookie (`POST /api/auth/refresh`); logout revokes the refresh-token family.

## PWA / offline

The Vite build uses `vite-plugin-pwa` in `frontend/vite.config.ts`.

- `manifest.webmanifest` is generated at build time with standalone display metadata and `frontend/public/pwa-icon.svg`.
- `frontend/src/pwa.ts` registers the generated service worker with auto-update enabled.
- Static build assets are precached by Workbox.
- Read-only reference endpoints are cached with `NetworkFirst` in cache `genesysforge-reference-v1`:
  - `/api/reference/*`
  - `/api/v1/reference/*`
  - `/api/spells/*`
  - `/api/v1/spells/*`

Offline reference access works after the user has previously loaded the same reference data online. Mutating API calls, auth refresh and realtime SignalR traffic are intentionally not cached.

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
- character notes;
- campaign create/join/notes;
- NPC create/edit/quick draft;
- encounter and game table forms;
- magic action builder controls;
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
- Deep links exist for main areas but not every sub-view (printable sheet, Game Table, encounter detail).
- No global data cache.
- UI behavior depends on full refetch after mutations (now nudged by SignalR invalidation events for campaign/Game-Table views).

