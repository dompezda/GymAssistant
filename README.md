# GymAssistant

GymAssistant is a portfolio project for planning workouts, running workout sessions and reviewing training history.

## Tech stack

- Backend: .NET 9, Minimal API, Entity Framework Core, SQL Server
- Frontend: React, TypeScript, Vite
- Authentication: ASP.NET Core Identity, JWT bearer access tokens and persisted refresh tokens

## Current authentication status

Implemented:

- development user stored through ASP.NET Core Identity,
- JWT access-token generation and validation,
- `POST /api/auth/login`, `POST /api/auth/refresh`, `POST /api/auth/logout` and protected `GET /api/auth/me`,
- `ICurrentUser` abstraction for reading the authenticated user id from JWT claims,
- frontend login, `AuthProvider`, `useAuth()` and protected routes,
- access-token persistence in `localStorage`,
- shared frontend `fetchApi` wrapper that attaches the bearer token to protected API requests,
- centralized `401 Unauthorized -> refresh -> retry once` handling,
- concurrent refresh protection through a shared refresh promise and stale-request token comparison,
- startup/F5 session recovery through `/api/auth/me` plus automatic refresh when necessary,
- separate frontend auth error state so technical failures are not treated as logout,
- `RefreshToken` persistence linked to `ApplicationUser` with cascade delete,
- SHA-256 refresh-token hashes stored as `binary(32)` with a unique index,
- cryptographically random refresh-token generation and hashing through `IRefreshTokenService`,
- refresh-token creation during successful login,
- refresh-token validation, rotation and revocation,
- raw refresh token stored in an `HttpOnly`, `Secure`, `SameSite=Strict` cookie while only its hash is stored in SQL Server,
- server-side logout that revokes the active refresh token and deletes the cookie,
- credential-enabled CORS for the local React origin and `credentials: "include"` where cookie transport is required,
- authorization and user-data isolation for workout templates,
- authorization and user-data isolation for workout sessions,
- exercise visibility rules: system exercises are global and private exercises are scoped to their owner,
- exercise history filtered through the authenticated user's workout sessions,
- protected muscle-group endpoint.

Still in progress / planned hardening:

- move refresh-token lifetime and related auth values to configuration,
- centralize frontend API base URL/configuration,
- final production-safe system-exercise creation/editing flow,
- database constraints for valid `IsSystem` / `OwnerUserId` combinations,
- reset or migration of old development data,
- authentication and two-user isolation integration tests,
- cleanup of temporary authentication/debug code,
- explicit decision on production access-token storage instead of treating `localStorage` as final by default,
- optional hardening for the edge case where explicit logout cannot reach the backend.

See [Authentication.md](Authentication.md) for the current authentication and authorization flow, [Api.md](Api.md) for the HTTP API and [todo.md](todo.md) for the implementation checklist.
