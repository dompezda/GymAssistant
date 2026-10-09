# GymAssistant API Documentation

This document describes the current HTTP API exposed by the GymAssistant backend.

The API is implemented as a .NET 9 Minimal API and application endpoints use the `/api` prefix.

Local frontend currently calls the API at:

```txt
http://localhost:5235
```

In the Development environment, the backend also maps the OpenAPI document and a Scalar API reference for manual endpoint testing. Scalar is backed by the existing ASP.NET Core OpenAPI definition and is not mapped outside Development.

## Authorization model

`POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/refresh` and `POST /api/auth/logout` do not require a valid access JWT. Refresh and logout use the `refreshToken` HttpOnly cookie when available. `GET /api/auth/me` and the current domain API require a valid JWT bearer token.

Protected requests use:

```http
Authorization: Bearer <access-token>
```

The backend resolves the authenticated user through `ICurrentUser`. Ownership identifiers are derived from the validated JWT and are not accepted from workout-template or workout-session request bodies.

Current data rules:

- workout templates belong to one user,
- workout sessions belong to one user,
- private exercises belong to one user through `OwnerUserId`,
- system exercises are global and have `OwnerUserId = null`,
- ordinary users can create and edit only their own private exercises,
- `IsSystem` is not accepted from normal exercise create/edit requests,
- SQL Server enforces valid system/private ownership combinations through `CK_Exercises_SystemOwnership`,
- exercise history is filtered through workout sessions owned by the authenticated user.

The frontend uses `fetchApi` for protected domain requests so bearer-token attachment, refresh and one-time retry are centralized.

---

## Endpoint overview

| Method | Endpoint | Authorization | Description |
| ------ | -------- | ------------- | ----------- |
| POST | `/api/auth/register` | Public | Creates a new Identity user using email/password validation and the configured password policy |
| POST | `/api/auth/login` | Public | Authenticates a user, returns a JWT access token and creates the refresh-token cookie |
| POST | `/api/auth/refresh` | Refresh cookie | Validates and rotates the refresh token, then returns a new JWT access token |
| POST | `/api/auth/logout` | Refresh cookie optional | Revokes the active refresh token when present and deletes the refresh cookie |
| GET | `/api/auth/me` | Required | Returns the currently authenticated user |
| GET | `/api/exercises` | Required | Returns system exercises plus the current user's private exercises |
| POST | `/api/exercises` | Required | Creates a private exercise owned by the current user |
| PATCH | `/api/exercises/{exerciseId}` | Required | Updates a private exercise owned by the current user |
| GET | `/api/exercises/{exerciseId}/history` | Required | Returns the current user's history for an accessible exercise |
| GET | `/api/muscle-groups` | Required | Returns available muscle groups |
| GET | `/api/workout-templates` | Required | Returns the current user's workout templates |
| POST | `/api/workout-templates` | Required | Creates a workout template owned by the current user |
| GET | `/api/workout-templates/{id}` | Required | Returns an owned workout template |
| PATCH | `/api/workout-templates/{id}` | Required | Updates an owned workout template |
| GET | `/api/workout-sessions` | Required | Returns the current user's finished workout sessions |
| POST | `/api/workout-sessions` | Required | Starts a workout session owned by the current user |
| GET | `/api/workout-sessions/{id}` | Required | Returns an owned workout session |
| PATCH | `/api/workout-sessions/{sessionId}/exercises/{sessionExerciseId}` | Required | Updates sets for an exercise inside an owned session |
| PATCH | `/api/workout-sessions/{sessionId}/finish` | Required | Finishes an owned workout session |
| GET | `/api/workout-sessions/{sessionId}/summary` | Required | Returns summary data for an owned session |
| POST | `/api/workout-sessions/{sessionId}/exercises` | Required | Adds an accessible exercise to an owned session |
| DELETE | `/api/workout-sessions/{sessionId}/exercises/{sessionExerciseId}` | Required | Removes an exercise from an owned active session |

---

# Authentication

## POST `/api/auth/register`

Creates a new ASP.NET Core Identity user. A valid access JWT is not required.

### Request

```json
{
  "email": "user@example.com",
  "password": "Example123!"
}
```

