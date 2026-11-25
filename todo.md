# GymAssistant TODO — 1.X

Updated: 25 November 2026. Scope: 1.X only; no commitment to later major features or minor release dates. P0 blocks public release; P1 improves readiness and reliability; P2/P3 are flexible and not release prerequisites. Tests specific to a feature are part of its completion. Conditional/evaluation tasks do not imply a commitment to implement. Stable task IDs should be retained when closing or moving tasks.

| Priority | Open tasks |
| --- | ---: |
| P0 | 16 |
| P1 | 26 |
| P2 | 51 |
| P3 | 28 |
| Total | 121 |

Completed: 2 of 123 tracked tasks (P0-12, P1-11). The table counts open tasks only.

## Near-term order

P1-12 (promoted to P0; stable ID retained) → P0-02 → P0-01 → P0-08 → P0-03 → P0-05 → P0-04 → P0-06 → P0-07; then remaining P0 and closed beta (P1-28). Resolve dependencies and critical findings before exposing real user data.

## P0 — Critical / release blockers

- [ ] **P0-01** [Auth] Configure production SMTP (preferred provider: OVHcloud); verify Azure connectivity, TLS and secret storage.
- [ ] **P0-02** [Auth] Production cross-site cookie failure fixed using https://api.gymassistant.pl; same-site request and refresh 200 confirmed, repeated recovery after removing access token and logout/reload passed on 24 November. Remaining: test natural JWT expiry and explicitly verify refresh-cookie rotation (compare locally without exposing token values).
- [ ] **P0-03** [Auth] Verify password reset end to end with real email delivery; distinguish invalid tokens and password-policy errors.
- [ ] **P0-04** [Auth] Implement email confirmation: 24-hour token, resend, block login and refresh before confirmation, remove automatic login on registration, add creation timestamp and integration tests.
- [ ] **P0-05** [Auth] Remove the two existing test accounts before enabling confirmation; verify dependent data and revoke sessions.
- [ ] **P0-06** [Auth] Rate-limit authentication endpoints, including registration, login, password recovery and confirmation resend.
- [ ] **P0-07** [Auth] Prevent account enumeration, including inconsistent responses or timing on SMTP failure.
- [ ] **P0-08** [Auth] Persist Data Protection keys and verify Identity tokens survive API restarts and deployments.
- [ ] **P0-09** [Security] Review resource ownership and isolation; verify PATCH rejects a set ID belonging to another exercise or user.
- [ ] **P0-10** [Security] Review secrets, published artifacts (exclude appsettings.Development.json), CORS, HTTPS and production configuration.
- [ ] **P0-11** [Azure] Review SQL firewall rules and remove temporary access if still present.
- [x] **P0-12** [Azure] Configure cost budgets and alerts (24 November): subscription-wide EUR 20 monthly budget; actual 50/80/100%, forecast 100%, email recipient confirmed; SQL overage disabled and default payment method present. Alerts are not a spending cap.
- [ ] **P0-13** [Azure] Verify database backup availability and retention.
- [ ] **P0-14** [QA] Complete production smoke tests: register/login/logout, exercise/template creation, start/resume/discard/finish/history; test lost connectivity, partial writes, API failure and blocking UI defects.
- [ ] **P0-15** [Privacy] Prepare privacy policy and terms, make them available at registration, assess GDPR obligations, lawful bases, processors, health-data implications and retention.
- [ ] **P0-16** [Privacy/Product] Provide administrator contact, basic bug-report channel and a manual process for data-rights requests.

- [ ] **P1-12** [Azure; promoted to P0 on 24 November] Resolve production SQL connection timeouts after idle periods. On 25 November reproduced error -2 (12,010 ms post-login); Activity Log confirmed recurring pause/resume. Switched to Azure SQL Basic (5 DTU, 2 GB, USD 4.90/month); first login succeeded in 776 ms. Existing EnableRetryOnFailure retained; no timeout change. Remaining: verify login after prolonged inactivity and next morning, monitor errors and DTU; close only after verification.

## P1 — High / production stability

