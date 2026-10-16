# GymAssistant TODO

The core authentication flow is implemented: public registration, login, persisted refresh tokens, refresh-token rotation, centralized frontend `401 -> refresh -> retry once`, startup session recovery and server-side logout/revocation.

Registration is now end-to-end: ASP.NET Core Identity enforces unique email addresses and the configured password policy, the frontend has a `/register` page with password confirmation and validation-error display, successful registration reuses the existing `login()` flow for automatic sign-in, and login/register pages link to each other.

Configuration cleanup for the current auth/frontend flow is also complete: the frontend API base URL is centralized through `VITE_API_URL`, refresh-token lifetime is configured and validated through `RefreshTokenOptions`, the credential-enabled CORS origin is read from backend configuration, and OpenAPI plus Scalar are mapped in Development for manual API testing.

System/private exercise ownership is now enforced: normal users create and edit only their own private exercises, `IsSystem` is not accepted from create/edit requests, and SQL Server enforces valid `IsSystem` / `OwnerUserId` combinations through a check constraint. The frontend marks system exercises with a `System` badge in both the exercise picker and the main exercise list, and system exercise details are presented in a read-only mode without an edit action.

Backend integration testing is bootstrapped with xUnit and `WebApplicationFactory`. The test host replaces the normal SQL Server `AppDbContext` configuration with SQLite in-memory and creates the schema for the test host. Current exercise coverage verifies unauthenticated `401 Unauthorized`, two-user private-exercise isolation, visibility of the current user's private exercises together with system exercises, ownership and `IsSystem = false` when creating a private exercise, `400 Bad Request` for an empty exercise name, successful editing of the current user's private exercise including muscle-group changes, blocked edits of another user's or a system exercise with persisted data verified unchanged, muscle-group patch behavior without duplicate relations and with only missing relations added, system-exercise history filtered to the authenticated user's workout sessions, and `404 Not Found` when requesting history for another user's private exercise. Authenticated cases use real JWT bearer tokens generated for Identity users created by the test setup. Reusable `TestHelpers` now centralize common user creation, client authorization, private/system exercise setup (including muscle groups) and workout-template setup so individual tests stay focused on behavior. Workout-template integration tests now cover create validation and ownership, list/detail user isolation, order normalization and successful PATCH behavior. PATCH tests also cover empty title, empty exercises, another user's private exercise, duplicate exercises, non-positive planned sets, adding a system exercise, a missing template id, and blocked access to another user's template with persisted data verified unchanged.

Workout-session integration coverage now includes GET list/detail, owned and non-owned requests, session creation and validation, set PATCH (including finished-session blocking), finish success/idempotency, exercise POST (valid/invalid, duplicate and ownership cases) and DELETE (removal, cascade deletion, order renumbering, ownership and lifecycle rules). Details GET also returns `StartedAt` and `FinishedAt` and replaces the removed backend `/summary` endpoint; the React summary view still exists. The workout-session test file is organized by GET, POST, PATCH and DELETE. Final suite execution should be rerun locally after removal of stale summary tests.

Frontend update (13 October 2026): the active workout session uses a compact four-column set layout, numeric mobile keyboards, a contextual exercise-delete dropdown and horizontal swipe between exercises. Swipe is limited to the exercise header/card area and ignores predominantly vertical movement. Gesture-origin filtering, touch cancellation/multitouch handling and real-device verification are still outstanding.

Frontend update (15 October 2026): active-workout write operations and exercise navigation now share a component-local `isSavingRef` guard with `isSaving` used to disable interactive controls. `saveCurrentExerciseIfDirty()` saves an edited exercise before switching to another exercise or adding a new one. Finishing uses `Promise.allSettled()` to remove successful exercise IDs from `dirtyIds`, retain failed ones for retry, and avoid calling `/finish` until the exercise writes succeed. After a successful finish, React navigates to `/workout-sessions/:id/summary`, which uses the existing session-details GET. Manual DevTools throttling (200 kb/s, 5000 ms latency) confirmed disabled UI during PATCH/DELETE and no duplicate PATCH on repeated navigation clicks; explicit failure-path tests and the full build/test suite are still pending.

