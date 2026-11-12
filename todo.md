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

Frontend update (28 October 2026): exercise create/edit/read-only modals received UI spacing and alignment improvements, consistent action-button layout, and optional history navigation from the shared `ExerciseForm`. Create/edit modals lock background vertical scrolling while open, restoring the previous value in effect cleanup. The shared form now uses `isSavingRef` to prevent rapid duplicate submits and `isSaving` to disable save, close and history buttons during submission. Manual mobile checks covered the modal and scroll behavior; automated frontend tests and robust network-error handling remain outstanding. The modal CSS has since been updated to use `max-width: 400px` and `max-height: 90dvh`; scroll access was manually checked at narrow viewport heights on 6 November 2026.

Frontend/backend update (3 November 2026): workout sessions now have nullable `DiscardedAt`, with a database migration and mutually exclusive active/finished/discarded lifecycle checks in the API. The start-workout page checks `GET /api/workout-sessions/active`, allows resuming the existing workout, and confirms discard through `POST /api/workout-sessions/{sessionId}/discard`; the backend rejects creating another active session (`409`) and blocks editing/finishing discarded sessions. Completed workout lists and exercise history exclude discarded and active sessions. Integration tests cover active lookup, discard ownership/idempotency, blocked writes and creating after discard. The exercise-history assertion was subsequently corrected (100 backend tests passed locally on 3 November 2026). Backend `dotnet test` passed locally again on 11 November 2026 (user reported); frontend production build and final manual end-to-end verification remain pending. A filtered unique SQL Server index may later be needed to enforce one active session against concurrent POST requests.

Frontend UI update (6 November 2026): login and registration now use a shared `.auth-page` layout; the main menu prioritizes the start-workout action; the exercise library uses shared `.page-header`, `.page-title` and `.back-button` styles. Exercise create/edit/read-only dialogs now share `.modal-header`, `.modal-title`, `.modal-body` and `.modal-component` styling; the muscle-group checkbox grid and action spacing were refined. Manual responsive checks verified modal scrolling at 375 × 400 and 375 × 600 viewports. This completes a UI pass for the exercise section, not the app-wide consistency task; other views still require review.

Frontend UI update (7 November 2026): workout-template list/detail screens received responsive layout adjustments, and the template detail description is clamped to four lines until expanded. The workout-template edit screen now uses a height-constrained flex layout with independently scrollable form content and an always-visible Save action. It renders only exercises selected for the template, uses the existing `ExercisePickerModal` to select library exercises and `CreateExerciseModal` to create and immediately add a new one, and coordinates modal visibility through a single `ActiveModal` union state. Each selected exercise has explicit remove and planned-set controls (minimum one set); picker selection checks for duplicate exercise IDs. No new backend endpoint or integration test was added for this UI refactor. Remaining work: bring template creation to the same selection flow; consider large-library favorites, picker loading/error feedback, template PATCH submission guards and narrow-screen/mobile keyboard verification. Search/filtering and open-only picker fetch have since been implemented.

Authentication update (9 November 2026): implemented public `POST /api/auth/forgot-password` and `POST /api/auth/reset-password` using ASP.NET Core Identity token providers, `IEmailSender` / MailKit SMTP, `SmtpOptions` and development Mailpit. React now includes `/forgot-password` and `/reset-password` pages, email links and password requirements. The happy path was manually verified end-to-end; integration tests now cover several negative cases, while production delivery and additional coverage remain open. Mailpit is local-only, not a production mail provider.

