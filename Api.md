# GymAssistant API Documentation

This document describes the current HTTP API exposed by the GymAssistant backend.

The API is implemented as a .NET Minimal API and all application endpoints use the `/api` prefix.

Local frontend currently calls the API at:

```txt
http://localhost:5235
```

> Note: the project is currently in a single-user/demo stage. Some data is still assigned to the test user `Caran`. Authentication and user ownership are planned but are not implemented yet.

---

## Endpoint overview

| Method | Endpoint | Description |
| ------ | -------- | ----------- |
| GET | `/api/exercises` | Returns all exercises |
| POST | `/api/exercises` | Creates a new exercise |
| PATCH | `/api/exercises/{exerciseId}` | Updates an existing exercise |
| GET | `/api/exercises/{exerciseId}/history` | Returns exercise history grouped by workout session |
| GET | `/api/muscle-groups` | Returns available muscle groups |
| GET | `/api/workout-templates` | Returns workout templates |
| POST | `/api/workout-templates` | Creates a workout template |
| GET | `/api/workout-templates/{id}` | Returns workout template details |
| PATCH | `/api/workout-templates/{id}` | Updates a workout template |
| GET | `/api/workout-sessions` | Returns finished workout sessions |
| POST | `/api/workout-sessions` | Starts a new workout session |
| GET | `/api/workout-sessions/{id}` | Returns active/full workout session details |
| PATCH | `/api/workout-sessions/{sessionId}/exercises/{sessionExerciseId}` | Updates sets for one exercise inside a session |
| PATCH | `/api/workout-sessions/{sessionId}/finish` | Finishes a workout session |
| GET | `/api/workout-sessions/{sessionId}/summary` | Returns workout session summary |
| POST | `/api/workout-sessions/{sessionId}/exercises` | Adds an exercise to an existing workout session |

---

# Exercises

## GET `/api/exercises`

Returns all exercises with their assigned muscle groups.

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

### Response fields

| Field | Type | Description |
| ----- | ---- | ----------- |
| `id` | `Guid` | Exercise identifier |
| `name` | `string` | Exercise name |
| `muscleGroups` | `int[]` | Muscle group enum values |
| `muscleGroupLabels` | `string[]` | Muscle group names |
| `isSystem` | `bool` | Indicates whether the exercise is a system/default exercise |

---

## POST `/api/exercises`

Creates a new exercise.

### Request body

```json
{
  "name": "Incline Dumbbell Press",
  "muscleGroups": [1, 6],
  "isSystem": false
}
```

### Request fields

| Field | Type | Required | Description |
| ----- | ---- | -------- | ----------- |
| `name` | `string` | Yes | Exercise name |
| `muscleGroups` | `int[]` | Yes | Muscle group enum values |
| `isSystem` | `bool` | Yes | Indicates whether the exercise is a system/default exercise |

### Validation

Returns `400 Bad Request` when:

- `name` is empty or whitespace,
- `muscleGroups` is null or empty,
- at least one muscle group value is not defined in the `MuscleGroup` enum.

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

---

## PATCH `/api/exercises/{exerciseId}`

Updates an existing exercise and synchronizes its muscle groups.

### Route parameters

| Parameter | Type | Description |
| --------- | ---- | ----------- |
| `exerciseId` | `Guid` | Exercise identifier |

### Request body

```json
{
  "name": "Dumbbell Bench Press",
  "muscleGroups": [1, 6],
  "isSystem": false
}
```

### Request fields

| Field | Type | Required | Description |
| ----- | ---- | -------- | ----------- |
| `name` | `string` | Yes | New exercise name |
| `muscleGroups` | `int[]` | Yes | Full target list of muscle group enum values |
| `isSystem` | `bool` | Yes | Indicates whether the exercise is a system/default exercise |

### Validation

Returns `400 Bad Request` when:

- `exerciseId` is empty,
- `name` is empty or whitespace,
- `muscleGroups` is null or empty,
- at least one muscle group value is not defined in the `MuscleGroup` enum.

Returns `404 Not Found` when the exercise does not exist.

### Response `200 OK`

```json
"5f6c0e70-2f53-4cb4-9b20-29c9d7e48c0a"
```

---

## GET `/api/exercises/{exerciseId}/history`

Returns the selected exercise history grouped by workout session.

