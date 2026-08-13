# GymAssistant Authentication

This document describes the authentication and authorization flow currently implemented in GymAssistant.

## Current scope

Implemented authentication currently includes:

- email/password login through ASP.NET Core Identity,
- JWT bearer access tokens,
- `POST /api/auth/login` and protected `GET /api/auth/me`,
- `ICurrentUser` for backend user resolution,
- frontend `AuthProvider`, protected routes and bearer-token handling,
- persisted refresh-token records linked to `ApplicationUser`,
- refresh-token generation and SHA-256 hashing through `IRefreshTokenService`,
- an `HttpOnly` refresh-token cookie created during successful login,
- user ownership enforcement for templates, sessions and private exercises.

Still not implemented: `POST /api/auth/refresh`, refresh-token rotation and server-side logout/revocation. Registration, password reset, external providers and roles remain outside the current MVP scope.

## Login flow

```txt
POST /api/auth/login
    -> UserManager validates email/password
    -> AccessTokenService creates JWT
    -> RefreshTokenService creates a random refresh token
    -> SHA-256 hash is stored in RefreshTokens
    -> raw refresh token is written to an HttpOnly cookie
    -> access JWT is returned to the frontend
```

The frontend stores the access token and its expiration in `localStorage`. The raw refresh token is not returned in JSON and is not stored in application JavaScript state.

The frontend login request uses `credentials: "include"`. Backend CORS allows credentials for the local Vite origin `http://localhost:5173`.

## Refresh-token persistence

`ApplicationUser` has a one-to-many relationship with `RefreshToken`.

`RefreshToken` currently stores:

- `Id`,
- `TokenHash`,
- `CreatedAtUtc`,
- `ExpiresAtUtc`,
- nullable `RevokedAtUtc`,
- `UserId`.

Deleting an application user cascades to their refresh-token records.

`TokenHash` is stored as required SQL Server `binary(32)` with a unique index. SHA-256 produces exactly 32 bytes, so the database stores the binary hash rather than a text representation. Only the hash is persisted; the raw token stays in the browser cookie.

The current refresh-token lifetime is seven days and is still hard-coded.

## Login cookie

Successful login currently writes the raw refresh token with:

```txt
HttpOnly = true
Secure = true
SameSite = Strict
Expires = refresh-token expiration
```

`HttpOnly` keeps the cookie out of frontend JavaScript. `Secure` requires secure transport behavior. `SameSite=Strict` is the current policy and can be revisited if deployment topology requires it.

## Planned refresh flow

The next backend step is `POST /api/auth/refresh`:

```txt
refresh-token cookie
    -> hash raw token
    -> find RefreshToken by TokenHash
    -> reject missing / revoked / expired token
    -> revoke used token
    -> create replacement refresh token
    -> replace cookie
    -> return new access JWT
```

This is refresh-token rotation. After it exists, frontend `401 Unauthorized` handling should attempt refresh once and retry the original request exactly once. If refresh fails, the frontend should clear authentication state and return to the login flow.

## Current frontend authentication flow

1. Login page calls `login(email, password)`.
2. `AuthProvider` calls `/api/auth/login` with `credentials: "include"`.
3. Browser receives the refresh-token cookie.
4. Frontend stores the access token in `localStorage`.
5. `AuthProvider` calls `/api/auth/me`.
6. Successful `/me` sets the authenticated user and status.
7. `ProtectedRoute` controls authenticated navigation.

`fetchApi` currently adds `Authorization: Bearer <access-token>` to protected API calls. It does not yet implement refresh/retry behavior.

Local logout currently clears frontend auth state only. Server-side refresh-token revocation is still pending.

## Domain authorization

Frontend route protection is only a UX mechanism. Backend endpoints remain the security boundary.

Current rules:

- workout templates belong to the authenticated user,
- workout sessions belong to the authenticated user,
- system exercises are globally readable,
- private exercises are scoped by `OwnerUserId`,
- exercise history is built only from the authenticated user's workout sessions,
- ownership identifiers are resolved on the backend rather than trusted from frontend requests.

The current exercise create/edit flow still allows authenticated users to set `isSystem` for development testing. This is temporary and must be replaced with a seed/admin strategy plus database constraints before the feature is production-ready.

## Remaining MVP work

- implement `/api/auth/refresh`,
- rotate refresh tokens,
- implement server-side logout/revocation,
- centralize frontend `401 -> refresh -> retry once`,
- handle concurrent refresh attempts safely,
- move refresh-token lifetime to configuration,
- finalize system/private exercise ownership,
- add authentication and two-user isolation integration tests,
- remove temporary auth/debug behavior.

## Security notes

JWT claims are signed but not secret. The access token is still stored in `localStorage`, so a later hardening pass can evaluate moving it to a less XSS-exposed storage model.

The long-lived refresh credential is kept in an `HttpOnly` cookie, while SQL Server stores only its SHA-256 hash. Raw refresh tokens should not be logged or committed.
