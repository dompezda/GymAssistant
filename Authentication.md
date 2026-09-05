# GymAssistant Authentication

This document describes the authentication and authorization flow currently implemented in GymAssistant.

## Current scope

Implemented authentication currently includes:

- public email/password registration through ASP.NET Core Identity,
- email/password login through ASP.NET Core Identity,
- unique email enforcement and a configured Identity password policy,
- JWT bearer access tokens,
- `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/refresh`, `POST /api/auth/logout` and protected `GET /api/auth/me`,
- `ICurrentUser` for backend user resolution,
- frontend `/register` and `/login` flows, `AuthProvider`, `useAuth()`, protected routes and centralized bearer-token handling,
- automatic login after successful frontend registration,
- persisted refresh-token records linked to `ApplicationUser`,
- cryptographically random refresh-token generation and SHA-256 hashing through `IRefreshTokenService`,
- an `HttpOnly` refresh-token cookie created during login and replaced during token rotation,
- refresh-token validation, rotation and revocation,
- refresh-token lifetime configured through `RefreshTokenOptions`,
- centralized frontend API base URL through `VITE_API_URL` and `getApiUrl()`,
- centralized frontend `401 -> refresh -> retry once` behavior,
- shared frontend refresh work so concurrent `401` responses do not start duplicate refresh requests,
- session recovery on application startup when the access token is missing or stale but the refresh cookie is still valid,
- server-side logout that revokes the active refresh token and removes its cookie,
- separate frontend authentication states for loading, authenticated, unauthenticated and technical-error cases,
- user ownership enforcement for templates, sessions and private exercises,
- system/private exercise ownership enforced by both API rules and a database check constraint,
- backend integration-test infrastructure based on `WebApplicationFactory` and SQLite in-memory, including unauthenticated exercise access and two-user private-exercise visibility coverage.

Password reset, email confirmation, external providers and roles remain outside the current MVP scope.

## Registration flow

`POST /api/auth/register` is public and creates a new `ApplicationUser` through ASP.NET Core Identity.

The request contains email and password only. The backend creates the Identity user with:

```txt
UserName = request.Email
Email = request.Email
```

The current application therefore treats email as the user-facing login identifier while Identity `UserName` is used only as the matching technical username value.

Identity configuration currently requires:

```txt
RequireUniqueEmail = true
RequiredLength = 8
RequireDigit = true
RequireLowercase = true
RequireUppercase = true
RequireNonAlphanumeric = true
```

`UserManager.CreateAsync()` remains the source of truth for password and email validation. The endpoint does not hash or validate password complexity manually.

Registration flow:

```txt
POST /api/auth/register
    -> reject missing email/password
    -> create ApplicationUser with UserName = Email
    -> UserManager.CreateAsync validates email, uniqueness and password policy
    -> success: return 200 OK
    -> failure: return Identity validation descriptions
```

Because the same value is stored as both `UserName` and `Email`, a duplicate account can produce both `DuplicateUserName` and `DuplicateEmail` inside Identity. The endpoint filters the technical `DuplicateUserName` result and exposes the email-related error instead.

The frontend `/register` page validates that all fields are present and that password/confirmation match before sending the request. Identity validation errors returned by the backend are displayed to the user.

After a successful registration, the frontend calls the existing `login(email, password)` function from `AuthProvider` and navigates to `/main-menu`. The registration endpoint itself does not create JWT or refresh tokens; token creation remains centralized in the normal login flow.

The login and registration pages provide navigation links between `/login` and `/register`.

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

The frontend login request uses `credentials: "include"`. Backend CORS allows credentials for the configured frontend origin read from `Frontend:Origin`; the current development value is `http://localhost:5173`.

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

The refresh-token lifetime is configured through the `RefreshToken:LifetimeDays` setting and bound to `RefreshTokenOptions`. The current development value is seven days. Configuration is validated on application startup and must be between 1 and 90 days.

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

## Frontend API configuration

The frontend API base URL is read from Vite configuration through `VITE_API_URL`. `frontend/src/config/apiConfig.ts` exposes `getApiUrl()`, which fails fast when the value is missing.

Protected domain calls pass relative paths such as `/api/exercises` or `/api/workout-sessions/...` to `fetchApi`; the wrapper combines those paths with the configured API base URL. Registration, login, logout and refresh requests also use the same centralized base URL instead of hard-coded backend addresses.

