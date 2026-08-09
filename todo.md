# GymAssistant TODO — Authentication and User Ownership

This checklist describes the migration from the original single-user/demo model to authenticated, user-owned data.

## Authentication MVP

- [x] **Define the MVP scope.** Use email and password login. Exclude registration, roles, password reset, external providers and refresh tokens from the first working version.
- [x] **Add `ApplicationUser`.** Create an application user type derived from `IdentityUser<Guid>` and avoid introducing a second, duplicate `User` entity.
- [x] **Integrate Identity with the existing database context.** Use the Identity context variant without roles and preserve all current entity configurations.
- [x] **Configure Identity services and password rules.** Let `UserManager` and the Identity password hasher handle password creation and verification.
- [x] **Create and review the Identity migration.** Verify the generated tables, key types and changes before applying the migration.
- [x] **Add a development user seed.** Create the test user through `UserManager.CreateAsync`; load the development password from User Secrets and never commit it.
- [x] **Add ownership to workout data.** Workout templates and workout sessions receive `UserId` from `ICurrentUser`; ownership is not accepted from frontend requests.
- [ ] **Finalize exercise ownership rules.** Runtime access rules are implemented: system exercises are global and private exercises are scoped by `OwnerUserId`. The temporary UI/API ability for any authenticated user to toggle `IsSystem` still needs to be replaced with a production-safe seed/admin strategy.
- [ ] **Add database constraints and indexes.** Enforce the invariant that system exercises have `OwnerUserId = null` and private exercises have a non-null owner. Add final indexes/uniqueness rules once the exercise model is settled.
- [ ] **Migrate or reset existing demo data.** Existing development records predate the final ownership rules. Reset the disposable development database or explicitly migrate the data after the model is finalized.
- [x] **Add validated JWT configuration.** Configure issuer, audience, signing key and access-token lifetime; keep the signing key outside the repository.
- [x] **Configure JWT bearer authentication.** Validate signature, issuer, audience and expiration for every protected API request.
- [x] **Add an access-token service.** Generate JWTs with at least `sub` (`UserId`), `email`, `jti`, issued-at and expiration claims.
- [x] **Add a current-user abstraction.** `ICurrentUser` / `CurrentUser` read `UserId` from `HttpContext.User` in one place instead of parsing claims separately in every endpoint.
- [x] **Add authentication contracts and validation.** Create login and authenticated-user response contracts; return `400` for invalid input and a generic `401` for invalid credentials.
- [x] **Implement authentication endpoints.** Add `POST /api/auth/login` and protected `GET /api/auth/me` endpoints.
- [x] **Protect existing domain endpoints.** Workout templates, workout sessions, exercises and muscle groups require authorization. User-owned queries are filtered by the authenticated user; system exercises remain globally readable.
- [x] **Add shared bearer-token handling on the frontend.** `fetchApi` wraps `fetch`, preserves request options/headers and adds `Authorization: Bearer <token>` when an access token exists.
- [ ] **Centralize `401 Unauthorized` handling.** Domain requests should clear stale authentication state and return the app to the existing login/auth flow when the access token expires or becomes invalid.
- [ ] **Remove temporary auth/debug behavior.** Remove development logging and any temporary authentication diagnostics before considering the MVP portfolio-ready.
- [ ] **Add backend integration tests.** Cover successful and failed login, invalid JWTs, `/auth/me`, protected endpoints, two-user data isolation, system exercises and attempts to access another user's resource by id.
- [ ] **Run a clean two-user acceptance test.** Create data for two users and verify templates, sessions, private exercises and exercise history cannot cross account boundaries while system exercises remain visible to both users.

## Current authorization rules

### Workout templates

- authenticated user sees only their own templates,
- new templates receive `UserId` from `ICurrentUser`,
- detail and update endpoints require template ownership,
- templates may contain system exercises or private exercises owned by the authenticated user.

### Workout sessions

- authenticated user sees and modifies only their own sessions,
- new sessions receive `UserId` from `ICurrentUser`,
- a referenced workout template must belong to the authenticated user,
- session exercises may reference system exercises or private exercises owned by the authenticated user,
- session summary, finish, set updates, exercise add/delete and history access are scoped through the owning workout session.

### Exercises

- exercise list contains system exercises plus private exercises owned by the authenticated user,
- exercise history is available only for an accessible exercise and is built only from the authenticated user's workout sessions,
- during development, `isSystem` is still accepted by create/edit requests so both ownership modes can be tested,
- current development invariant: `IsSystem = true` means `OwnerUserId = null`; `IsSystem = false` means `OwnerUserId = currentUser.UserId`.

## After the MVP

- [ ] Add refresh-token persistence, rotation and an `HttpOnly` cookie.
- [ ] Add `POST /api/auth/refresh` and server-side `POST /api/auth/logout`.
- [ ] Retry one failed request after a successful token refresh once refresh tokens are implemented.
- [ ] Add registration, login rate limiting, account lockout and email confirmation as separate features.
- [ ] Introduce a dedicated admin/seed flow if system exercises need to be managed outside application startup data.

## MVP completion criteria

The authentication MVP is complete when a user can log in, access only their own workout data plus system exercises, create records that are assigned by the backend to their account, cannot access another user's resources even when their identifiers are known, and stale/expired access tokens are handled consistently by the frontend.
