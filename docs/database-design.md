# Database Design

This file is kept as a compatibility note from the earlier documentation set.

Primary current database documentation: [database.md](database.md).

Use [database.md](database.md) as the source of truth for:

- current `AppDbContext`;
- DbSets/entities;
- tables and relationships;
- configured indexes;
- migrations;
- seed data behavior;
- missing database constraints.

When schema changes are requested, update [database.md](database.md) first. Keep this file only as a pointer unless a separate design proposal is explicitly requested.


GEN-RD-03 is implemented by `ContentLibraryV2`: independent account definitions, M:N pack membership,
book exclusions, per-entry campaign approval states, separate connections and manual book overrides.
The migration/rollback limitations and index definitions are documented in [database.md](database.md#content-library-v2-tables-gen-rd-03).