Frontend update (12 November 2026, v1.1 in progress): the active workout UI has been split into an exercise list in `WorkoutSessionPage` and a separate `WorkoutSessionExerciseDetails` component for entering sets, weights and repetitions. Selection is tracked by `selectedExerciseId`, and the selected exercise is derived from the parent session state rather than duplicated. The parent still owns `session`, `dirtyIds`, the save-in-flight guard and API writes; the details component updates sets through `setSession` and reports edits through `onExerciseChanged`. Returning from details calls the parent's dirty-only PATCH flow, keeping the details view open on save failure. The exercise list exposes add/remove actions (last exercise cannot be deleted by backend rule), while session finish and refresh are shown on the list rather than in details. JSX/CSS for both views was adjusted using scoped workout selectors to avoid changing shared styles. **Not complete:** horizontal swipe navigation was removed during this refactor and must be restored or deliberately replaced; PATCH/error-path behavior, mobile scrolling and full regression checks still need verification. Existing descriptions of swipe and the earlier single-screen workout layout refer to the pre-refactor implementation. No backend endpoint or data-model change was required.

## Roadmap

### Must-have — before first public release (v1.0)

#### Error handling and data safety
- [ ] Standardize user-visible API error presentation across the app and agree on a backend error response contract (active workout GET/PATCH/POST/DELETE/finish feedback is implemented).
- [ ] Complete failure-path verification for PATCH/POST/DELETE/finish and session refresh, including network failures and HTTP errors; active-workout guards and retry behavior are implemented.
- [ ] Add a minimal unsaved-changes warning when leaving or reloading an active workout if `dirtyIds.length > 0` (including browser reload/close and in-app navigation). Existing save-before-exercise-switch and save-before-finish behavior remains unchanged; no autosave or offline persistence is required for v1.0. This is the agreed scope compromise: warn about potential loss of locally edited sets rather than redesigning session persistence.
- [ ] Review the PATCH edge case where a set ID belongs to another workout-session exercise.
- [ ] Consider a SQL Server filtered unique index for one active workout per user to protect against concurrent creation requests.

#### UI/UX consistency
- [ ] Unify buttons, forms, modals, spacing and feedback patterns across screens.
- [ ] Bring template creation onto the shared exercise picker flow and improve selection visibility for large catalogs. The shared ExercisePickerModal now has client-side name search, empty results and a fixed-height scrollable list.
- [ ] Add template-editor save-in-flight protection and user-visible request/network error handling; validate mobile scrolling and keyboard behavior.
- [ ] Make loading, saving and disabled states consistent.
- [ ] After restoring swipe in the v1.1 exercise-list/details flow, ignore gestures starting on inputs/buttons and handle touch cancellation and multitouch.
- [ ] Verify set editing, exercise-list add/remove, return-to-list PATCH/error handling and responsive layout on a physical mobile device; verify swipe again after its restoration.

#### Authentication and release quality
- [ ] Complete remaining authentication integration coverage: logout, refresh rotation, expired/revoked token rejection, unauthorized access, and outstanding reset-password negative cases (e.g. expired token, password policy and missing fields). `AuthEndpointsTests` already covers register/login, forgot-password and reset-password success, invalid/reused reset tokens, and refresh-token revocation, using `FakeEmailSender`.
- [ ] Prepare production email delivery: real SMTP provider, credentials stored outside source control, TLS/STARTTLS, delivery-failure behavior, rate limits and logging that does not expose tokens.
- [ ] Standardize password-reset error responses; distinguish password-policy failures from invalid tokens and show actionable feedback in React. Duplicate password-reset submissions are already guarded in the frontend.
- [ ] Verify the frontend production build (`npm run build`) and complete final end-to-end smoke tests (registration/login/reset/logout, exercise/template creation, workout start/resume/discard/finish and history). User reported `dotnet test` passing locally on 11 November 2026; no frontend build result has been confirmed.
- [ ] Clean up temporary authentication/debug behavior and review access-token storage (`localStorage` is not a production decision).
- [ ] Define a production-safe strategy to seed/manage system exercises.
- [ ] Reset development/demo data before the public release.

#### Azure deployment
- [ ] Deploy .NET API, React frontend and production SQL database on Azure.
- [ ] Configure production secrets, HTTPS, CORS, refresh-token cookies, migrations and environment variables.
- [ ] Smoke-test the full register → login → workout → finish → history flow in the deployed environment.
- [ ] Add the public demo URL and deployment notes to README.