The backend creates the Identity user with:

```txt
UserName = request.Email
Email = request.Email
```

Email is therefore the user-facing login identifier and is also stored as the technical Identity username value.

Identity is configured with unique-email enforcement and the following password policy:

```txt
minimum length: 8
requires digit: yes
requires lowercase: yes
requires uppercase: yes
requires non-alphanumeric character: yes
```

`UserManager.CreateAsync()` performs the actual Identity validation and password hashing. The endpoint does not implement custom password hashing.

When the same email already exists, Identity can produce both username and email duplicate errors because `UserName` and `Email` contain the same value. The endpoint filters the technical `DuplicateUserName` error and keeps the email-oriented validation result.

### Responses

- `200 OK` — the user was created successfully.
- `400 Bad Request` — email/password is missing or Identity validation fails, for example because the email is invalid/already used or the password does not satisfy the configured policy.

The frontend registration page also checks that password and confirmation match before sending the request. After `200 OK`, it calls the existing frontend `login(email, password)` flow and navigates to `/main-menu`. The registration endpoint itself does not issue JWT or refresh tokens.

---

## POST `/api/auth/login`

Authenticates a user with email and password through ASP.NET Core Identity.

### Request

```json
{
  "email": "user@example.com",
  "password": "example-password"
}
```

### Responses

- `200 OK` — credentials are valid.
- `400 Bad Request` — email or password is empty/invalid input.
- `401 Unauthorized` — credentials are invalid.

Successful response:

```json
{
  "accessToken": "jwt-access-token",
  "expiresAtUtc": "2026-08-20T20:30:00Z"
}
```

Successful login also creates a refresh token using the configured lifetime. Only its SHA-256 hash is persisted in SQL Server; the raw token is returned only through the `refreshToken` cookie configured as `HttpOnly`, `Secure` and `SameSite=Strict`.

The login response intentionally does not reveal whether an email exists.

---

## POST `/api/auth/refresh`

Issues a replacement access JWT when the browser has a valid refresh-token cookie. A valid access JWT is not required.

The endpoint:

1. reads `refreshToken` from the request cookie,
2. hashes the raw value,
3. finds a matching persisted token that has not expired and has not been revoked,
4. resolves the owning Identity user,
5. sets `RevokedAtUtc` on the used token,
6. creates and persists a replacement refresh-token hash,
7. replaces the browser cookie with the new raw refresh token,
8. returns a new access JWT and expiration.

Successful response:

```json
{
  "accessToken": "replacement-jwt-access-token",
  "expiresAtUtc": "2026-08-20T20:45:00Z"
}
```

### Responses

- `200 OK` — refresh succeeded and refresh-token rotation completed.
- `401 Unauthorized` — the cookie is missing/empty, the token does not exist, is expired/revoked, or its user cannot be resolved.

Each successful refresh invalidates the refresh token that was just used and makes the replacement token the active refresh credential.

---

## POST `/api/auth/logout`

Revokes the refresh token represented by the current cookie when a matching non-revoked record exists, deletes the `refreshToken` cookie and returns `204 No Content`.

The endpoint is intentionally idempotent. Missing cookies, unknown tokens and already-revoked tokens still result in `204 No Content` because the requested logged-out end state is already satisfied.

A valid access JWT is not required, so logout can still be attempted when the access token is expired.

### Response

- `204 No Content` — logout processing completed.

---

## GET `/api/auth/me`

Returns the Identity user associated with the validated JWT.

### Response `200 OK`

```json
{
  "id": "6e40dbd4-6f8f-43bc-9522-a041fbe9138d",
  "email": "user@example.com"
}
```

Returns `401 Unauthorized` when the bearer token is missing, invalid or expired, or the authenticated user cannot be resolved.

---

# Exercises

All exercise endpoints require authorization.

An authenticated user may read:

```txt
IsSystem == true
OR
OwnerUserId == currentUser.UserId
```

System/private ownership is additionally protected by the database check constraint:

```txt
(IsSystem = 1 AND OwnerUserId IS NULL)
OR
(IsSystem = 0 AND OwnerUserId IS NOT NULL)
```

## GET `/api/exercises`

Returns system exercises and private exercises owned by the authenticated user.

