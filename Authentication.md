# GymAssistant Authentication

This document describes the authentication and authorization flow currently implemented in GymAssistant.

## Current scope

The authentication MVP currently uses:

- email and password credentials,
- ASP.NET Core Identity for user storage and password verification,
- JWT bearer access tokens,
- a seeded development user,
- browser `localStorage` for temporary access-token persistence,
- `AuthProvider` and `AuthContext` for frontend authentication state,
- `useAuth()` for safe authentication-context access,
- `ProtectedRoute` for authenticated frontend routes,
- `GET /api/auth/me` to validate and restore the frontend session,
- `ICurrentUser` / `CurrentUser` to expose the authenticated `UserId` from JWT claims on the backend,
- `fetchApi` to attach bearer tokens to protected frontend API requests,
- authorization and ownership filtering for workout templates, workout sessions and exercises,
- globally readable system exercises plus user-scoped private exercises,
- exercise history scoped through the authenticated user's workout sessions.

Registration, refresh tokens, external providers, password reset and roles are outside the current MVP scope.

## Backend authentication flow

The backend separates credential verification, token creation and token validation:

```txt
login request
    -> ASP.NET Core Identity / UserManager
    -> credentials valid
    -> AccessTokenService creates JWT
    -> frontend stores access token

protected API request
    -> Authorization: Bearer <access-token>
    -> JWT bearer authentication validates token
    -> claims are exposed through HttpContext.User
    -> CurrentUser reads the sub claim as UserId
    -> authorized endpoint uses UserId for ownership filtering
```

`ICurrentUser` is registered as a scoped service. It uses `IHttpContextAccessor` and reads the `sub` claim from the already validated `HttpContext.User`. Domain endpoints do not parse the raw JWT themselves.

## Authentication endpoints

### POST `/api/auth/login`

Authenticates a user and returns a JWT access token.

Request:

```json
{
  "email": "user@example.com",
  "password": "development-password"
}
```

Successful response:

```json
{
  "accessToken": "jwt-access-token",
  "expiresAtUtc": "2026-08-09T14:30:00Z"
}
```

The endpoint returns:

- `400 Bad Request` when email or password is empty,
- `401 Unauthorized` when the credentials are invalid.

The response intentionally does not reveal whether the email or password was incorrect.

### GET `/api/auth/me`

Returns the currently authenticated user.

Request header:

```http
Authorization: Bearer <access-token>
```

Successful response:

```json
{
  "id": "6e40dbd4-6f8f-43bc-9522-a041fbe9138d",
  "email": "user@example.com"
}
```

The endpoint is protected and uses `ICurrentUser.UserId` plus `UserManager<ApplicationUser>` to load the current Identity user.

## Domain authorization

Frontend route protection is only a UX/navigation mechanism. Real access control is enforced by the backend.

### Workout templates

All `/api/workout-templates` endpoints require authorization.

Current rules:

- new templates receive `UserId` from `ICurrentUser`; ownership is never accepted from the frontend,
- template lists return only records owned by the authenticated user,
- template detail and update queries require both the requested template id and the authenticated `UserId`,
- a template may reference system exercises or private exercises owned by the authenticated user,
- a private exercise owned by another user cannot be added to a template.

### Workout sessions

All `/api/workout-sessions` endpoints require authorization.

Current rules:

- new sessions receive `UserId` from `ICurrentUser`,
- finished-session lists return only the authenticated user's sessions,
- session detail, set updates, finish, summary, exercise deletion and exercise addition first resolve a session owned by the authenticated user,
- when a session is created from a workout template, that template must belong to the authenticated user,
- exercises used to create or extend a session must be system exercises or private exercises owned by the authenticated user,
- `WorkoutSessionExercise` uses `ExerciseNameSnapshot`, so historical session reads do not depend on the current exercise name.

A `WorkoutSessionExercise` is treated as part of its owning workout-session aggregate. Once the session itself is verified as belonging to the authenticated user, nested session exercises and sets are accessed through that owned session instead of re-checking the current owner of the source `Exercise` record.

### Exercises

All `/api/exercises` endpoints require authorization.

Current rules:

- exercise lists return `IsSystem == true` exercises plus private exercises where `OwnerUserId == currentUser.UserId`,
- an exercise detail/history lookup succeeds only for a system exercise or a private exercise owned by the authenticated user,
- exercise history is built only from workout sessions where `WorkoutSession.UserId == currentUser.UserId`,
- workout templates and workout sessions may only reference system exercises or the authenticated user's private exercises.

During development, create/edit requests still contain `isSystem` so both ownership modes can be tested from the UI. The backend currently enforces this runtime invariant:

```txt
IsSystem = true  -> OwnerUserId = null
IsSystem = false -> OwnerUserId = currentUser.UserId
```

This is intentionally temporary. In the production-ready model, a normal user should not be able to create a global system exercise or convert an existing system exercise into a private one. System exercises should eventually come from a seed or a dedicated privileged/admin flow, and the database should enforce the system/private ownership invariant.

### Muscle groups

`GET /api/muscle-groups` also requires authorization for consistency with the authenticated application API. It returns enum metadata and does not contain user-owned data.

## Frontend authentication flow

### Login

1. The login page calls `login(email, password)` from `useAuth()`.
2. `AuthProvider` sends the credentials to `POST /api/auth/login`.
3. After a successful response, the frontend stores `accessToken` and `expiresAtUtc` in `localStorage`.
4. `AuthProvider` calls `GET /api/auth/me` with the new bearer token.
5. After `/api/auth/me` succeeds, the context stores the authenticated user and changes status to `authenticated`.
6. The login page navigates to `/main-menu`.
7. Invalid credentials are displayed as a generic login error.

### Session restoration

`AuthProvider` starts with the `loading` status and checks the current session when the application mounts.

```txt
application start
    -> read accessToken from localStorage
    -> no token: unauthenticated
    -> token exists: GET /api/auth/me
        -> 200: authenticated + current user
        -> non-success response: clear stored token + unauthenticated
```

The authentication context exposes the current status, user, login and logout operations.

### Protected routes

Authenticated application pages are nested under `ProtectedRoute`.

- `loading` renders a temporary loading state,
- `unauthenticated` redirects to `/login`,
- `authenticated` renders the nested route through `Outlet`.

This protects frontend navigation only. Backend authorization remains mandatory independently.

### Shared API wrapper

Protected domain requests use `frontend/src/api/fetchApi.ts`.

The wrapper:

1. reads `accessToken` from `localStorage`,
2. creates `Headers` from the caller's existing `RequestInit.headers`,
3. adds `Authorization: Bearer <token>` when a token exists,
4. forwards the original request options to `fetch`.

Conceptually:

```ts
const headers = new Headers(options.headers);

if (token) {
  headers.set("Authorization", `Bearer ${token}`);
}

return fetch(url, { ...options, headers });
```

The wrapper deliberately does not yet parse response bodies, force JSON content types or implement refresh-token behavior.

### Logout

Local logout currently:

- removes `accessToken` and `expiresAtUtc` from `localStorage`,
- clears the current user,
- changes authentication status to `unauthenticated`.

There is no server-side logout or refresh-token revocation in the current MVP.

## Remaining MVP work

- centralize `401 Unauthorized` handling for domain requests so an expired/stale token clears frontend auth state and returns the user to the existing login flow,
- redirect authenticated users away from `/login` when appropriate,
- remove temporary authentication debug logging,
- replace the temporary user-controlled `isSystem` exercise flow with a seed/admin strategy,
- add a database constraint for valid `IsSystem` / `OwnerUserId` combinations,
- reset or migrate old development data created before ownership was fully enforced,
- add backend integration tests for authentication, protected endpoints and two-user isolation,
- run explicit two-user acceptance tests for templates, sessions, exercises and exercise history.

## Security notes

The frontend route guard and `AuthContext` improve navigation and state management, but neither is an API security boundary. A stale frontend state never grants backend access.

JWT payload data is readable by the client; token integrity comes from signature validation, not payload secrecy. Secrets, passwords and connection strings must never be placed in JWT claims or committed to the repository.

Storing an access token in `localStorage` is acceptable for the current learning MVP, but it increases exposure to token theft through cross-site scripting. A later version should evaluate short-lived access tokens with refresh-token rotation and an `HttpOnly` cookie.

Authentication also does not replace rate limiting. Requiring a valid JWT blocks anonymous access to protected domain endpoints, but an authenticated client can still send excessive requests. Rate limiting is a separate concern for a later hardening pass.
