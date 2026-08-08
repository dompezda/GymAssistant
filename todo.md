# GymAssistant TODO — Authentication and User Ownership

This checklist describes the migration from the original single-user/demo model to authenticated, user-owned data.

## Authentication MVP

- [x] **Define the MVP scope.** Use email and password login. Exclude registration, roles, password reset, external providers and refresh tokens from the first working version.
- [x] **Add `ApplicationUser`.** Create an application user type derived from `IdentityUser<Guid>` and avoid introducing a second, duplicate `User` entity.
- [x] **Integrate Identity with the existing database context.** Use the Identity context variant without roles and preserve all current entity configurations.
- [x] **Configure Identity services and password rules.** Let `UserManager` and the Identity password hasher handle password creation and verification.
- [x] **Create and review the Identity migration.** Verify the generated tables, key types and changes before applying the migration.
- [x] **Add a development user seed.** Create the test user through `UserManager.CreateAsync`; load the development password from User Secrets and never commit it.
- [ ] **Audit entity ownership.** Workout templates and workout sessions are directly user-owned; system exercises are global and user exercises are private. Finish the exercise ownership/lineage rules before closing this item.
- [ ] **Add ownership to workout data.** Workout-template ownership is implemented. Workout-session ownership still needs to be assigned and enforced.
- [ ] **Extend the `Exercise` model.** Nullable exercise ownership exists as `OwnerUserId`, but the full system/private rules, lineage support and endpoint enforcement are still incomplete.
- [ ] **Add database constraints and indexes.** Enforce valid `IsSystem`/owner combinations and any final uniqueness/lineage constraints required by the exercise model.
- [ ] **Migrate or reset existing demo data.** Assign current data to the development user and initialize any final exercise ownership/lineage data, or reset the development database if the data is disposable.
- [x] **Add validated JWT configuration.** Configure issuer, audience, signing key and access-token lifetime; keep the signing key outside the repository.
- [x] **Configure JWT bearer authentication.** Validate signature, issuer, audience and expiration for every protected API request.
- [x] **Add an access-token service.** Generate JWTs with at least `sub` (`UserId`), `email`, `jti`, issued-at and expiration claims.
- [x] **Add a current-user abstraction.** `ICurrentUser` / `CurrentUser` read `UserId` from `HttpContext.User` in one place instead of parsing claims separately in every endpoint.
- [x] **Add authentication contracts and validation.** Create login and authenticated-user response contracts; return `400` for invalid input and a generic `401` for invalid credentials.
- [x] **Implement authentication endpoints.** Add `POST /api/auth/login` and protected `GET /api/auth/me` endpoints.
- [ ] **Protect all existing domain endpoints.** Workout-template endpoints are protected and filtered by authenticated `UserId`. Sessions, history and exercise ownership rules still need equivalent protection. System exercises must remain globally readable while private exercises remain user-scoped.
- [ ] **Add backend integration tests.** Cover successful and failed login, invalid JWTs, `/auth/me`, two-user data isolation and protection of system exercises.
- [ ] **Complete the frontend authentication flow.** Login, `AuthProvider`, startup session validation, `useAuth()`, protected routes and local logout are implemented. Still required: shared bearer-token handling, centralized `401` handling and cleanup of temporary auth debug code.

## After the MVP

- [ ] Add refresh-token persistence, rotation and an `HttpOnly` cookie.
- [ ] Add `POST /api/auth/refresh` and server-side `POST /api/auth/logout`.
- [ ] Retry one failed request after a successful token refresh once refresh tokens are implemented.
- [ ] Add registration, login rate limiting, account lockout and email confirmation as separate features.

## MVP completion criteria

The authentication MVP is complete when a user can log in, access only their own workout data plus system exercises, create records that are assigned by the backend to their account, and cannot access another user's resources even when their identifiers are known.
