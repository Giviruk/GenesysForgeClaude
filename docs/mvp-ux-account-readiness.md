# MVP UX and Account Readiness

Этот документ закрывает пункт MVP-review про известные UX/account ограничения: refresh/session rotation, password reset/email confirmation и URL deep links. Он не описывает новую игровую функциональность.

## Current state

- Auth: `POST /api/auth/register` and `POST /api/auth/login` return a JWT. Frontend stores it in `localStorage`.
- Session expiry: API client clears the token and returns the user to auth screen on `401` when a token was present.
- Recovery: self-service password reset and email confirmation are not implemented.
- Routing: top-level navigation and selected entity state live in React state. There is no URL route for a character, campaign, NPC, encounter or content pack.
- Sharing: character sheets are not shareable by URL. Campaign access is through join code plus owned character membership.

## MVP decision

Private MVP can launch with the current auth model if the release notes state these limits:

- expired session requires signing in again;
- lost password is handled manually by an operator or by recreating an account;
- browser refresh returns to the top-level area instead of restoring the selected sheet/view;
- links to a specific character/campaign/NPC are not supported.

Public MVP should not launch with only manual recovery. It needs at least email-based password reset or a documented support workflow with identity verification.

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

## Acceptance checklist

- Refreshing `/characters/:id` reopens the same character after login.
- Refreshing `/campaigns/:id` reopens the same campaign if the user has access.
- Opening a forbidden/deleted entity URL shows a clear error and a link back to the list.
- `401` clears the token, shows a session-expired message and keeps the intended route.
- Private MVP docs describe manual recovery limits.
- Public MVP either has password reset or a documented support recovery process.

## Not in scope

- Refresh tokens and rotation.
- OAuth/social login.
- Real-time collaboration.
- Public share links for unauthenticated viewers.
- Role-based administration UI.
