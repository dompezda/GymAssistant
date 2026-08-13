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
- `POST /api/auth/login` and protected `GET /api/auth/me`,
- `ICurrentUser` abstraction for reading the authenticated user id from JWT claims,
- frontend login, `AuthProvider`, `useAuth()`, protected routes and local logout,
- access-token persistence in `localStorage`,
- shared frontend `fetchApi` wrapper that attaches the bearer token to protected API requests,
- `RefreshToken` persistence linked to `ApplicationUser` with cascade delete,
- SHA-256 refresh-token hashes stored as `binary(32)` with a unique index,
- cryptographically random refresh-token generation and hashing through `IRefreshTokenService`,
- refresh-token creation during successful login,
- raw refresh token stored in an `HttpOnly`, `Secure`, `SameSite=Strict` cookie while only its hash is stored in SQL Server,
- credential-enabled CORS for the local React origin and `credentials: "include"` on the frontend login request,
- authorization and user-data isolation for workout templates,
- authorization and user-data isolation for workout sessions,
- exercise visibility rules: system exercises are global and private exercises are scoped to their owner,
- exercise history filtered through the authenticated user's workout sessions,
- protected muscle-group endpoint.

Still in progress:

- `POST /api/auth/refresh` with refresh-token validation and rotation,
- server-side logout with refresh-token revocation,
- centralized `401 Unauthorized` handling that refreshes the access token and retries the original request once,
- final production-safe system-exercise creation/editing flow,
- database constraints for valid `IsSystem` / `OwnerUserId` combinations,
- reset or migration of old development data,
- authentication and two-user isolation integration tests,
- cleanup of temporary authentication/debug code.

See [Authentication.md](Authentication.md) for the current authentication and authorization flow, [Api.md](Api.md) for the HTTP API and [todo.md](todo.md) for the implementation checklist.
