# GymAssistant TODO

The core authentication flow is implemented: login, persisted refresh tokens, refresh-token rotation, centralized frontend `401 -> refresh -> retry once`, startup session recovery and server-side logout/revocation.

Configuration cleanup for the current auth/frontend flow is also complete: the frontend API base URL is centralized through `VITE_API_URL`, refresh-token lifetime is configured and validated through `RefreshTokenOptions`, and the credential-enabled CORS origin is read from backend configuration.

System/private exercise ownership is now enforced: normal users create and edit only their own private exercises, `IsSystem` is not accepted from create/edit requests, and SQL Server enforces valid `IsSystem` / `OwnerUserId` combinations through a check constraint. The exercise picker already marks system exercises with a `System` badge.

## Remaining work

- [ ] Add a `System` badge to system exercises on the main exercise list.
- [ ] Define the production-safe seed/admin strategy for creating and maintaining the system exercise catalog.
- [ ] Add authentication and two-user data-isolation integration tests.
- [ ] Clean up temporary authentication/debug behavior.
- [ ] Decide the production access-token storage model instead of treating `localStorage` as final by default.
- [ ] Optionally harden explicit logout when the backend is unreachable.
- [ ] Reset development/demo data before the production-ready release.

See [Authentication.md](Authentication.md) for the current authentication and authorization flow, [README.md](README.md) for the project status and [Api.md](Api.md) for the HTTP API.