Only sets belonging to the selected exercise are returned. Workout entries are ordered by workout date descending, so the most recent workout is returned first.

### Route parameters

| Parameter | Type | Description |
| --------- | ---- | ----------- |
| `exerciseId` | `Guid` | Exercise identifier |

### Response `200 OK`

```json
{
  "name": "Bench Press",
  "id": "5f6c0e70-2f53-4cb4-9b20-29c9d7e48c0a",
  "workouts": [
    {
      "id": "ecfd3a0e-06d8-4e4b-97f6-9dfe5e28ad79",
      "workoutDate": "2026-07-06T18:00:00.0000000Z",
      "sets": [
        {
          "id": "39a38b2d-b8de-43af-b39c-487aeacb7a37",
          "reps": 8,
          "weight": 74,
          "setNumber": 1
        },
        {
          "id": "7ae29166-f73c-43df-82ff-b7f8837cc3f2",
          "reps": 6,
          "weight": 76,
          "setNumber": 2
        }
      ]
    }
  ]
}
```

When the exercise exists but has not been used in any workout session, `workouts` is returned as an empty array.

### Response fields

| Field | Type | Description |
| ----- | ---- | ----------- |
| `name` | `string` | Current exercise name |
| `id` | `Guid` | Exercise identifier |
| `workouts` | `ExerciseHistoryWorkoutResponse[]` | Workout sessions containing the selected exercise |

### `ExerciseHistoryWorkoutResponse`

| Field | Type | Description |
| ----- | ---- | ----------- |
| `id` | `Guid` | Workout session identifier |
| `workoutDate` | `DateTime` | Workout session start date |
| `sets` | `WorkoutSet[]` | Sets performed for the selected exercise during this workout |

### `WorkoutSet`

| Field | Type | Description |
| ----- | ---- | ----------- |
| `id` | `Guid` | Set identifier |
| `reps` | `int?` | Number of repetitions |
| `weight` | `decimal?` | Set weight |
| `setNumber` | `int` | Set order within the exercise |

### Validation

Returns `400 Bad Request` when `exerciseId` is empty.

Returns `404 Not Found` when the exercise does not exist.

---

# Muscle groups

## GET `/api/muscle-groups`

Returns available muscle groups.

### Response `200 OK`

```json
[
  {
    "value": 1,
    "label": "Chest"
  },
  {
    "value": 2,
    "label": "Back"
  },
  {
    "value": 3,
    "label": "Legs"
  },
  {
    "value": 4,
    "label": "Shoulders"
  },
  {
    "value": 5,
    "label": "Biceps"
  },
  {
    "value": 6,
    "label": "Triceps"
  },
  {
    "value": 7,
    "label": "Core"
  },
  {
    "value": 8,
    "label": "FullBody"
  }
]
```

### Response fields

| Field | Type | Description |
| ----- | ---- | ----------- |
| `value` | `int` | Muscle group enum value |
| `label` | `string` | Muscle group name |

---

# Workout templates

A workout template defines a reusable workout plan. A template contains ordered exercises and planned set counts.

## GET `/api/workout-templates`

Returns all workout templates.

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

### Response fields

| Field | Type | Description |
| ----- | ---- | ----------- |
| `id` | `Guid` | Workout template identifier |
| `title` | `string` | Template title |
| `notes` | `string?` | Optional template notes |

---

## POST `/api/workout-templates`

Creates a new workout template.

### Request body

```json
{
  "title": "FBW A",
  "notes": "Full body workout A",
  "exercises": [
    {
      "exerciseId": "5f6c0e70-2f53-4cb4-9b20-29c9d7e48c0a",
      "plannedSets": 3,
      "order": 1
    },
    {
      "exerciseId": "f7ab35e9-8f0e-4d87-bc01-59d2272d5e87",
      "plannedSets": 4,
      "order": 2
    }
  ]
}
```

### Request fields

| Field | Type | Required | Description |
| ----- | ---- | -------- | ----------- |
| `title` | `string` | Yes | Template title |
| `notes` | `string?` | No | Optional template notes |
| `exercises` | `CreateWorkoutTemplateExerciseRequest[]` | Yes | Exercises included in the template |

### `CreateWorkoutTemplateExerciseRequest`

