# GymAssistant TODO — Authentication and User Ownership

This checklist describes the migration from the original single-user/demo model to authenticated, user-owned data.

## Authentication MVP

- [x] **Define the MVP scope.** Use email and password login. Exclude registration, roles, password reset, external providers and refresh tokens from the first working version.
- [x] **Add `ApplicationUser`.** Create an application user type derived from `IdentityUser<Guid>` and avoid introducing a second, duplicate `User` entity.
- [x] **Integrate Identity with the existing database context.** Use the Identity context variant without roles and preserve all current entity configurations.
- [x] **Configure Identity services and password rules.** Let `UserManager` and the Identity password hasher handle password creation and verification.
- [x] **Create and review the Identity migration.** Verify the generated tables, key types and changes before applying the migration.
- [x] **Add a development user seed.** Create the test user through `UserManager.CreateAsync`; load the development password from User Secrets and never commit it.
- [ ] **Audit entity ownership.** Decide which current entities are global, directly user-owned or owned through another aggregate.
- [ ] **Add ownership to workout data.** Assign `UserId` to workout templates and workout sessions where they are independently queried or created.
- [ ] **Extend the `Exercise` model.** Add nullable `UserId` and `LineageId`; keep system exercises global (`UserId == null`) and user exercises private (`UserId != null`).
- [ ] **Add database constraints and indexes.** Enforce valid `IsSystem`/`UserId` combinations and unique `(UserId, LineageId)` values for user exercises.
- [ ] **Migrate or reset existing demo data.** Assign current data to the development user and initialize `LineageId`, or reset the development database if the data is disposable.
- [x] **Add validated JWT configuration.** Configure issuer, audience, signing key and access-token lifetime; keep the signing key outside the repository.
- [x] **Configure JWT bearer authentication.** Validate signature, issuer, audience and expiration for every protected API request.
- [x] **Add an access-token service.** Generate JWTs with at least `sub` (`UserId`), `email`, `jti`, issued-at and expiration claims.
- [ ] **Add a current-user abstraction.** Read `UserId` from `HttpContext.User` in one place instead of parsing claims separately in every endpoint.
- [x] **Add authentication contracts and validation.** Create login and authenticated-user response contracts; return `400` for invalid input and a generic `401` for invalid credentials.
- [x] **Implement authentication endpoints.** Add `POST /api/auth/login` and protected `GET /api/auth/me` endpoints.
- [ ] **Protect all existing domain endpoints.** Filter templates, sessions, history and user exercises by the authenticated `UserId`; expose system exercises to everyone and never accept ownership fields from the frontend.
- [ ] **Add backend integration tests.** Cover successful and failed login, invalid JWTs, `/auth/me`, two-user data isolation and protection of system exercises.
- [ ] **Complete the frontend authentication flow.** Login page, authentication context and startup session validation are implemented. Still required: `useAuth()`, protected routes, shared bearer-token handling and local logout.

## After the MVP

- [ ] Add refresh-token persistence, rotation and an `HttpOnly` cookie.
- [ ] Add `POST /api/auth/refresh` and server-side `POST /api/auth/logout`.
- [ ] Restore the frontend session after a page refresh and retry one failed request after a successful token refresh.
- [ ] Add registration, login rate limiting, account lockout and email confirmation as separate features.

## MVP completion criteria

The authentication MVP is complete when a user can log in, access only their own workout data plus system exercises, create records that are assigned by the backend to their account, and cannot access another user's resources even when their identifiers are known.
