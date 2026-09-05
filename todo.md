# GymAssistant TODO

The core authentication flow is implemented: public registration, login, persisted refresh tokens, refresh-token rotation, centralized frontend `401 -> refresh -> retry once`, startup session recovery and server-side logout/revocation.

Registration is now end-to-end: ASP.NET Core Identity enforces unique email addresses and the configured password policy, the frontend has a `/register` page with password confirmation and validation-error display, successful registration reuses the existing `login()` flow for automatic sign-in, and login/register pages link to each other.

Configuration cleanup for the current auth/frontend flow is also complete: the frontend API base URL is centralized through `VITE_API_URL`, refresh-token lifetime is configured and validated through `RefreshTokenOptions`, the credential-enabled CORS origin is read from backend configuration, and OpenAPI plus Scalar are mapped in Development for manual API testing.

System/private exercise ownership is now enforced: normal users create and edit only their own private exercises, `IsSystem` is not accepted from create/edit requests, and SQL Server enforces valid `IsSystem` / `OwnerUserId` combinations through a check constraint. The frontend marks system exercises with a `System` badge in both the exercise picker and the main exercise list, and system exercise details are presented in a read-only mode without an edit action.

Backend integration testing is now bootstrapped with xUnit and `WebApplicationFactory`. The test host replaces the normal SQL Server `AppDbContext` configuration with SQLite in-memory, creates the schema for the test host and currently covers unauthenticated access to `/api/exercises` plus two-user private-exercise visibility isolation with a real JWT bearer token.

## Remaining work

- [ ] Define the production-safe seed/admin strategy for creating and maintaining the system exercise catalog.
- [ ] Expand integration-test coverage to authentication flows and additional two-user ownership boundaries.
- [ ] Add explicit test-data isolation/reset strategy as the integration-test suite grows.
- [ ] Clean up temporary authentication/debug behavior.
- [ ] Decide the production access-token storage model instead of treating `localStorage` as final by default.
- [ ] Optionally harden explicit logout when the backend is unreachable.
- [ ] Reset development/demo data before the production-ready release.

See [Authentication.md](Authentication.md) for the current authentication and authorization flow, [README.md](README.md) for the project status and [Api.md](Api.md) for the HTTP API.
