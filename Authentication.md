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
- authenticated, user-owned workout-template endpoints.

Registration, refresh tokens, external providers, password reset and roles are outside the current MVP scope.

## Backend

### Authentication pipeline

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

### POST `/api/auth/login`

Authenticates a user and returns an access token.

#### Request

```json
{
  "email": "user@example.com",
  "password": "development-password"
}
```

#### Response `200 OK`

```json
{
  "accessToken": "jwt-access-token",
  "expiresAtUtc": "2026-08-02T14:30:00Z"
}
```

The endpoint returns:

- `400 Bad Request` when email or password is empty,
- `401 Unauthorized` when the credentials are invalid.

The response intentionally does not reveal whether the email or password was incorrect.

### GET `/api/auth/me`

Returns the currently authenticated user.

#### Request header

```http
Authorization: Bearer <access-token>
```

#### Response `200 OK`

```json
{
  "id": "6e40dbd4-6f8f-43bc-9522-a041fbe9138d",
  "email": "user@example.com"
}
```

The endpoint is protected and returns `401 Unauthorized` when the token is missing, invalid or expired.

The endpoint uses `ICurrentUser.UserId` to identify the authenticated user and `UserManager<ApplicationUser>` to load the current Identity user.

### Workout-template authorization

All `/api/workout-templates` endpoints require authorization.

Ownership rules currently enforced for workout templates:

- new templates receive `UserId` from `ICurrentUser`; ownership is never accepted from the frontend,
- template lists are filtered by the authenticated `UserId`,
- template detail and update queries require both the requested template id and the authenticated `UserId`,
- templates may reference system exercises or exercises owned by the authenticated user,
- private exercises owned by another user cannot be added to a template.

Workout-session and exercise endpoints still require equivalent ownership protection before the authentication MVP is complete.

## Frontend flow

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

The authentication context exposes:

```ts
type AuthStatus = "loading" | "authenticated" | "unauthenticated";

type AuthUser = {
  id: string;
  email: string;
};

type AuthContextValue = {
  status: AuthStatus;
  user: AuthUser | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
};
```

`AuthContext` represents the frontend's current view of the authentication state. It is not an API security boundary and can temporarily become stale, for example when an access token expires while the application is open.

### Protected routes

Authenticated application pages are nested under `ProtectedRoute`.

- `loading` renders a temporary loading state,
- `unauthenticated` redirects to `/login`,
- `authenticated` renders the nested route through `Outlet`.

This protects frontend navigation only. Backend authorization is still required independently.

### Logout

Local logout currently:

- removes `accessToken` and `expiresAtUtc` from `localStorage`,
- clears the current user,
- changes authentication status to `unauthenticated`.

There is no server-side logout or refresh-token revocation in the current MVP.

## Remaining MVP work

- Add a shared HTTP client or request wrapper that attaches the bearer token to protected API requests.
- Handle `401 Unauthorized` from domain API requests by clearing stale authentication state and redirecting through the existing auth flow.
- Redirect authenticated users away from `/login` when appropriate.
- Remove temporary authentication debug code.
- Protect workout-session endpoints and filter all session data by the authenticated user.
- Protect exercise endpoints, expose system exercises plus the authenticated user's exercises, and prevent users from creating or modifying system ownership flags.
- Filter exercise history through the authenticated user's workout sessions.
- Add backend integration tests for authentication and two-user data isolation.

## Security notes

The frontend route guard improves navigation and user experience, but it is not an API security boundary. A stale `AuthContext` value does not grant backend access. Protected backend requests must contain a valid bearer token and user-owned queries must filter using the authenticated `UserId`.

JWT payload data is readable by the client; token integrity comes from signature validation, not payload secrecy. Secrets, passwords and connection strings must never be placed in JWT claims.

Storing an access token in `localStorage` is acceptable for the current learning MVP, but it increases exposure to token theft through cross-site scripting. A later version should evaluate short-lived access tokens with refresh-token rotation and an `HttpOnly` cookie.