| Field | Type | Required | Description |
| ----- | ---- | -------- | ----------- |
| `exerciseId` | `Guid` | Yes | Exercise identifier |
| `plannedSets` | `int` | Yes | Planned number of sets |
| `order` | `int` | Yes | Exercise order in the template |

### Validation

Returns `400 Bad Request` when:

- `title` is empty or whitespace,
- `exercises` is empty,
- at least one exercise does not exist.

### Response `201 Created`

```json
{
  "id": "4eb84d11-8f8d-4e3e-9f4c-94849f7a4c71"
}
```

---

## GET `/api/workout-templates/{id}`

Returns details of a single workout template.

### Route parameters

| Parameter | Type | Description |
| --------- | ---- | ----------- |
| `id` | `Guid` | Workout template identifier |

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

### Response `404 Not Found`

Returned when the workout template does not exist.

---

## PATCH `/api/workout-templates/{id}`

Updates a workout template. The request contains the full target state of the template exercises. Existing template exercises are updated, missing ones are removed, and new ones are added.

### Route parameters

| Parameter | Type | Description |
| --------- | ---- | ----------- |
| `id` | `Guid` | Workout template identifier |

### Request body

```json
{
  "title": "FBW A - updated",
  "notes": "Updated full body workout",
  "exercises": [
    {
      "exerciseId": "5f6c0e70-2f53-4cb4-9b20-29c9d7e48c0a",
      "plannedSets": 4,
      "order": 1
    }
  ]
}
```

### Validation

Returns `400 Bad Request` when:

- `title` is empty or whitespace,
- `exercises` is empty,
- duplicate exercises are provided,
- any `plannedSets` value is less than or equal to `0`,
- at least one exercise does not exist.

Returns `404 Not Found` when the workout template does not exist.

### Response `204 No Content`

No response body.

---

# Workout sessions

A workout session represents a started workout. It contains ordered session exercises and sets.

The current model is:

```txt
WorkoutSession
  -> WorkoutSessionExercise
      -> WorkoutSessionSet
```

Each `WorkoutSessionExercise` stores `ExerciseNameSnapshot`. This preserves historical training data even if the original exercise name is changed later.

---

## GET `/api/workout-sessions`

Returns finished workout sessions ordered by start date descending.

Only sessions with `finishedAt != null` are returned.

### Response `200 OK`

```json
[
  {
    "id": "ecfd3a0e-06d8-4e4b-97f6-9dfe5e28ad79",
    "notes": null,
    "startedAt": "2026-06-21T11:41:06.0000000Z",
    "finishedAt": "2026-06-21T11:59:48.0000000Z",
    "exerciseCount": 4,
    "setCount": 16,
    "totalVolume": 8450,
    "durationMinutes": 18
  }
]
```

### Response fields

| Field | Type | Description |
| ----- | ---- | ----------- |
| `id` | `Guid` | Workout session identifier |
| `notes` | `string?` | Optional session notes |
| `startedAt` | `DateTime` | Session start time in UTC |
| `finishedAt` | `DateTime?` | Session finish time in UTC |
| `exerciseCount` | `int` | Number of exercises in the session |
| `setCount` | `int` | Number of sets in the session |
| `totalVolume` | `decimal?` | Sum of `reps * weight` across all sets |
| `durationMinutes` | `int?` | Duration between `startedAt` and `finishedAt` in minutes |

---

## POST `/api/workout-sessions`

Starts a new workout session. The session may optionally be created from a workout template.

The backend sets:

- `id`,
- `startedAt`,
- `userId`, currently hardcoded as `Caran`,
- `exerciseNameSnapshot` for every session exercise.

