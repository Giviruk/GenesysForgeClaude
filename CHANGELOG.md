# Changelog

All notable changes to GenesysForge are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and the project aims to follow [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
once it reaches a tagged 1.0 release. The project is currently pre-1.0; the
`Unreleased` section tracks work on the current `master`.

## [Unreleased]

### Added
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