### Response `200 OK`

```json
[
  {
    "id": "5f6c0e70-2f53-4cb4-9b20-29c9d7e48c0a",
    "name": "Bench Press",
    "muscleGroups": [1, 6],
    "muscleGroupLabels": ["Chest", "Triceps"],
    "isSystem": true
  }
]
```

---

## POST `/api/exercises`

Creates a new private exercise owned by the authenticated user.

### Request

```json
{
  "name": "Incline Dumbbell Press",
  "muscleGroups": [1, 6]
}
```

Validation rejects an empty name, an empty muscle-group collection and unknown enum values.

The endpoint does not accept `IsSystem`. Ownership is assigned by the backend:

```txt
OwnerUserId = currentUser.UserId
IsSystem = false
```

### Response `201 Created`

```json
{
  "id": "5f6c0e70-2f53-4cb4-9b20-29c9d7e48c0a",
  "name": "Incline Dumbbell Press",
  "muscleGroups": [1, 6],
  "muscleGroupLabels": ["Chest", "Triceps"],
  "isSystem": false
}
```

System exercises are not created through this normal user endpoint. A dedicated seed/admin strategy for the system catalog is still planned.

---

## PATCH `/api/exercises/{exerciseId}`

Updates a private exercise owned by the authenticated user.

### Request

```json
{
  "name": "Dumbbell Bench Press",
  "muscleGroups": [1, 6]
}
```

The endpoint does not accept `IsSystem` and does not change exercise ownership. It resolves the target only when:

```txt
Exercise.Id == exerciseId
AND Exercise.OwnerUserId == currentUser.UserId
AND Exercise.IsSystem == false
```

Returns `404 Not Found` when the exercise does not exist, is owned by another user, or is a system exercise.

---

## GET `/api/exercises/{exerciseId}/history`

Returns exercise history grouped by workout session.

The endpoint first verifies that the exercise is system-wide or owned by the authenticated user. It then returns only matching workout sessions where:

```txt
WorkoutSession.UserId == currentUser.UserId
```

This means a shared system exercise can be visible to multiple users while each user sees only their own training history.

### Response `200 OK`

```json
{
  "name": "Bench Press",
  "id": "5f6c0e70-2f53-4cb4-9b20-29c9d7e48c0a",
  "workouts": [
    {
      "id": "ecfd3a0e-06d8-4e4b-97f6-9dfe5e28ad79",
      "workoutDate": "2026-08-09T10:00:00Z",
      "sets": [
        {
          "id": "39a38b2d-b8de-43af-b39c-487aeacb7a37",
          "reps": 8,
          "weight": 80,
          "setNumber": 1
        }
      ]
    }
  ]
}
```

Returns `404 Not Found` when the exercise does not exist or is not accessible to the current user.

---

# Muscle groups

## GET `/api/muscle-groups`

Requires authorization and returns the `MuscleGroup` enum as value/label pairs.

Current values:

| Value | Label |
| ----- | ----- |
| 1 | Chest |
| 2 | Back |
| 3 | Legs |
| 4 | Shoulders |
| 5 | Biceps |
| 6 | Triceps |
| 7 | Core |
| 8 | FullBody |

---

# Workout templates

All workout-template endpoints require authorization. A user can access only templates where:

```txt
WorkoutTemplate.UserId == currentUser.UserId
```

## GET `/api/workout-templates`

Returns templates owned by the authenticated user.

### Response `200 OK`

```json
[
  {
    "id": "4eb84d11-8f8d-4e3e-9f4c-94849f7a4c71",
    "title": "FBW A",
    "notes": "Full body workout A"
  }
]
```

---

## POST `/api/workout-templates`

Creates a template and assigns:

```txt
UserId = currentUser.UserId
```

The frontend does not provide the owner id.

### Request

```json
{
  "title": "FBW A",
  "notes": "Full body workout A",
  "exercises": [
    {
      "exerciseId": "5f6c0e70-2f53-4cb4-9b20-29c9d7e48c0a",
      "plannedSets": 3,
      "order": 1
    }
  ]
}
```