- [ ] **P1-01** [Auth] Implement self-service account deletion.
- [ ] **P1-02** [Auth] Implement password change for authenticated users.
- [ ] **P1-03** [Auth] Extend integration tests for refresh, logout, expired/revoked tokens and outstanding password-reset cases.
- [ ] **P1-04** [Auth] Review access-token storage in localStorage and XSS/CSRF tradeoffs.
- [ ] **P1-05** [Auth] Verify and improve logout behavior when the backend is unreachable.
- [ ] **P1-06** [Auth] Verify session revocation after password change/reset, including remaining JWT lifetime.
- [ ] **P1-07** [Security] Enforce one active workout per user against concurrent requests (consider a filtered unique SQL Server index).
- [ ] **P1-08** [Security] Review input validation: weights, reps, text lengths and collection limits.
- [ ] **P1-09** [Security] Verify deletion and cascade behavior across related entities.
- [ ] **P1-10** [Security] Verify production logs do not expose credentials, tokens or sensitive data.
- [x] **P1-11** [Azure] Upgrade F1 to B1 (one Linux instance, Poland Central), enable Always On, bind custom API domain with managed TLS (24 November).
- [ ] **P1-13** [Azure] Configure API monitoring and alerts for errors and downtime. Trace scopes enabled locally; verify production log retention and trace lookup.
- [ ] **P1-14** [Azure] Add a health-check endpoint.
- [ ] **P1-15** [Azure] Define and test production EF Core migration strategy.
- [ ] **P1-16** [Azure] Test database restoration.
- [ ] **P1-17** [Workout] Complete unsaved-change protection for SPA navigation and browser Back; existing beforeunload is not sufficient. Evaluate router migration only as needed.
- [ ] **P1-18** [Workout] Test authorization expiry during an active workout.
- [ ] **P1-19** [UI/UX] Verify main screens on physical Android/iOS devices, including keyboard, scrolling and active workout interactions.
- [ ] **P1-20** [API] Standardize API error responses (e.g. ProblemDetails). Partial: global unhandled exception response and trace logging implemented in `927a1a7`; integration test passed locally (owner report). Remaining: remove temporary Development-only test endpoint, verify deployment and align expected error responses across endpoints.
- [ ] **P1-21** [Frontend] Distinguish network, validation and server errors; handle timeouts, safe manual retries and reconnection.
- [ ] **P1-22** [Frontend] Prevent duplicate submissions across forms, including template editing.
- [ ] **P1-23** [Tests] Extend backend integration tests for remaining ownership, validation and concurrency cases.
- [ ] **P1-24** [Tests] Isolate/reset test data between integration tests.
- [ ] **P1-25** [Tests] Test selected SQL Server-specific behavior using a real SQL Server instance (e.g. Testcontainers).
- [ ] **P1-26** [Privacy] Provide user-data export; ensure applicable rights can be fulfilled manually before automation.
- [ ] **P1-27** [Privacy] Define incident and personal-data breach response procedure.
- [ ] **P1-28** [Product] Recruit 3–5 testers, run closed beta, collect feedback after real workouts and fix critical issues.

## P2 — Normal / product improvements

- [ ] **P2-01** [Auth] Allow email-address change with verification.
- [ ] **P2-02** [Auth] Allow users to manage and revoke sessions on other devices.
- [ ] **P2-03** [Auth] Send security notifications for account changes.
- [ ] **P2-04** [Auth] Unify authentication and account settings UI.
- [ ] **P2-05** [Auth] Add show/hide password controls.
- [ ] **P2-06** [Security] Review security headers including CSP.
- [ ] **P2-07** [Security] Automate dependency vulnerability checks (e.g. Dependabot).
- [ ] **P2-08** [Azure] Evaluate a separate staging environment.
- [ ] **P2-09** [Azure] Define deployment rollback strategy.
- [ ] **P2-10** [Azure] Establish versioning, Git tags and GitHub Releases; expose the application version and short build commit SHA (injected by CI at build time) on the login screen and main menu. Distinguish frontend and backend build identifiers if they can be deployed independently.
- [ ] **P2-11** [Exercises] Define ongoing system exercise catalog update/versioning strategy.
- [ ] **P2-12** [Exercises] Add GET /api/exercises/{exerciseId}/last-workout: latest completed, non-discarded session for the current user, including date, sets, weights and reps; define no-history response.
- [ ] **P2-13** [Exercises] Allow private exercise deletion while preserving historical records.
- [ ] **P2-14** [Exercises] Evaluate favorites and system catalog visibility (design decision pending).
- [ ] **P2-15** [Exercises] Filter exercises by muscle groups.
- [ ] **P2-16** [Exercises] Design and implement tracking types for weight/reps, bodyweight, additional load, assistance, duration and distance; adapt data model, validation, DTOs, dynamic set UI, history, previous-value copying and existing catalog migration; preserve historical semantics.
- [ ] **P2-17** [Templates] Unify create/edit flows using shared exercise picker.
- [ ] **P2-18** [Templates] Reorder exercises in templates.
- [ ] **P2-19** [Templates] Delete templates without losing workout history.
- [ ] **P2-20** [Templates] Duplicate templates.
- [ ] **P2-21** [Workout] Show completion indicator derived from filled sets; avoid color-only indication.
- [ ] **P2-22** [Workout] Show previous workout sets in active exercise view (depends on P2-12).
- [ ] **P2-23** [Workout] Copy previous weights and reps into current session (depends on P2-12).
- [ ] **P2-24** [Workout] Add rest timer between sets.
- [ ] **P2-25** [Workout] Reorder exercises in an active session and persist order.
- [ ] **P2-26** [Workout] Restore or intentionally replace swipe navigation; handle input/button gesture origins, cancellation and multitouch.
- [ ] **P2-27** [History] Delete completed workout sessions.
- [ ] **P2-28** [History] Redesign workout summary: compact read-only exercise list, drill-down into historical sets and concise session metrics; reuse GET /api/workout-sessions/{id}, not a new summary endpoint.
- [ ] **P2-29** [History] Filter workout history by date range.
- [ ] **P2-30** [History] Improve exercise history formatting, null values, spacing and empty/error navigation.
- [ ] **P2-31** [UI/UX] Unify buttons, forms, modals, cards, headers, spacing and typography.
- [ ] **P2-32** [UI/UX] Standardize loading, saving, disabled and empty states.
- [ ] **P2-33** [UI/UX] Confirm destructive operations.
- [ ] **P2-34** [UI/UX] Improve layout on larger screens.
- [ ] **P2-35** [UI/UX] Add installable PWA support (not offline synchronization).
- [ ] **P2-36** [Frontend] Add shared user notification mechanism.
- [ ] **P2-37** [Frontend] Inform users when connectivity is unavailable.
- [ ] **P2-38** [Frontend] Add React Error Boundary.
- [ ] **P2-39** [Frontend] Add a useful 404 page.
- [ ] **P2-40** [Tests] Introduce frontend unit/component tests for sets, dirtyIds and fetchApi.
- [ ] **P2-41** [Tests] Add automated end-to-end tests for main flows.
- [ ] **P2-42** [Tests] Report code coverage in CI.
- [ ] **P2-43** [Tech debt] Organize CSS by responsibility and remove cross-feature selectors and duplication.
- [ ] **P2-44** [Tech debt] Extract shared TypeScript API response types.
- [ ] **P2-45** [Tech debt] Separate API request logic from components where justified.
- [ ] **P2-46** [Privacy] Version legal documents and notify users of material changes.
- [ ] **P2-47** [Product] Create landing page with features, screenshots and register/login actions.
- [ ] **P2-48** [Product] Add in-app bug and suggestion reporting.
- [ ] **P2-49** [Product] Add privacy-conscious usage analytics.
- [ ] **P2-50** [Product] Promote on LinkedIn, Reddit and relevant training communities.
- [ ] **P2-51** [Product] Prepare public release (after P0 and beta readiness; other P2/P3 items are not prerequisites).

