# Changelog

All notable changes to GenesysForge are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and the project aims to follow [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
once it reaches a tagged 1.0 release. The project is currently pre-1.0; the
`Unreleased` section tracks work on the current `master`.

## [Unreleased]

### Fixed
- **Loading a character no longer explodes into millions of database rows.** The character graph was
  read in one query with a dozen collection `Include`s, which EF joined into a single result set —
  the rows multiply against each other. Measured on real data: a character with 40 items, 20
  talents, 3 mounts and 5 attachments expanded into **7,360,000 rows**, each carrying every joined
  table's columns including the full catalog descriptions. It ran twice per action: once for the
  edit handler, once to build the response sheet. Because items, transport and attachments are the
  multipliers, the app got monotonically slower the more a character owned — which is exactly what
  players reported on those tabs. Fixed by loading the collections as separate queries
  (`UseQuerySplittingBehavior`), making each one linear in its own size.
- **The sheet is now read in parts instead of all at once.** A played character's sheet is ~116 KB,
  and 64 % of that is the inventory the main tab never shows. `GET /api/characters/{id}/slices?include=…`
  serves only the named parts (`base`, `items`, `talents`, `mounts`, `attachments`), each with its
  own narrow query rather than the shared full-graph loader, and edits return just the parts the
  client currently has on screen (`X-Return-Slices`). A tab loads its parts when it is opened and
  keeps them. Opening a character: 116 KB → 17 KB; the Talents tab: 34 KB. Both are opt-in — the
  full `GET /api/characters/{id}` is unchanged and still serves print, export, share links and the
  GM's view.
- **Adding an item and buying transport or an attachment no longer cost an extra round-trip.** Those
  routes answer `201 Created` with the new record's id, and the filter only ever replaced
  `204 No Content`, so the client still had to reread the sheet afterwards. They now carry the
  requested parts too, with the created id in `createdId` so nothing is lost. `duplicate` and
  `import` are deliberately left alone: they also answer `201 Created`, but they create *another*
  character, and the parts would belong to the original. Adding a critical injury got the same fix.

### Changed
- `X-Return-Sheet` is replaced by `X-Return-Slices`, which names the parts to return instead of
  asking for the whole sheet. `Items`, `Talents` and `TalentTierCounts` on the sheet DTO are now
  nullable, where `null` means "not requested" as distinct from an empty collection.

- **An edit in the sheet now costs a single request.** The client rereads the sheet after every
  edit, which was a second round-trip on top of the mutation — 250–500 ms on the deployment even
  over a warm connection. Edits can now carry the updated sheet back in their own response
  (`X-Return-Sheet` request header), built by the same handler that serves `GET`. Opt-in, so
  without the header every route answers `204 No Content` exactly as before. Together with the
  catalog cache below: 3 requests per click → 1.
- **The sheet no longer refetches the whole game catalog after every click.** Each action used to
  cost three sequential requests — the mutation, the sheet, and then the full reference catalog
  (~560 KB and about ten DB queries), with the UI updating only after the last one. The reference is
  now cached for the session and invalidated whenever something can actually change it (custom
  content, homebrew packs, content packs, end of session), and the sheet renders as soon as it
  arrives instead of waiting. Per click: 3 requests → 2, ~639 KB → 77 KB.
- `Haste` and `Swift` (Augment) were attached to each other's mechanics in both the
  magic reference and the quality catalog: `Haste` described ignoring difficult
  terrain and `Swift` the extra maneuver. The codes now carry the right rule
  (`Haste` = a second maneuver without strain, `Swift` = ignore difficult terrain
  and immobilization), and implements configured with either effect are migrated so
  the GM keeps the effect they actually picked.
- Magic builder now honours **which school may take which additional effect**
  (ROT-MAG-01). Previously any effect of the chosen action could be added by any
  school — a Divine caster could pick `Doom` or `Manipulative`, which the rules
  reserve for Arcana. Restricted effects are shown blocked, with the reason, and
  no longer count towards the difficulty after switching schools.
- `Despair` and `Paralyzed` can no longer be combined with `Additional Target`
  under `Curse`; the restriction used to be a sentence at the end of the
  description that nothing enforced.
- Removed the duplicate `Attack/Move` reference entry (same text and restriction
  as `Manipulative`). Implements configured with it are migrated automatically.
- NPC quick-draft generator now grants **talents** automatically per the adversary
  creation rules: the **Adversary** talent (Nemesis 1–2 by power level; combat
  Rival 1), role-specific talents, and a signature per-round ability for Nemeses.
  Previously generated NPCs (including Nemeses) had an empty talent list — e.g. a
  Nemesis had no `Adversary` talent.

### Added
- **Barding and saddlebags actually do something** (ROT-MOUNT-NPC-01, equipment scope). Barding sat
  in the catalog with soak 0 and defense 0, so installing it changed nothing. It now gives the
  animal **+2 soak and defense 1**, and saddlebags **+4 capacity**. The defense is *provided*, not
  added: it competes with the profile's own value instead of stacking, so barding lifts a beast of
  burden from 0 to 1 but adds nothing to a flying mount's printed ranged defense of 2. Barding is
  meant for a war mount — putting it on any other mount now needs an explicit GM reason, which is
  recorded in character history. Removing equipment restores the profile numbers by itself: the
  values are computed on read and never rewrite the statblock.
- **Transport section** (ROT-TRANSPORT-01). The **Mounts** tab is now **Transport** and covers
  wagons as well. The Wagon was a gear row with a fictitious `Enc 0`; it is now a vehicle with its
  own durability, system threshold, silhouette and cargo capacity, and previously bought wagons are
  converted into vehicle instances (money is not recalculated). Cargo is kept **per item** instead
  of as a single number: an inventory row can be moved onto a transport with one atomic command
  that checks ownership and capacity, splits part of a stack when asked, and writes to character
  history. Cargo on a transport leaves the owner's encumbrance entirely. Barding and saddlebags are
  installed on one specific animal — saddlebags raise its capacity, barding protects **it** and not
  the rider. A wagon is hitched to a draft animal; unhitching or selling the animal leaves the
  wagon standing with its cargo instead of dumping it on the owner. Selling transport with cargo is
  refused, deleting it returns the cargo to the owner, and export/import (`v5`), duplicate and
  deletion leave no dangling references. The printed sheet gained a Transport card.
  **Breaking:** the old free-form `carriedLoad` number on a mount is dropped — it described cargo
  with no items behind it and could not be migrated into rows.
- **Mounts are creatures now, not gear** (ROT-MOUNT-ITEM-01). The four Realms of Terrinoth
  profiles (Beast of Burden, Riding Beast, War Mount, Flying Mount) used to be catalog
  "gear" rows with `Enc 0` and the description "Gear" — buying one put a nameless line in
  the backpack. They are their own content type with the full book statblock (characteristics,
  soak, wound threshold, defense, silhouette, skills, structural attack with `Knockdown`,
  the Flyer ability, included harness/riding tack), and buying one creates a mount instance
  on a new **Mounts** tab. A mount has no weight of its own and never counts towards the
  owner's encumbrance; its capacity comes from the profile (18/12/13/12) and overrides the
  generic `5 + Brawn` rule. The Flying Mount carries the official errata: no talents and no
  printed Dodge 2. Purchase supports the same payment options as items — grant without
  payment, haggled percentage, or a GM price with a reason — and sale the same three modes.
  A mount carrying cargo cannot be sold until it is unloaded. Already-bought mount gear rows
  are converted into real mounts (quantity N becomes N creatures) with a history entry; the
  old catalog rows are retired rather than deleted. Character export moves to
  `genesysforge.character.v4`, which carries mounts; v1–v3 files still import.
- Spell effects whose rating comes from Knowledge ranks now show the actual number
  (`Burn 2`, `Pierce 3`) in the builder, the copied card and the printed card, instead
  of the sentence "equal to the caster's Knowledge ranks". In Realms of Terrinoth the
  source is `Knowledge (Lore)`; with the **Dark Insight** talent the player picks between
  Lore and `Knowledge (Forbidden)` per spell, and the sheet reports which sources are
  available. Qualities without a rating (`Sunder`, `Knockdown`, `Auto-fire`) never get one.
- Magic reference now shows the **8×5 availability matrix** (action × school) and
  explains unavailability instead of silently hiding it, plus per-effect columns
  for who may take an effect, what it cannot be combined with and whether it is
  repeatable. Expanded Player's Guide entries (`Mask`, `Predict`, `Transform`)
  are marked as optional content.
- Structural rule fields on magic entries (`AllowedSkills`, `DifficultyIncrease`,
  `Exclusions`, `Resolution`, `IsOptional`), fed by a single domain matrix
  (`MagicMatrix`) instead of being parsed out of descriptions.
- `LICENSE` (Apache-2.0) covering the project source code.
- `NOTICE` with the content & trademark disclaimer (independent fan project,
  not affiliated with Fantasy Flight Games; no official book text included).
- In-app **About** page (`/about`) with project info, links and the copyright
  disclaimer.
- Site **footer** with links to About and the changelog plus a short disclaimer
  line, visible on the auth screen and inside the app.
- This `CHANGELOG.md`.

## Baseline (pre-1.0, implemented on `master`)

These capabilities already exist in the codebase prior to introducing this
changelog. They are listed once here as a starting point; future changes will
be tracked incrementally under `Unreleased`.

### Added
- Authentication: email/password registration and login (JWT), Google sign-in
  (optional, disabled until configured), refresh tokens with rotation and
  family revocation, and self-service password reset (e-mail delivery stubbed
  to the log until a provider is configured).
- Characters: create/read/update/delete, creation phase with XP spending rules,
  buy/refund of characteristics, skill ranks and talents, talent pyramid
  validation, Realms of Terrinoth heroic abilities and upgrades.
- Inventory, equipment effects, money tracking and derived-stat calculation.
- Custom content (skills, talents, items, heroic abilities) scoped per user.
- Campaigns with join codes and notes, NPC/adversary library, encounter builder,
  Game Table / GM cockpit, and campaign handbook / content packs.
- Magic reference and Magic Action Builder; browser-print cards for game
  materials.
- Real-time campaign/Game-Table updates over SignalR.
- URL deep links for characters, campaigns, NPCs and magic.
- Two idempotent seed pipelines (`PrivateFull` / `PublicSafe`) selected by
  `ContentMode`.
- Docker compose, PostgreSQL persistence with EF Core migrations, GitHub Actions
  CI and automated VPS deploy.

[Unreleased]: https://github.com/Giviruk/GenesysForgeClaude/commits/master