Referenced exercises must be system exercises or private exercises owned by the authenticated user. Duplicate exercise ids are rejected, and every exercise must have `plannedSets > 0`. Exercise order is normalized by the backend after sorting the request by `order`.

### Response `201 Created`

```json
{
  "id": "4eb84d11-8f8d-4e3e-9f4c-94849f7a4c71"
}
```

---

## GET `/api/workout-templates/{id}`

Returns template details only when the template belongs to the authenticated user.

### Response `200 OK`

```json
{
  "id": "4eb84d11-8f8d-4e3e-9f4c-94849f7a4c71",
  "title": "FBW A",
  "notes": "Full body workout A",
  "exercises": [
    {
      "exerciseId": "5f6c0e70-2f53-4cb4-9b20-29c9d7e48c0a",
      "exerciseName": "Bench Press",
      "plannedSets": 3,
      "order": 1
    }
  ]
}
```

Returns `404 Not Found` for a missing template or a template owned by another user.

---

## PATCH `/api/workout-templates/{id}`

Updates an owned template using the full requested exercise state.

Validation rejects:

- an empty title,
- an empty exercise list,
- duplicate exercises,
- `plannedSets <= 0`,
- exercises that are neither system-wide nor owned by the authenticated user.

Existing template exercises missing from the request are removed, existing entries are updated and new entries are added. Order is normalized by the backend.

### Response `204 No Content`

---

# Workout sessions

All workout-session endpoints require authorization.

A session is the ownership boundary for its nested `WorkoutSessionExercise` and `WorkoutSessionSet` records:

```txt
WorkoutSession
  -> WorkoutSessionExercise
      -> WorkoutSessionSet
```

Once a session has been resolved with `WorkoutSession.UserId == currentUser.UserId`, nested session exercises/sets are accessed through that owned session.

Each `WorkoutSessionExercise` stores `ExerciseNameSnapshot` so historical workout data remains readable after the source exercise name changes.

## GET `/api/workout-sessions`

Returns the authenticated user's finished sessions ordered by start date descending.

Only records with `FinishedAt != null` are returned.

### Response `200 OK`

```json
[
  {
    "id": "ecfd3a0e-06d8-4e4b-97f6-9dfe5e28ad79",
    "notes": null,
    "startedAt": "2026-08-09T10:00:00Z",
    "finishedAt": "2026-08-09T10:45:00Z",
    "exerciseCount": 4,
    "setCount": 16,
    "totalVolume": 8450,
    "durationMinutes": 45
  }
]
```

---

## POST `/api/workout-sessions`

Starts a workout session and assigns:

```txt
UserId = currentUser.UserId
```

If `workoutTemplateId` is provided, the template must belong to the authenticated user. Every exercise must be system-wide or owned by the authenticated user. An empty exercise list and duplicate exercise ids are rejected. The POST handler also rejects negative `reps` and `weight` values when provided. Both `0` and `null` are accepted, consistent with the session-set PATCH handler.

### Request

```json
{
  "workoutTemplateId": "4eb84d11-8f8d-4e3e-9f4c-94849f7a4c71",
  "exercises": [
    {
      "exerciseId": "5f6c0e70-2f53-4cb4-9b20-29c9d7e48c0a",
      "sets": [
        {
          "setNumber": 1,
          "weight": null,
          "reps": null
        }
      ],
      "sessionExerciseId": "00000000-0000-0000-0000-000000000000"
    }
  ]
}
```

The backend generates session, session-exercise and set ids and normalizes exercise/set order. `ExerciseNameSnapshot` is copied from the selected exercise. Validation failures return `400 Bad Request`, including a non-owned template and inaccessible exercises.

### Response `201 Created`

```json
{
  "id": "ecfd3a0e-06d8-4e4b-97f6-9dfe5e28ad79"
}
```

---

## GET `/api/workout-sessions/{id}`

Returns exercises and sets only when the session belongs to the authenticated user.

Exercise names are returned from `ExerciseNameSnapshot`, not the current `Exercise.Name` value.

Returns `404 Not Found` when the session cannot be resolved for the current user.

---

## PATCH `/api/workout-sessions/{sessionId}/exercises/{sessionExerciseId}`

Updates the full set state for one exercise inside an owned session.

The request contains:

```json
{
  "sets": [
    {
      "id": "39a38b2d-b8de-43af-b39c-487aeacb7a37",
      "setNumber": 1,
      "reps": 8,
      "weight": 80
    }
  ]
}
```

Validation rejects:

- an empty set list,
- negative reps or weight,
- empty set ids,
- duplicate set ids.

Sets missing from the request are removed, existing sets are updated and new set ids are added. `SetNumber` is recalculated sequentially.

---

## PATCH `/api/workout-sessions/{sessionId}/finish`

Finds the session using both `sessionId` and `currentUser.UserId` and sets:

```txt
FinishedAt = DateTime.UtcNow
```

Returns `400 Bad Request` for an empty id or when the session cannot be found by the current user.

---

## GET `/api/workout-sessions/{sessionId}/summary`

Returns summary data for an owned session, including `StartedAt`, `FinishedAt`, ordered session exercises, name snapshots and sets.

Returns `404 Not Found` when the session cannot be resolved for the current user.

---

## POST `/api/workout-sessions/{sessionId}/exercises`

Adds an exercise to an owned workout session.

The selected source exercise must satisfy:

```txt
Exercise.IsSystem == true
OR
Exercise.OwnerUserId == currentUser.UserId
```

The endpoint rejects duplicate exercises in the same session. A new session exercise receives the next order value, an `ExerciseNameSnapshot` and one empty initial set.

---

## DELETE `/api/workout-sessions/{sessionId}/exercises/{sessionExerciseId}`

Removes an exercise from an owned workout session.

The endpoint rejects deletion when:

- the session has already been finished,
- the session exercise does not exist,
- the exercise is the last remaining exercise in the session.

After deletion, remaining exercise order values are recalculated sequentially.

### Response `204 No Content`

---

# Frontend request flow

Protected domain requests use `frontend/src/api/fetchApi.ts`.

The wrapper preserves the caller's `RequestInit`, converts existing headers to a `Headers` object and attaches the current access token when available.

If the first request returns anything other than `401 Unauthorized`, that response is returned directly. On `401`, the wrapper first checks whether another request has already replaced the access token in `localStorage`. If so, it retries once with the newer token.

Otherwise all concurrent unauthorized requests share one module-level `refreshPromise`. The first request starts `refreshAccessToken()` and the others await the same promise. The refresh call uses native `fetch` with `credentials: "include"`, stores the new access token/expiration on success, and the original request is retried once with the replacement bearer token.

If refresh fails, stale local access-token data is removed and the original unauthorized response is returned. The refresh endpoint is never called through `fetchApi`, preventing recursive `401 -> refresh` loops.

`AuthProvider` also uses `fetchApi` for startup `/api/auth/me` validation. Therefore a page refresh can recover an authenticated session even when the access token is missing or stale, as long as the HttpOnly refresh cookie is still valid.

Registration uses the centralized API base URL but intentionally calls the public registration endpoint directly. On successful registration, the page reuses `AuthProvider.login()` rather than duplicating token/session setup logic.

---

# Current limitations

- A production-safe seed/admin flow for creating and maintaining system exercises is still required.
- Existing demo data may need to be reset before the production-ready release.
- Access JWTs are stored in `localStorage`; the production storage model still needs an explicit security decision.
- Authentication integration tests, some remaining workout-template edge cases and broader two-user data-isolation coverage are not implemented yet. Workout-template PATCH integration tests cover validation, system exercises, missing templates and ownership isolation. Workout-session GET and POST integration tests cover ownership, list filtering, creation/persistence and selected validation paths; session PATCH/finish/summary/add/delete coverage is still pending.
- Workout-session finish and edit lifecycle is not fully enforced yet: set updates and adding an exercise currently do not reject a finished session, and finishing again can overwrite `FinishedAt`.
- If explicit logout cannot reach the backend, frontend state is cleared locally but the server-side refresh token may remain valid until it expires or is later revoked.
- Password reset, email confirmation, external providers, roles and broader account-management features are outside the current MVP.
- Most validation is currently implemented directly inside endpoint handlers or ASP.NET Core Identity; FluentValidation is not integrated yet.
- Trainer mode, statistics and charts are planned but not implemented yet.