## P3 — Low / optional and technical improvements

- [ ] **P3-01** [Auth] Automatically delete unconfirmed accounts after seven days; avoid deleting accounts with existing training data, handle confirmation races and schedule reliably.
- [ ] **P3-02** [Auth] Clean up expired and revoked refresh tokens.
- [ ] **P3-03** [Auth] Consider security-event history.
- [ ] **P3-04** [Azure] Evaluate Infrastructure as Code (Bicep or Terraform).
- [ ] **P3-05** [Azure] Automate environment configuration.
- [ ] **P3-06** [Azure] Optimize infrastructure costs and performance as needed.
- [ ] **P3-07** [Exercises] Expand system exercise library.
- [ ] **P3-08** [Exercises] Optimize search when catalog growth warrants it.
- [ ] **P3-09** [Templates] Protect unsaved template-editor changes.
- [ ] **P3-10** [Workout] Evaluate automatic creation of an empty set after filling the last one.
- [ ] **P3-11** [Workout] Evaluate allowing the same exercise multiple times per session.
- [ ] **P3-12** [History] Add pagination when record volume warrants it.
- [ ] **P3-13** [UI/UX] Improve accessibility (critical accessibility defects take higher priority).
- [ ] **P3-14** [UI/UX] Evaluate light/dark themes.
- [ ] **P3-15** [UI/UX] Consider subtle animations and transitions.
- [ ] **P3-16** [Frontend] Consider automatic retries only for safe/idempotent operations.
- [ ] **P3-17** [Frontend] Centralize duplicated error-handling logic.
- [ ] **P3-18** [Tests] Standardize test names and helpers as needed.
- [ ] **P3-19** [Tests] Improve ESLint and code formatting configuration.
- [ ] **P3-20** [Tech debt] Decompose large React components and extract custom hooks where justified.
- [ ] **P3-21** [Tech debt] Review large backend endpoints and extract business logic where justified.
- [ ] **P3-22** [Tech debt] Remove unused code, styles and comments.
- [ ] **P3-23** [Tech debt] Standardize file, component and function naming.
- [ ] **P3-24** [Tech debt] Review EF Core query performance.
- [ ] **P3-25** [Tech debt] Maintain dependencies.
- [ ] **P3-26** [Privacy] Automate data-rights request handling if warranted.
- [ ] **P3-27** [Product] Add FAQ and short usage instructions.
- [ ] **P3-28** [Product] Consider an About page with application version/build identifier, user-facing release notes, curated planned improvements (not internal security tasks or promised dates), and contact/feedback links. Reuse P2-10 version metadata and the contact channel from P0-16.

## Completed foundations (not included in open count)

- Backend (.NET 10) on Azure B1 with Always On, Azure SQL, Managed Identity, 100 seeded system exercises.
- React frontend, custom domain https://gymassistant.pl/, HTTPS and redirects (reported working 23 November).
- GitHub Actions CI and automated frontend/backend deployment (backend CD confirmed by owner on 24 November).
- Registration/login, JWT and refresh rotation, local password-reset implementation, workout lifecycle and exercise history; production verification remains open above.

See [DEPLOYMENT.md](DEPLOYMENT.md) for current infrastructure and cost controls, [CHANGELOG.md](CHANGELOG.md) for historical development notes and [Authentication.md](Authentication.md), [Api.md](Api.md) for implementation details.
