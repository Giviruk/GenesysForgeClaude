# Open Questions

## Domain questions

1. ~~Should `Dedication` become a fully automated post-creation characteristic increase flow, or remain descriptive?~~ Resolved: automated — picking the talent prompts for a characteristic and increases it (`GrantsCharacteristic`).
2. Which active talent effects should be mechanically automated versus stored as text only?
3. Should heroic abilities have structured mechanical fields, cooldowns and activation costs?
4. Should weapon stats become structured enough for attack/damage rolling?
5. What exact scope of Realms of Terrinoth content is legally and product-wise acceptable?

## Technical questions

1. Should API versioning be introduced before release 1.0?
2. Should startup migrations remain automatic in production?
3. Should database check constraints be added for ranks, XP, tier and quantity?
4. Should frontend adopt real routing for character URLs?
5. Should CI include PostgreSQL integration tests instead of only InMemory API tests?

## UX questions

1. Should character sheet be shareable by URL?
2. Should creation phase be a wizard instead of the current modal + sheet workflow?
3. How should XP history be displayed?
4. Should custom content be managed globally or per character context?
5. What mobile layout is expected for full sheet editing?

## Legal/copyright questions

1. Are current seed descriptions sufficiently original/paraphrased?
2. Are official names acceptable to store while descriptions are not?
3. Should the app include a user-facing copyright disclaimer?
4. Should custom content import include warnings against copying official text?
5. Should built-in seed data be reduced further to avoid legal risk?

## Not found in current codebase

- Formal legal policy document beyond docs created in this pass.
- Product decision on sharing/community content.
- Release criteria for 1.0.

