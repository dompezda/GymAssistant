# GymAssistant TODO

The core authentication flow is implemented: login, persisted refresh tokens, refresh-token rotation, centralized frontend `401 -> refresh -> retry once`, startup session recovery and server-side logout/revocation.

Configuration cleanup for the current auth/frontend flow is also complete: the frontend API base URL is centralized through `VITE_API_URL`, refresh-token lifetime is configured and validated through `RefreshTokenOptions`, and the credential-enabled CORS origin is read from backend configuration.

The remaining authentication and ownership hardening roadmap is maintained in [Authentication.md](Authentication.md). Current priorities include production-safe system/private exercise ownership, integration tests, cleanup of temporary development/debug behavior and the final production decision for access-token storage.

See [README.md](README.md) for the current project status and [Api.md](Api.md) for the HTTP API.