Because `VITE_` variables are exposed to browser code, `VITE_API_URL` must contain only public configuration and must not contain secrets, tokens or connection strings.

## Frontend request and retry flow

Protected domain requests use `frontend/src/api/fetchApi.ts`.

The wrapper:

1. combines the configured API base URL with the relative endpoint path,
2. reads the current access token from `localStorage`,
3. attaches `Authorization: Bearer <token>` when available,
4. returns successful/non-401 responses normally,
5. on `401`, first checks whether another request has already replaced the access token,
6. if a newer access token already exists, retries the original request once with that token,
7. otherwise waits for a shared `refreshPromise`, creating one only when no refresh is already running,
8. `refreshAccessToken()` calls `/api/auth/refresh` with `credentials: "include"`,
9. a successful refresh stores the replacement access token and expiration,
10. the original API request is retried once with the replacement bearer token.

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

Deleting a refresh-token cookie manually in the browser does not notify the backend. The corresponding database record therefore remains non-revoked until it expires; this is expected in the current multi-session model because the server stores only the token hash and revocation happens through refresh/logout requests that present the raw token.

## Domain authorization

Frontend route protection is only a UX mechanism. Backend endpoints remain the security boundary.

Current rules:

- workout templates belong to the authenticated user,
- workout sessions belong to the authenticated user,
- system exercises are globally readable and have `OwnerUserId = null`,
- private exercises are scoped by `OwnerUserId`,
- ordinary authenticated users create exercises only as private records with `OwnerUserId = currentUser.UserId` and `IsSystem = false`,
- ordinary authenticated users can edit only their own private exercises; system exercises cannot be modified through the normal user PATCH endpoint,
- create/edit request DTOs do not accept `IsSystem`, so ownership mode is not trusted from the frontend,
- exercise history is built only from the authenticated user's workout sessions,
- ownership identifiers are resolved on the backend rather than trusted from frontend requests.

The database also enforces the exercise ownership invariant through `CK_Exercises_SystemOwnership`:

```txt
(IsSystem = 1 AND OwnerUserId IS NULL)
OR
(IsSystem = 0 AND OwnerUserId IS NOT NULL)
```

This prevents invalid system/private ownership combinations even if an application-level bug attempts to persist one.

A dedicated production-safe seed/admin strategy for creating and maintaining system exercises is still planned. Normal authenticated users are intentionally not allowed to create or edit system exercises.

## Integration-test coverage

Backend integration tests use `WebApplicationFactory<Program>` and the `Testing` environment. The test host removes the production `AppDbContext` provider configuration and registers SQLite in-memory instead, keeping one SQLite connection open for the lifetime of the factory and creating the schema with `Database.EnsureCreated()`.

Current exercise integration coverage includes:

- unauthenticated `GET /api/exercises` returns `401 Unauthorized`,
- a two-user isolation scenario creates two Identity users and one private exercise for each user, issues a real access JWT for the first user through `IAccessTokenService`, calls the protected endpoint through `HttpClient`, verifies that the first user's exercise is returned and verifies that the second user's exercise is not returned.

The test deliberately bypasses the login endpoint when preparing the authenticated exercise request so the exercise-authorization test does not depend on the correctness of the separate login flow. Authentication endpoint integration coverage is still planned separately.

The current `IClassFixture<GymAssistantWebApplicationFactory>` setup keeps the SQLite database alive across tests using the same factory. As the suite grows, test-data reset/isolation must be made explicit so tests do not become order-dependent or leak state into one another.

## Remaining MVP / hardening work

- define the final seed/admin strategy for the system exercise catalog,
- expand integration-test coverage to authentication flows and additional two-user ownership boundaries,
- add an explicit integration-test data reset/isolation strategy as the suite grows,
- remove temporary auth/debug behavior,
- decide whether the access token should remain in `localStorage` for the production version,
- optionally harden explicit logout behavior for the backend-unavailable edge case.

## Security notes

JWT claims are signed but not secret. The access token is currently stored in `localStorage`, so an XSS vulnerability could expose it; a later hardening pass should explicitly evaluate the desired storage model.

The long-lived refresh credential is kept in an `HttpOnly` cookie, while SQL Server stores only its SHA-256 hash. Raw refresh tokens should not be logged or committed.

Refresh-token rotation limits reuse of an already-used token, while server-side revocation makes normal logout invalidate the active persisted refresh credential.
