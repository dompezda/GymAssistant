# GymAssistant API — Endpointy

Dokumentacja aktualnych endpointów API aplikacji GymAssistant.

Base URL lokalnie:

```txt
https://localhost:<port>
```

Wszystkie endpointy znajdują się pod prefiksem:

```txt
/api
```

## Spis endpointów

| Metoda | Endpoint                      | Opis                                 |
| ------ | ----------------------------- | ------------------------------------ |
| GET    | `/api/exercises`              | Pobiera listę ćwiczeń                |
| POST   | `/api/exercises`              | Tworzy nowe ćwiczenie                |
| GET    | `/api/muscle-groups`          | Pobiera listę grup mięśniowych       |
| GET    | `/api/workout-templates`      | Pobiera listę planów treningowych    |
| GET    | `/api/workout-templates/{id}` | Pobiera szczegóły planu treningowego |
| POST   | `/api/workout-templates`      | Tworzy nowy plan treningowy          |
| POST   | `/api/workout-sessions`       | Zapisuje sesję treningową            |

---

# Exercises

## GET `/api/exercises`

Pobiera listę ćwiczeń.

### Response `200 OK`

```json
[
  {
    "id": "5f6c0e70-2f53-4cb4-9b20-29c9d7e48c0a",
    "name": "Bench Press",
    "muscleGroups": [1, 5],
    "muscleGroupLabels": ["Chest", "Triceps"],
    "isSystem": true
  }
]
```

### Pola response

| Pole                | Typ        | Opis                                      |
| ------------------- | ---------- | ----------------------------------------- |
| `id`                | `Guid`     | Identyfikator ćwiczenia                   |
| `name`              | `string`   | Nazwa ćwiczenia                           |
| `muscleGroups`      | `int[]`    | Lista grup mięśniowych jako wartości enum |
| `muscleGroupLabels` | `string[]` | Lista nazw grup mięśniowych               |
| `isSystem`          | `bool`     | Informacja, czy ćwiczenie jest systemowe  |

---

## POST `/api/exercises`

Tworzy nowe ćwiczenie.

### Request body

```json
{
  "name": "Incline Dumbbell Press",
  "muscleGroups": [1, 5]
}
```

### Pola request

| Pole           | Typ      | Wymagane | Opis                   |
| -------------- | -------- | -------- | ---------------------- |
| `name`         | `string` | Tak      | Nazwa ćwiczenia        |
| `muscleGroups` | `int[]`  | Tak      | Lista grup mięśniowych |

### Walidacja

Endpoint zwraca `400 Bad Request`, gdy:

* nazwa ćwiczenia jest pusta,
* nie wybrano żadnej grupy mięśniowej,
* podano nieprawidłową wartość grupy mięśniowej.

### Response `201 Created`

```json
{
  "id": "5f6c0e70-2f53-4cb4-9b20-29c9d7e48c0a"
}
```

---

# Muscle Groups

## GET `/api/muscle-groups`

