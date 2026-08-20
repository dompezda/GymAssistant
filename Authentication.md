# GymAssistant Authentication

This document describes the authentication and authorization flow currently implemented in GymAssistant.

## Current scope

Implemented authentication currently includes:

- email/password login through ASP.NET Core Identity,
- JWT bearer access tokens,
- `POST /api/auth/login`, `POST /api/auth/refresh`, `POST /api/auth/logout` and protected `GET /api/auth/me`,
- `ICurrentUser` for backend user resolution,
- frontend `AuthProvider`, `useAuth()`, protected routes and centralized bearer-token handling,
- persisted refresh-token records linked to `ApplicationUser`,
- cryptographically random refresh-token generation and SHA-256 hashing through `IRefreshTokenService`,
- an `HttpOnly` refresh-token cookie created during login and replaced during token rotation,
- refresh-token validation, rotation and revocation,
- centralized frontend `401 -> refresh -> retry once` behavior,
- shared frontend refresh work so concurrent `401` responses do not start duplicate refresh requests,
- session recovery on application startup when the access token is missing or stale but the refresh cookie is still valid,
- server-side logout that revokes the active refresh token and removes its cookie,
- separate frontend authentication states for loading, authenticated, unauthenticated and technical-error cases,
- user ownership enforcement for templates, sessions and private exercises.

Registration, password reset, external providers and roles remain outside the current MVP scope.

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

The frontend stores the access token and its expiration in `localStorage`. The raw refresh token is not returned in JSON and is not accessible through application JavaScript.

The frontend login request uses `credentials: "include"`. Backend CORS allows credentials for the local Vite origin `http://localhost:5173`.

## Refresh-token persistence

`ApplicationUser` has a one-to-many relationship with `RefreshToken`.

`RefreshToken` stores:

- `Id`,
- `TokenHash`,
- `CreatedAtUtc`,
- `ExpiresAtUtc`,
- nullable `RevokedAtUtc`,
- `UserId`.

Deleting an application user cascades to their refresh-token records.

`TokenHash` is stored as required SQL Server `binary(32)` with a unique index. SHA-256 produces exactly 32 bytes, so the database stores the binary hash rather than a text representation. Only the hash is persisted; the raw token stays in the browser cookie.

The current refresh-token lifetime is seven days and is still hard-coded.

## Refresh-token cookie

Login and successful refresh currently write the raw refresh token with:

```txt
HttpOnly = true
Secure = true
SameSite = Strict
Expires = refresh-token expiration
```

`HttpOnly` keeps the cookie out of frontend JavaScript. `Secure` requires secure transport behavior. `SameSite=Strict` is the current policy and can be revisited if deployment topology requires it.

## Refresh flow

`POST /api/auth/refresh` does not require a valid access JWT. It authenticates the refresh operation through the refresh-token cookie.

```txt
refresh-token cookie
    -> hash raw token
    -> find RefreshToken by TokenHash
    -> reject missing / revoked / expired token
    -> resolve ApplicationUser
    -> revoke used refresh token
    -> create and persist replacement refresh-token hash
    -> replace HttpOnly cookie with the new raw refresh token
    -> return new access JWT
```

This implements refresh-token rotation: a successfully used refresh token cannot be used again because its `RevokedAtUtc` is set before the replacement token becomes the active credential.

The refresh endpoint returns `401 Unauthorized` when the cookie is missing/empty, the token cannot be found, the token is expired or revoked, or its user cannot be resolved.

## Frontend request and retry flow

Protected domain requests use `frontend/src/api/fetchApi.ts`.

The wrapper:

1. reads the current access token from `localStorage`,
2. attaches `Authorization: Bearer <token>` when available,
3. returns successful/non-401 responses normally,
4. on `401`, first checks whether another request has already replaced the access token,
5. if a newer access token already exists, retries the original request once with that token,
6. otherwise waits for a shared `refreshPromise`, creating one only when no refresh is already running,
7. `refreshAccessToken()` calls `/api/auth/refresh` with `credentials: "include"`,
8. a successful refresh stores the replacement access token and expiration,
9. the original API request is retried once with the replacement bearer token.

The refresh request deliberately uses native `fetch`, not `fetchApi`, so a failed `/refresh` response cannot recursively trigger another refresh attempt.

When refresh fails, `refreshAccessToken()` removes the stale access-token data and returns `null`. The original unauthorized response is then returned to the caller rather than entering an infinite retry loop.

### Concurrent 401 handling

A module-level `refreshPromise` is shared by all `fetchApi` calls. If several requests receive `401` at nearly the same time, they wait for the same refresh operation instead of rotating the same refresh token independently.

There is also a second race-condition guard: after receiving `401`, a request compares the token it originally used with the token currently stored in `localStorage`. If another request already completed refresh, the late request simply retries with the newer token instead of starting an unnecessary second refresh.

## Startup session recovery

`AuthProvider` starts in the `loading` state and calls protected `/api/auth/me` through `fetchApi`.

This means the provider does not assume that a missing access token means the session is gone:

```txt
application startup / F5
    -> fetchApi calls /api/auth/me
    -> valid JWT: /me succeeds directly
    -> missing/stale JWT: /me returns 401
        -> fetchApi attempts /api/auth/refresh
        -> valid refresh cookie: new JWT + retry /me
        -> invalid/missing refresh cookie: final 401
```

A final `401` sets the auth state to `unauthenticated`. Other HTTP failures and network failures use the separate `error` state instead of incorrectly treating a backend outage as a logout.

`ProtectedRoute` therefore distinguishes:

- `loading` — authentication is still being checked,
- `authenticated` — protected content can be rendered,
- `unauthenticated` — redirect to `/login`,
- `error` — show an authentication/server error instead of redirecting to login.

## Logout flow

`POST /api/auth/logout` is intentionally idempotent and does not require a valid access JWT.

```txt
POST /api/auth/logout
    -> read refresh-token cookie when present
    -> hash it
    -> find matching non-revoked RefreshToken
    -> set RevokedAtUtc when found
    -> delete refresh-token cookie
    -> return 204 No Content
```

Missing cookies or missing/already-revoked database records still result in `204 No Content`: the requested end state is already satisfied.

Frontend `logout()` calls this endpoint with `credentials: "include"` and clears the local access token, user, auth error and auth status in a `finally` block so local logout still happens if the network request itself fails.

A current edge case remains: if the backend cannot be reached during explicit logout, the frontend is cleared locally but the server-side refresh token and HttpOnly cookie may remain valid because the revoke request never reached the API. This can be hardened later if required.

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

## Remaining MVP / hardening work

- move refresh-token lifetime and related auth values to configuration,
- finalize system/private exercise ownership and remove the temporary normal-user system-exercise flow,
- add database constraints for valid `IsSystem` / `OwnerUserId` combinations,
- add authentication and two-user isolation integration tests,
- remove temporary auth/debug behavior,
- centralize frontend API base URL/configuration,
- decide whether the access token should remain in `localStorage` for the production version,
- optionally harden explicit logout behavior for the backend-unavailable edge case.

## Security notes

JWT claims are signed but not secret. The access token is currently stored in `localStorage`, so an XSS vulnerability could expose it; a later hardening pass should explicitly evaluate the desired storage model.

The long-lived refresh credential is kept in an `HttpOnly` cookie, while SQL Server stores only its SHA-256 hash. Raw refresh tokens should not be logged or committed.

Refresh-token rotation limits reuse of an already-used token, while server-side revocation makes normal logout invalidate the active persisted refresh credential.
