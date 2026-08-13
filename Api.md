# GymAssistant API Documentation

This document describes the current HTTP API exposed by the GymAssistant backend.

The API is implemented as a .NET 9 Minimal API and application endpoints use the `/api` prefix.

Local frontend currently calls the API at:

```txt
http://localhost:5235
```

## Authorization model

`POST /api/auth/login` is public. `GET /api/auth/me` and the current domain API require a valid JWT bearer token.

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
- exercise history is filtered through workout sessions owned by the authenticated user.

The frontend uses `fetchApi` for protected domain requests so the bearer token is attached in one place.

---

## Endpoint overview

| Method | Endpoint | Authorization | Description |
| ------ | -------- | ------------- | ----------- |
| POST | `/api/auth/login` | Public | Authenticates a user and returns a JWT access token |
| GET | `/api/auth/me` | Required | Returns the currently authenticated user |
| GET | `/api/exercises` | Required | Returns system exercises plus the current user's private exercises |
| POST | `/api/exercises` | Required | Creates an exercise using the current development system/private flow |
| PATCH | `/api/exercises/{exerciseId}` | Required | Updates an accessible exercise |
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
  "expiresAtUtc": "2026-08-09T14:30:00Z"
}
```

The login response intentionally does not reveal whether an email exists.

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

Creates a new exercise.

### Current development request

```json
{
  "name": "Incline Dumbbell Press",
  "muscleGroups": [1, 6],
  "isSystem": false
}
```

Validation rejects an empty name, an empty muscle-group collection and unknown enum values.

During development, `isSystem` is deliberately exposed so both ownership modes can be tested. The backend sets ownership itself:

```txt
isSystem = true  -> OwnerUserId = null
isSystem = false -> OwnerUserId = currentUser.UserId
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

> `isSystem` is a temporary development capability. A production-ready user endpoint should not allow an ordinary authenticated user to create global system exercises.

---

## PATCH `/api/exercises/{exerciseId}`

Updates an exercise that is either system-wide or owned by the authenticated user.

### Request

```json
{
  "name": "Dumbbell Bench Press",
  "muscleGroups": [1, 6],
  "isSystem": false
}
```

The current development flow also updates ownership according to `isSystem`:

- `true` clears `OwnerUserId`,
- `false` assigns `OwnerUserId = currentUser.UserId`.

Returns `404 Not Found` when the exercise is not accessible to the authenticated user.

> Because system/private switching is still a test feature, an authenticated user can currently edit a system exercise. This is a known temporary limitation and must be replaced by a seed/admin strategy before the feature is production-ready.

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

Referenced exercises must be system exercises or private exercises owned by the authenticated user. Duplicate exercise ids are rejected.

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

If `workoutTemplateId` is provided, the template must belong to the authenticated user. Every exercise must be system-wide or owned by the authenticated user.

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

The backend generates session, session-exercise and set ids and normalizes exercise/set order. `ExerciseNameSnapshot` is copied from the selected exercise.

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

The wrapper preserves the caller's `RequestInit`, converts existing headers to a `Headers` object and adds the bearer token when available:

```ts
const token = localStorage.getItem("accessToken");
const headers = new Headers(options.headers);

if (token) {
  headers.set("Authorization", `Bearer ${token}`);
}

return fetch(url, { ...options, headers });
```

The wrapper does not yet centralize `401 Unauthorized` handling. `AuthProvider` separately handles login and startup `/api/auth/me` validation.

---

# Current limitations

- `isSystem` is still exposed in the normal exercise create/edit flow for development testing. A production-safe seed/admin flow is still required.
- The database does not yet enforce the final `IsSystem` / `OwnerUserId` invariant with a check constraint.
- Existing demo data may need to be reset after ownership work is finalized.
- Centralized frontend handling for expired/stale JWTs is not implemented yet.
- Authentication and two-user data-isolation integration tests are not implemented yet.
- Registration, refresh tokens, account-management features and server-side logout are outside the current MVP.
- Most validation is currently implemented directly inside endpoint handlers; FluentValidation is not integrated yet.
- Trainer mode, statistics and charts are planned but not implemented yet.