Pobiera listę dostępnych grup mięśniowych.

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
  }
]
```

### Pola response

| Pole    | Typ      | Opis                          |
| ------- | -------- | ----------------------------- |
| `value` | `int`    | Wartość enum grupy mięśniowej |
| `label` | `string` | Nazwa grupy mięśniowej        |

---

# Workout Templates

## GET `/api/workout-templates`

Pobiera listę planów treningowych.

### Response `200 OK`

```json
[
  {
    "id": "4eb84d11-8f8d-4e3e-9f4c-94849f7a4c71",
    "title": "FBW A",
    "notes": "Plan treningowy na poniedziałek"
  }
]
```

### Pola response

| Pole    | Typ       | Opis                             |
| ------- | --------- | -------------------------------- |
| `id`    | `Guid`    | Identyfikator planu treningowego |
| `title` | `string`  | Nazwa planu                      |
| `notes` | `string?` | Notatki do planu                 |

---

## GET `/api/workout-templates/{id}`

Pobiera szczegóły konkretnego planu treningowego.

### Parametry route

| Parametr | Typ    | Opis                             |
| -------- | ------ | -------------------------------- |
| `id`     | `Guid` | Identyfikator planu treningowego |

### Response `200 OK`

```json
{
  "id": "4eb84d11-8f8d-4e3e-9f4c-94849f7a4c71",
  "title": "FBW A",
  "notes": "Plan treningowy na poniedziałek",
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

Zwracany, gdy plan treningowy o podanym `id` nie istnieje.

---

## POST `/api/workout-templates`

Tworzy nowy plan treningowy.

### Request body

```json
{
  "title": "FBW A",
  "notes": "Plan treningowy na poniedziałek",
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

### Pola request

| Pole        | Typ                         | Wymagane | Opis                     |
| ----------- | --------------------------- | -------- | ------------------------ |
| `title`     | `string`                    | Tak      | Nazwa planu treningowego |
| `notes`     | `string?`                   | Nie      | Notatki do planu         |
| `exercises` | `WorkoutTemplateExercise[]` | Tak      | Lista ćwiczeń w planie   |

### `WorkoutTemplateExercise`

| Pole          | Typ    | Wymagane | Opis                         |
| ------------- | ------ | -------- | ---------------------------- |
| `exerciseId`  | `Guid` | Tak      | Identyfikator ćwiczenia      |
| `plannedSets` | `int`  | Tak      | Planowana liczba serii       |
| `order`       | `int`  | Tak      | Kolejność ćwiczenia w planie |

### Walidacja

Endpoint zwraca `400 Bad Request`, gdy:

* `title` jest pusty,
* lista `exercises` jest pusta,
* jedno lub więcej ćwiczeń nie istnieje w bazie.

### Response `201 Created`

```json
{
  "id": "4eb84d11-8f8d-4e3e-9f4c-94849f7a4c71"
}
```

---

# Workout Sessions

## POST `/api/workout-sessions`

Zapisuje sesję treningową.

Aktualnie endpoint zapisuje całą sesję na podstawie danych przesłanych z frontendu. Sesja może być powiązana z planem treningowym przez `workoutTemplateId`, ale nie musi.

### Request body

```json
{
  "workoutTemplateId": "4eb84d11-8f8d-4e3e-9f4c-94849f7a4c71",
  "exercises": [
    {
      "exerciseId": "5f6c0e70-2f53-4cb4-9b20-29c9d7e48c0a",
      "sets": [
        {
          "setNumber": 1,
          "weight": 80.5,
          "reps": 8
        },
        {
          "setNumber": 2,
          "weight": 80.5,
          "reps": 7
        }
      ]
    }
  ]
}
```

Dla pustego treningu `workoutTemplateId` może być `null`:

```json
{
  "workoutTemplateId": null,
  "exercises": [
    {
      "exerciseId": "5f6c0e70-2f53-4cb4-9b20-29c9d7e48c0a",
      "sets": [
        {
          "setNumber": 1,
          "weight": 80,
          "reps": 8
        }
      ]
    }
  ]
}
```

### Pola request

| Pole                | Typ                        | Wymagane | Opis                                                       |
| ------------------- | -------------------------- | -------- | ---------------------------------------------------------- |
| `workoutTemplateId` | `Guid?`                    | Nie      | Identyfikator planu treningowego, z którego powstała sesja |
| `exercises`         | `WorkoutSessionExercise[]` | Tak      | Lista ćwiczeń wykonanych w sesji                           |

### `WorkoutSessionExercise`

| Pole         | Typ                   | Wymagane | Opis                      |
| ------------ | --------------------- | -------- | ------------------------- |
| `exerciseId` | `Guid`                | Tak      | Identyfikator ćwiczenia   |
| `sets`       | `WorkoutSessionSet[]` | Tak      | Lista serii dla ćwiczenia |

### `WorkoutSessionSet`

| Pole        | Typ        | Wymagane | Opis                 |
| ----------- | ---------- | -------- | -------------------- |
| `setNumber` | `int`      | Tak      | Numer serii          |
| `weight`    | `decimal?` | Nie      | Ciężar użyty w serii |
| `reps`      | `int?`     | Nie      | Liczba powtórzeń     |

### Walidacja

Endpoint zwraca `400 Bad Request`, gdy:

* lista `exercises` jest pusta,
* podany `workoutTemplateId` nie istnieje,
* jedno lub więcej ćwiczeń nie istnieje w bazie.

### Response `201 Created`

```json
{
  "id": "ecfd3a0e-06d8-4e4b-97f6-9dfe5e28ad79"
}
```

---

# Aktualne ograniczenia / TODO

## Autoryzacja

Aktualnie endpointy nie są jeszcze opisane jako chronione autoryzacją. Docelowo większość endpointów powinna wymagać zalogowanego użytkownika.

Planowane podejście:

* rejestracja użytkownika,
* logowanie,
* przypisywanie danych do `UserId`,
* filtrowanie ćwiczeń, planów i sesji po aktualnie zalogowanym użytkowniku,
* brak przesyłania `UserId` z frontendu.

## UserId

Aktualnie w kodzie część danych jest przypisywana do testowego użytkownika:

```txt
Caran
```

Docelowo `UserId` powinien być pobierany po stronie backendu z kontekstu zalogowanego użytkownika.

## Sesje treningowe

Aktualny model sesji obsługuje:

```txt
WorkoutSession
  -> WorkoutSessionExercise
      -> WorkoutSessionSet
```

Planowany kierunek rozwoju:

```txt
WorkoutSession
  -> WorkoutSessionBlock
      -> WorkoutSessionExercise
          -> WorkoutSessionSet
```

Dzięki temu będzie można w przyszłości obsłużyć:

* normalne ćwiczenia,
* superserie,
* obwody,
* grupowanie ćwiczeń w bloki.

## Snapshot nazw ćwiczeń

Docelowo sesja treningowa powinna zapisywać snapshot nazwy ćwiczenia, np.:

```txt
ExerciseNameSnapshot
```

Dzięki temu historia treningów pozostanie czytelna nawet wtedy, gdy nazwa ćwiczenia zostanie później zmieniona.

## Czas sesji

Aktualnie data sesji jest ustawiana po stronie backendu.

Docelowo warto rozważyć pola:

```txt
StartedAt
FinishedAt
Duration
```

albo przynajmniej:

```txt
Date
StartedAt
FinishedAt
```

## Historia sesji

Aktualnie dostępny jest endpoint do zapisu sesji. Do pełnej wersji alpha warto dodać:

| Metoda | Endpoint                     | Opis                                  |
| ------ | ---------------------------- | ------------------------------------- |
| GET    | `/api/workout-sessions`      | Lista zapisanych sesji treningowych   |
| GET    | `/api/workout-sessions/{id}` | Szczegóły zapisanej sesji treningowej |