### Request body - from template

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
        },
        {
          "setNumber": 2,
          "weight": null,
          "reps": null
        }
      ]
    }
  ]
}
```

### Request body - empty/custom workout

```json
{
  "workoutTemplateId": null,
  "exercises": [
    {
      "exerciseId": "5f6c0e70-2f53-4cb4-9b20-29c9d7e48c0a",
      "sets": [
        {
          "setNumber": 1,
          "weight": null,
          "reps": null
        }
      ]
    }
  ]
}
```

### Request fields

| Field | Type | Required | Description |
| ----- | ---- | -------- | ----------- |
| `workoutTemplateId` | `Guid?` | No | Template used to start the session, if any |
| `exercises` | `CreateWorkoutSessionExerciseRequest[]` | Yes | Exercises included in the session |

### `CreateWorkoutSessionExerciseRequest`

| Field | Type | Required | Description |
| ----- | ---- | -------- | ----------- |
| `exerciseId` | `Guid` | Yes | Exercise identifier |
| `sets` | `CreateWorkoutSessionSetRequest[]` | Yes | Initial sets for the exercise |

### `CreateWorkoutSessionSetRequest`

| Field | Type | Required | Description |
| ----- | ---- | -------- | ----------- |
| `setNumber` | `int` | Yes | Initial set number. The backend reorders sets sequentially while creating the session. |
| `weight` | `decimal?` | No | Set weight |
| `reps` | `int?` | No | Set repetitions |

### Validation

Returns `400 Bad Request` when:

- `exercises` is empty,
- `workoutTemplateId` is provided but does not exist,
- at least one exercise does not exist.

### Response `201 Created`

```json
{
  "id": "ecfd3a0e-06d8-4e4b-97f6-9dfe5e28ad79"
}
```

---

## GET `/api/workout-sessions/{id}`

Returns full workout session details, including exercises and sets.

This endpoint is used by the active workout session page.

### Route parameters

| Parameter | Type | Description |
| --------- | ---- | ----------- |
| `id` | `Guid` | Workout session identifier |

### Response `200 OK`

```json
{
  "id": "ecfd3a0e-06d8-4e4b-97f6-9dfe5e28ad79",
  "workoutTemplateId": "4eb84d11-8f8d-4e3e-9f4c-94849f7a4c71",
  "userId": "Caran",
  "exercises": [
    {
      "id": "2937f7ed-df91-460c-8ab1-6d61db6ef4ab",
      "exerciseId": "5f6c0e70-2f53-4cb4-9b20-29c9d7e48c0a",
      "exerciseName": "Bench Press",
      "order": 1,
      "sets": [
        {
          "id": "39a38b2d-b8de-43af-b39c-487aeacb7a37",
          "setNumber": 1,
          "weight": 80,
          "reps": 8
        }
      ]
    }
  ]
}
```

### Response `404 Not Found`

Returned when the workout session does not exist.

---

## PATCH `/api/workout-sessions/{sessionId}/exercises/{sessionExerciseId}`

Updates all sets for a single exercise inside a workout session.

The request represents the full target state of the exercise sets:

- sets missing from the request are removed,
- existing sets are updated,
- new sets are added,
- `setNumber` is recalculated sequentially by the backend.

### Route parameters

| Parameter | Type | Description |
| --------- | ---- | ----------- |
| `sessionId` | `Guid` | Workout session identifier |
| `sessionExerciseId` | `Guid` | Identifier of the exercise inside the workout session |

### Request body

```json
{
  "sets": [
    {
      "id": "39a38b2d-b8de-43af-b39c-487aeacb7a37",
      "setNumber": 1,
      "reps": 8,
      "weight": 80
    },
    {
      "id": "7ae29166-f73c-43df-82ff-b7f8837cc3f2",
      "setNumber": 2,
      "reps": 7,
      "weight": 80
    }
  ]
}
```

### Request fields

| Field | Type | Required | Description |
| ----- | ---- | -------- | ----------- |
| `sets` | `WorkoutSessionSetSummaryDto[]` | Yes | Full target list of sets for this session exercise |

### `WorkoutSessionSetSummaryDto`

| Field | Type | Required | Description |
| ----- | ---- | -------- | ----------- |
| `id` | `Guid` | Yes | Set identifier. New frontend-created sets must provide a generated GUID. |
| `setNumber` | `int` | Yes | Current set number. The backend recalculates it sequentially. |
| `reps` | `int?` | No | Set repetitions. Must be `0` or greater when provided. |
| `weight` | `decimal?` | No | Set weight. Must be `0` or greater when provided. |

### Validation

Returns `400 Bad Request` when:

- `sets` is null or empty,
- any `reps` value is less than `0`,
- any `weight` value is less than `0`,
- any set `id` is empty,
- duplicate set ids are provided.

Returns `404 Not Found` when:

- the workout session does not exist,
- the selected session exercise does not exist in that session.

### Response `200 OK`

No response body.

---

## PATCH `/api/workout-sessions/{sessionId}/finish`

Finishes a workout session by setting `finishedAt` to the current UTC time.

### Route parameters

| Parameter | Type | Description |
| --------- | ---- | ----------- |
| `sessionId` | `Guid` | Workout session identifier |

### Validation

Returns `400 Bad Request` when:

- `sessionId` is empty,
- the session does not exist.

### Response `200 OK`

No response body.

---

## GET `/api/workout-sessions/{sessionId}/summary`

Returns workout session summary data.

This endpoint is used by the workout summary page after finishing a session and by the training history page when opening a past session.

### Route parameters

| Parameter | Type | Description |
| --------- | ---- | ----------- |
| `sessionId` | `Guid` | Workout session identifier |

### Response `200 OK`

```json
{
  "startedAt": "2026-06-21T11:41:06.0000000Z",
  "finishedAt": "2026-06-21T11:59:48.0000000Z",
  "sessionExercises": [
    {
      "exerciseId": "5f6c0e70-2f53-4cb4-9b20-29c9d7e48c0a",
      "sets": [
        {
          "id": "39a38b2d-b8de-43af-b39c-487aeacb7a37",
          "setNumber": 1,
          "reps": 8,
          "weight": 80
        }
      ],
      "sessionExerciseId": "2937f7ed-df91-460c-8ab1-6d61db6ef4ab",
      "exerciseName": "Bench Press",
      "order": 1
    }
  ]
}
```

### Validation

Returns `400 Bad Request` when `sessionId` is empty.

Returns `404 Not Found` when the workout session does not exist.

---

## POST `/api/workout-sessions/{sessionId}/exercises`

Adds an exercise to an existing workout session.

The backend creates a new `WorkoutSessionExercise` with:

- a new session exercise id,
- `exerciseNameSnapshot` based on the current exercise name,
- `order` equal to the next position in the session,
- one empty set with `setNumber = 1`.

### Route parameters

| Parameter | Type | Description |
| --------- | ---- | ----------- |
| `sessionId` | `Guid` | Workout session identifier |

### Request body

```json
{
  "exerciseId": "5f6c0e70-2f53-4cb4-9b20-29c9d7e48c0a"
}
```

### Request fields

| Field | Type | Required | Description |
| ----- | ---- | -------- | ----------- |
| `exerciseId` | `Guid` | Yes | Exercise identifier to add to the session |

### Validation

Returns `400 Bad Request` when:

- `sessionId` is empty,
- `exerciseId` is empty.

Returns `404 Not Found` when:

- the workout session does not exist,
- the exercise does not exist.

Returns `409 Conflict` when the selected exercise already exists in the session.

### Response `200 OK`

```json
{
  "id": "2937f7ed-df91-460c-8ab1-6d61db6ef4ab",
  "exerciseId": "5f6c0e70-2f53-4cb4-9b20-29c9d7e48c0a",
  "exerciseName": "Bench Press",
  "order": 4,
  "sets": [
    {
      "id": "39a38b2d-b8de-43af-b39c-487aeacb7a37",
      "setNumber": 1,
      "weight": null,
      "reps": null
    }
  ]
}
```

---

# Domain notes

## Current user handling

The app currently uses a temporary demo user value:

```txt
Caran
```

Planned direction:
- add authentication,
- assign data to the authenticated user,
- filter exercises, templates and sessions by user,
- keep system exercises globally available,
- avoid accepting `userId` from the frontend.

## Exercise name snapshot

Workout sessions store `ExerciseNameSnapshot` on each `WorkoutSessionExercise`.

This is intentional. Historical training data should remain readable even if the original exercise name is edited later.

## Current session model

The current model supports regular exercises and sets:

```txt
WorkoutSession
  -> WorkoutSessionExercise
      -> WorkoutSessionSet
```

Future features such as supersets, circuits or grouped blocks may require introducing an additional level, for example:

```txt
WorkoutSession
  -> WorkoutSessionBlock
      -> WorkoutSessionExercise
          -> WorkoutSessionSet
```

This is not implemented yet.

---

# Current limitations

- Authentication is not implemented yet.
- User management is not implemented yet.
- Most validation is currently implemented directly inside endpoint handlers.
- FluentValidation is planned but not implemented yet.
- Trainer mode is planned but not implemented yet.
- Statistics and charts are planned but not implemented yet.