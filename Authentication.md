# GymAssistant Authentication

This document describes the authentication flow currently implemented in GymAssistant.

## Current scope

The authentication MVP currently uses:

- email and password credentials,
- ASP.NET Core Identity for user storage and password verification,
- JWT bearer access tokens,
- a seeded development user,
- browser `localStorage` for temporary access-token persistence,
- `GET /api/auth/me` to validate and restore the frontend session.

Registration, refresh tokens, external providers, password reset and roles are outside the current MVP scope.

## Backend

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

## Frontend flow

### Login

1. The login page sends the email and password to `POST /api/auth/login`.
2. After a successful response, the frontend stores `accessToken` and `expiresAtUtc` in `localStorage`.
3. The user is navigated to `/main-menu`.
4. Invalid credentials are displayed as a generic login error.

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

The authentication context currently exposes:

```ts
type AuthStatus = "loading" | "authenticated" | "unauthenticated";

type AuthUser = {
  id: string;
  email: string;
};
```

## Remaining MVP work

- Add a `useAuth()` hook for safe context access.
- Add `ProtectedRoute` and redirect unauthenticated users to `/login`.
- Redirect authenticated users away from `/login` when appropriate.
- Add a shared HTTP client or request wrapper that attaches the bearer token.
- Add local logout and clear authentication state.
- Remove temporary authentication debug code.
- Protect and filter domain endpoints by the authenticated user.
- Add backend integration tests for authentication and user-data isolation.

## Security notes

The frontend route guard will improve navigation and user experience, but it is not an API security boundary. Every user-owned backend endpoint must still require authorization and filter data using the authenticated user identifier.

Storing an access token in `localStorage` is acceptable for the current learning MVP, but it increases exposure to token theft through cross-site scripting. A later version should evaluate short-lived access tokens with refresh-token rotation and an `HttpOnly` cookie.