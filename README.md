# GymAssistant

GymAssistant is a portfolio project for planning workouts, running workout sessions and reviewing training history.

## Tech stack

- Backend: .NET 9, Minimal API, Entity Framework Core, SQL Server
- Frontend: React, TypeScript, Vite
- Authentication: ASP.NET Core Identity, JWT bearer access tokens and persisted refresh tokens
- API documentation/testing: ASP.NET Core OpenAPI with Scalar in Development

## Current authentication status

Implemented:

- development user stored through ASP.NET Core Identity,
- public email/password registration through `POST /api/auth/register`,
- Identity-enforced unique email addresses and password policy,
- registration creates an `ApplicationUser` with email used as both `Email` and the technical Identity `UserName`,
- frontend `/register` page with email/password confirmation, backend validation error display, Login/Register navigation and automatic login after successful registration,
- JWT access-token generation and validation,
- `POST /api/auth/login`, `POST /api/auth/refresh`, `POST /api/auth/logout` and protected `GET /api/auth/me`,
- `ICurrentUser` abstraction for reading the authenticated user id from JWT claims,
- frontend login, `AuthProvider`, `useAuth()` and protected routes,
- access-token persistence in `localStorage`,
- shared frontend `fetchApi` wrapper that attaches the bearer token to protected API requests,
- centralized frontend API base URL through `VITE_API_URL` and `getApiUrl()`, with `fetchApi` receiving relative `/api/...` paths,
- centralized `401 Unauthorized -> refresh -> retry once` handling,
- concurrent refresh protection through a shared refresh promise and stale-request token comparison,
- startup/F5 session recovery through `/api/auth/me` plus automatic refresh when necessary,
- separate frontend auth error state so technical failures are not treated as logout,
- `RefreshToken` persistence linked to `ApplicationUser` with cascade delete,
- SHA-256 refresh-token hashes stored as `binary(32)` with a unique index,
- cryptographically random refresh-token generation and hashing through `IRefreshTokenService`,
- refresh-token creation during successful login,
- refresh-token validation, rotation and revocation,
- refresh-token lifetime configured through `RefreshTokenOptions` / `RefreshToken:LifetimeDays` with startup validation,
- raw refresh token stored in an `HttpOnly`, `Secure`, `SameSite=Strict` cookie while only its hash is stored in SQL Server,
- server-side logout that revokes the active refresh token and deletes the cookie,
- credential-enabled CORS with the allowed frontend origin read from `Frontend:Origin`,
- development-only OpenAPI/Scalar API reference for manual endpoint testing,
- authorization and user-data isolation for workout templates,
- authorization and user-data isolation for workout sessions,
- exercise visibility rules: system exercises are global and private exercises are scoped to their owner,
- ordinary users can create and edit only their own private exercises; `IsSystem` is no longer accepted from create/edit requests,
- database check constraint enforcing valid `IsSystem` / `OwnerUserId` combinations,
- system exercises are marked with a `System` badge in the exercise picker and main exercise list,
- system exercise details use a read-only frontend mode: the name and muscle groups cannot be changed and the edit action is not rendered,
- exercise history filtered through the authenticated user's workout sessions,
- protected muscle-group endpoint.

Still in progress / planned hardening:

- final seed/admin strategy for creating and maintaining the system exercise catalog,
- reset of development/demo data before the production-ready release,
- authentication and two-user isolation integration tests,
- cleanup of temporary authentication/debug code,
- explicit decision on production access-token storage instead of treating `localStorage` as final by default,
- optional hardening for the edge case where explicit logout cannot reach the backend.

See [Authentication.md](Authentication.md) for the current authentication and authorization flow, [Api.md](Api.md) for the HTTP API and [todo.md](todo.md) for the implementation checklist.
