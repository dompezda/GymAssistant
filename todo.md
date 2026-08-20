# GymAssistant TODO

The core authentication flow is now implemented: login, persisted refresh tokens, refresh-token rotation, centralized frontend `401 -> refresh -> retry once`, startup session recovery and server-side logout/revocation.

The remaining authentication and ownership hardening roadmap is maintained in [Authentication.md](Authentication.md). Current priorities include configuration cleanup, production-safe system/private exercise ownership, integration tests and removal of temporary development/debug behavior.

See [README.md](README.md) for the current project status and [Api.md](Api.md) for the HTTP API.