Frontend update (16 October 2026): the active workout screen now displays errors for failed exercise PATCH/POST/DELETE, finish operations and session GET requests instead of reporting failures only in the console. Failed writes do not advance exercise navigation or finish the workout, and unsuccessful exercise saves remain marked dirty for retry. A manual session refresh action reloads backend state, asks for confirmation before discarding locally edited sets, and clears dirty IDs only after a successful GET. Initial loading failures offer retry, and empty-session fallback UI offers refresh/navigation. This is a manual recovery path, not offline caching or automatic conflict resolution; failure and production-build verification remain part of release QA.

## Roadmap

### Must-have — before first public release (v1.0)

#### Error handling and data safety
- [ ] Standardize user-visible API error presentation across the app and agree on a backend error response contract (active workout GET/PATCH/POST/DELETE/finish feedback is implemented).
- [ ] Complete failure-path verification for PATCH/POST/DELETE/finish and session refresh, including network failures and HTTP errors; active-workout guards and retry behavior are implemented.
- [ ] Protect unsaved workout edits when leaving the page or reloading it.
- [ ] Review the PATCH edge case where a set ID belongs to another workout-session exercise.

#### UI/UX consistency
- [ ] Unify buttons, forms, modals, spacing and feedback patterns across screens.
- [ ] Make loading, saving and disabled states consistent.
- [ ] Ignore swipes starting on inputs, buttons and other interactive controls; handle touch cancellation and multitouch.
- [ ] Verify swipe, set editing, exercise menu and responsive layout on a physical mobile device and narrow viewports.

#### Authentication and release quality
- [ ] Add authentication integration tests: register, login, logout, refresh rotation, expired/revoked token rejection and unauthorized access.
- [ ] Run the complete backend test suite after test cleanup and verify the frontend production build.
- [ ] Clean up temporary authentication/debug behavior and review access-token storage (`localStorage` is not a production decision).
- [ ] Define a production-safe strategy to seed/manage system exercises.
- [ ] Reset development/demo data before the public release.

#### Azure deployment
- [ ] Deploy .NET API, React frontend and production SQL database on Azure.
- [ ] Configure production secrets, HTTPS, CORS, refresh-token cookies, migrations and environment variables.
- [ ] Smoke-test the full register → login → workout → finish → history flow in the deployed environment.
- [ ] Add the public demo URL and deployment notes to README.

### Nice-to-have — after v1.0 (v1.1)
- [ ] Improve exercise history: show previous sessions, dates, sets, weights and repetitions clearly (the exercise-history API and initial UI already exist).
- [ ] Show the last recorded sets for the same exercise on the active workout card; optionally allow copying previous values.
- [ ] Add GitHub Actions CI (build + tests) and later automated Azure deployment (CD).
- [ ] Extend integration coverage for remaining exercise/template and ownership edge cases.
- [ ] Introduce explicit test-data isolation/reset for the growing integration suite.
- [ ] Add frontend tests/E2E for the main workout flow.
- [ ] Optionally improve logout behavior when the backend cannot be reached.
- [ ] Decide whether duplicate exercises should remain forbidden within a single workout session (currently rejected).

### Future — larger product features (v2.0+)
- [ ] Trainer mode: trainer/trainee roles, assigning workout plans and reviewing trainee sessions.
- [ ] Workout plan sharing: private/public sharing and importing/copying plans.
- [ ] Progress charts: weight and volume trends, personal records and weekly/monthly statistics.

### Completed milestones
- [x] Add active-workout request error messages, manual backend session refresh with unsaved-change confirmation, and GET retry/empty-session fallback UI.
- [x] Consolidate workout-session details and summary data into `GET /api/workout-sessions/{id}`; remove the unused backend summary endpoint and tests.
- [x] Enforce finished-session immutability in exercise PATCH/POST/DELETE, and cover finish idempotency.
- [x] Add exercise POST/DELETE integration tests for the core success, validation, ordering and access-control cases.
- [x] Improve the active workout-session set layout and add a compact exercise-delete menu.
- [x] Implement initial horizontal swipe navigation between workout-session exercises.
- [x] Prevent overlapping active-workout write operations and rapid exercise-navigation requests using `isSavingRef`; disable relevant controls with `isSaving`.
- [x] Keep `markExerciseDirty()` calls outside `setSession()` updater callbacks and avoid duplicate dirty marking.

See [Authentication.md](Authentication.md) for the authentication and authorization design, [README.md](README.md) for project status and [Api.md](Api.md) for the HTTP API.
