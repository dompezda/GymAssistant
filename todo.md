# GymAssistant TODO

The core authentication flow is implemented: public registration, login, persisted refresh tokens, refresh-token rotation, centralized frontend `401 -> refresh -> retry once`, startup session recovery and server-side logout/revocation.

Registration is now end-to-end: ASP.NET Core Identity enforces unique email addresses and the configured password policy, the frontend has a `/register` page with password confirmation and validation-error display, successful registration reuses the existing `login()` flow for automatic sign-in, and login/register pages link to each other.

Configuration cleanup for the current auth/frontend flow is also complete: the frontend API base URL is centralized through `VITE_API_URL`, refresh-token lifetime is configured and validated through `RefreshTokenOptions`, the credential-enabled CORS origin is read from backend configuration, and OpenAPI plus Scalar are mapped in Development for manual API testing.

System/private exercise ownership is now enforced: normal users create and edit only their own private exercises, `IsSystem` is not accepted from create/edit requests, and SQL Server enforces valid `IsSystem` / `OwnerUserId` combinations through a check constraint. The frontend marks system exercises with a `System` badge in both the exercise picker and the main exercise list, and system exercise details are presented in a read-only mode without an edit action.

Backend integration testing is bootstrapped with xUnit and `WebApplicationFactory`. The test host replaces the normal SQL Server `AppDbContext` configuration with SQLite in-memory and creates the schema for the test host. Current exercise coverage verifies unauthenticated `401 Unauthorized`, two-user private-exercise isolation, visibility of the current user's private exercises together with system exercises, ownership and `IsSystem = false` when creating a private exercise, `400 Bad Request` for an empty exercise name, successful editing of the current user's private exercise including muscle-group changes, blocked edits of another user's or a system exercise with persisted data verified unchanged, muscle-group patch behavior without duplicate relations and with only missing relations added, system-exercise history filtered to the authenticated user's workout sessions, and `404 Not Found` when requesting history for another user's private exercise. Authenticated cases use real JWT bearer tokens generated for Identity users created by the test setup. Reusable `TestHelpers` now centralize common user creation, client authorization, private/system exercise setup (including muscle groups) and workout-template setup so individual tests stay focused on behavior. Workout-template integration tests now cover create validation and ownership, list/detail user isolation, order normalization and successful PATCH behavior. PATCH tests also cover empty title, empty exercises, another user's private exercise, duplicate exercises, non-positive planned sets, adding a system exercise, a missing template id, and blocked access to another user's template with persisted data verified unchanged.

Workout-session integration tests now cover list/detail GET (including finished-session filtering, missing ids and user isolation) and POST creation (persisted ownership, exercise/sets, allowed system/private exercises, inaccessible templates/exercises, empty/duplicate exercises and negative reps/weight). A reusable `WorkoutSessionHelper` prepares sessions for tests. Duplicate exercise validation and negative set-value validation were added to the session POST endpoint. Both POST and PATCH now allow zero and null values for reps/weight.

## Remaining work

- [ ] Define the production-safe seed/admin strategy for creating and maintaining the system exercise catalog.
- [ ] Expand integration-test coverage to authentication flows, remaining exercise validation cases, remaining template edge cases, workout-session PATCH/finish/summary/add/delete and broader two-user ownership boundaries.
- [ ] Add workout-session POST/PATCH regression tests explicitly covering zero and null reps/weight values.
- [ ] Enforce finished-session immutability consistently (especially PATCH sets and POST add exercise), and prevent repeated finish from silently overwriting `FinishedAt`.
- [ ] Add explicit test-data isolation/reset strategy as the integration-test suite grows.
- [ ] Extend PATCH rejection assertions to verify persisted template exercises remain unchanged; keep the current rule rejecting duplicate exercises until the template data model supports duplicates.
- [ ] Clean up temporary authentication/debug behavior.
- [ ] Decide the production access-token storage model instead of treating `localStorage` as final by default.
- [ ] Optionally harden explicit logout when the backend is unreachable.
- [ ] Reset development/demo data before the production-ready release.

See [Authentication.md](Authentication.md) for the current authentication and authorization flow, [README.md](README.md) for the project status and [Api.md](Api.md) for the HTTP API.
