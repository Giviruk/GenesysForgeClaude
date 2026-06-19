# MVP UX and Account Readiness

Этот документ закрывает пункт MVP-review про известные UX/account ограничения и критичные UX-доработки: URL deep links, session UX, password reset/email confirmation, Google OAuth, refresh tokens/rotation, real-time collaboration и поиск/фильтры магазина.

## Current state

- Auth: `POST /api/auth/register` and `POST /api/auth/login` return a JWT. Frontend stores it in `localStorage`.
- Session expiry: API client clears the token and returns the user to auth screen on `401` when a token was present.
- Recovery: self-service password reset and email confirmation are not implemented.
- Routing: top-level navigation and selected entity state live in React state. There is no URL route for a character, campaign, NPC, encounter or content pack.
- Sharing: character sheets are not shareable by URL. Campaign access is through join code plus owned character membership.
- OAuth: Google sign-in is not implemented.
- Refresh tokens: session rotation and token revocation are not implemented.
- Collaboration: all views are request/refresh based; there are no real-time updates between users.
- Shop UX: inventory shop search exists, but MVP search/filter requirements should include description and tag matching plus a dedicated tag picker button.

## MVP decision

Private MVP can launch with the current auth model if the release notes state these limits:

- expired session requires signing in again;
- lost password is handled manually by an operator or by recreating an account;
- browser refresh returns to the top-level area instead of restoring the selected sheet/view;
- links to a specific character/campaign/NPC are not supported.

Public MVP should not launch with only manual recovery. It needs at least email-based password reset or a documented support workflow with identity verification.

Google OAuth, refresh-token rotation and real-time collaboration are not required for private MVP. For public MVP, refresh-token rotation is recommended before broad usage; Google OAuth and real-time collaboration can remain post-MVP unless they become explicit product requirements.

## Recommended implementation order

1. URL state first.

   Add lightweight browser routing before introducing account recovery. This reduces support friction immediately and is low risk because it does not change game rules or persistence model.

   Suggested routes:

   ```text
   /login
   /register
   /characters
   /characters/:characterId
   /campaigns
   /campaigns/:campaignId
   /npcs
   /npcs/:npcId
   /magic
   ```

   Implementation options:

   - minimal custom `history.pushState`/`popstate` wrapper, no dependency;
   - or `react-router` only if package changes are explicitly approved.

2. Session UX second.

   Keep JWT-only backend for MVP, but improve the user experience:

   - show a clear "session expired" message after automatic logout;
   - preserve the intended URL so login can return to the selected route;
   - document token lifetime in operator notes.

3. Password reset third.

   Minimum public-ready flow:

   - `POST /api/auth/password-reset/request` accepts email and always returns `204`;
   - backend stores a hashed single-use reset token with expiry;
   - email provider sends a reset link;
   - `POST /api/auth/password-reset/confirm` sets a new password and invalidates the token;
   - frontend adds request and confirm screens.

   Assumption: email provider and sender domain are not selected yet.

4. Email confirmation fourth.

   Add only if public signup abuse or account ownership is a real release risk. For private MVP, invite/manual account policy may be enough.

5. Google OAuth fifth.

   Use Google OAuth as an additional login method, not a replacement for email/password, unless product decides to close direct registration.

   Minimum public-ready flow:

   - create Google OAuth client for the production hostname;
   - add backend endpoint to exchange/validate Google ID token;
   - link Google identity to existing `User` by verified email when safe;
   - create a new user when no account exists;
   - preserve existing JWT response shape so the frontend auth context stays simple;
   - add UI button on login/register screens.

   Data model impact:

   - add external auth identity storage, e.g. provider `google`, provider user id, user id, verified email;
   - enforce uniqueness on provider + provider user id;
   - decide whether the same email can have both password and Google auth.

   Assumption: Google Cloud project, OAuth consent screen and allowed domains are not configured yet.

6. Refresh tokens and rotation sixth.

   Add this before broad public usage or long-lived sessions. It is not necessary for a small private MVP if short JWT lifetime plus re-login is acceptable.

   Recommended model:

   - short-lived access JWT, e.g. 10-30 minutes;
   - long-lived refresh token stored server-side as a hash;
   - one active token family per device/session;
   - rotate refresh token on every refresh;
   - revoke token family on reuse detection;
   - logout revokes the current refresh token family.

   API surface:

   ```text
   POST /api/auth/refresh
   POST /api/auth/logout
   ```

   Storage/security requirements:

   - store refresh token hash, user id, expiry, created/revoked timestamps, replacement token id and user agent/IP metadata where useful;
   - prefer HttpOnly Secure SameSite cookie for refresh token transport in browser deployment;
   - keep access token in memory where practical, or continue current storage only as a deliberate risk decision;
   - add tests for rotation, reuse detection, expiry and logout revocation.

7. Real-time collaboration seventh.

   Real-time collaboration is not needed for MVP character ownership flows. It becomes relevant when multiple users actively view/edit the same campaign, encounter or Game Table.

   Recommended first scope:

   - Game Table updates for campaign members;
   - encounter sent-to-table notifications;
   - campaign membership/notes refresh notifications.

   Suggested backend approach:

   - ASP.NET Core SignalR hub scoped by campaign id;
   - authorize hub connections with the same JWT auth;
   - join groups only after campaign access check;
   - broadcast small invalidation/update events after existing command handlers succeed;
   - keep REST as source of truth; clients refetch affected resources on events.

   Suggested frontend approach:

   - connect only while a campaign detail/Game Table view is open;
   - on event, refetch the current session/campaign/encounter instead of applying complex local patches;
   - show connection state only when it affects expected behavior.

   Not first scope:

   - character sheet co-editing;
   - operational transform/CRDT document editing;
   - offline-first conflict resolution.

8. Shop search and tag filter eighth.

   Improve the inventory shop so players can find items by how they are described, not only by visible names or categories.

   Search behavior:

   - search text should match item name, Russian display name where present, safe/full description text, source text and item properties/tags;
   - matching should be case-insensitive and tolerant of extra spaces;
   - search and filters should compose, e.g. text query + item kind + selected tags.

   Tag filter UX:

   - tag selection must open from a separate button near the search field, e.g. "Tags" / "Теги";
   - the tag picker should not be permanently expanded in the shop layout;
   - selected tags should be visible as removable chips;
   - the filter button should indicate active filters, e.g. count badge or active state;
   - include a clear/reset action for selected tags.

   Data assumptions:

   - existing item properties can be used as initial tags if no dedicated tag field exists;
   - if a dedicated tag field is added later, built-in seed and custom item forms should use the same tag model;
   - do not add copyrighted item descriptions while improving search metadata.

## Acceptance checklist

- Refreshing `/characters/:id` reopens the same character after login.
- Refreshing `/campaigns/:id` reopens the same campaign if the user has access.
- Opening a forbidden/deleted entity URL shows a clear error and a link back to the list.
- `401` clears the token, shows a session-expired message and keeps the intended route.
- Private MVP docs describe manual recovery limits.
- Public MVP either has password reset or a documented support recovery process.
- Google OAuth decision is explicit: deferred, optional, or required for public signup.
- If refresh tokens are added, reused rotated tokens revoke the token family and force login.
- If real-time collaboration is added, unauthorized users cannot subscribe to campaign events.
- Shop search returns items matched by description/properties/tags, not only by name.
- Shop tag filter opens from a dedicated button, shows selected tags as chips and can be reset.

## Not in scope

- Public share links for unauthenticated viewers.
- Role-based administration UI.