### Nice-to-have — after v1.0 (v1.1)
- [x] Introduce separate active-session exercise list and set-entry details component, with add/remove actions on the list (12 November 2026; UI and regression verification ongoing).
- [ ] Restore or consciously replace quick swipe navigation lost in the 12 November split; keep gestures away from editable inputs and actions.
- [x] Move set-entry UI/handlers out of the large `WorkoutSessionPage`, keep parent-owned session state, `dirtyIds`, dirty-only PATCH on return and request guards (12 November 2026; manual failure-path regression checks pending).
- [ ] Allow reordering exercises in a session (drag and drop); explicitly define order persistence and reconcile frontend display order with backend-assigned order.
- [ ] Evaluate auto-appending an empty set when the last set is filled. Decide trigger rules, persistence semantics and how to avoid accidental extra sets before implementing; retain manual add until validated.
- [ ] Organize CSS by responsibility (global tokens/base layout, shared components, auth and workout-specific styles), reduce duplicated selectors and remove cross-feature dependencies such as `.templates-page` outside templates. Do this as a separate regression-tested refactor.
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
- [x] Split active workout into selectable exercise list and separate set-entry details view, with scoped styling and parent-controlled save-on-return (12 November 2026; swipe restoration and regression QA remain open).
- [x] Improve the start-workout and select-template mobile UI, use accessible `Link` cards to select a plan, and align session setup cards with workout-template edit cards (11 November 2026).
- [x] Add ExercisePickerModal client-side search, empty-result messaging, open-only loading and a stable scrollable picker layout (11 November 2026).
- [x] Update workout-session setup with shared add-exercise handling, duplicate checks, and guarded POST submission with error display (11 November 2026).
- [x] Reorganize active workout mobile layout into a persistent header, scrollable set-entry area, and visible navigation/add/finish actions; further polish and regression smoke tests remain (11 November 2026).
- [x] Add authentication integration coverage for register/login, forgot/reset password and successful reset-token invalidation/revocation using FakeEmailSender (10 November 2026); other auth coverage remains outstanding.
- [x] Implement local password-reset flow with MailKit, Identity token generation/verification, Mailpit and two React pages; manually verify successful reset (9 November 2026). Production email and additional automated edge-case tests remain open.
- [x] Refactor workout-template editing to show selected exercises only, add the exercise picker/create flow, and keep the save action visible with a scrollable form (7 November 2026).
- [x] Add active workout resume/discard flow, nullable `DiscardedAt` migration, active-session creation guard and lifecycle restrictions.
- [x] Filter completed-workout lists and exercise history to finished, non-discarded sessions, add lifecycle integration scenarios, and correct the exercise-history assertion.
- [x] Improve exercise create/edit modal layout, action consistency, background scroll locking and duplicate-submit prevention.
- [x] Unify login/registration page spacing, main-menu action priority, exercise page headers and exercise dialog layout (6 November 2026).
- [x] Add active-workout request error messages, manual backend session refresh with unsaved-change confirmation, and GET retry/empty-session fallback UI.
- [x] Consolidate workout-session details and summary data into `GET /api/workout-sessions/{id}`; remove the unused backend summary endpoint and tests.
- [x] Enforce finished-session immutability in exercise PATCH/POST/DELETE, and cover finish idempotency.
- [x] Add exercise POST/DELETE integration tests for the core success, validation, ordering and access-control cases.
- [x] Improve the active workout-session set layout and add a compact exercise-delete menu.
- [x] Implement initial horizontal swipe navigation between workout-session exercises (historical milestone; removed by the 12 November 2026 refactor, restoration tracked under v1.1).
- [x] Prevent overlapping active-workout write operations and rapid exercise-navigation requests using `isSavingRef`; disable relevant controls with `isSaving`.
- [x] Keep `markExerciseDirty()` calls outside `setSession()` updater callbacks and avoid duplicate dirty marking.

See [Authentication.md](Authentication.md) for the authentication and authorization design, [README.md](README.md) for project status and [Api.md](Api.md) for the HTTP API.
