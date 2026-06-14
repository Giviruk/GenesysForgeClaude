# API Conventions

This file is kept as a compatibility note from the earlier documentation set.

Primary current API documentation: [api.md](api.md).

Use [api.md](api.md) as the source of truth for:

- discovered endpoints;
- methods and routes;
- request/response DTOs;
- auth requirements;
- error handling;
- current limitations.

High-level conventions still apply:

- API routes start with `/api`.
- DTO names use `*Request`, `*Response`, `*Dto`, `*ListItemDto`.
- Do not return EF entities directly.
- Known errors use `{ "message": "..." }`.
- JSON enum values are camelCase.
- Route enum parsing should stay case-insensitive.

