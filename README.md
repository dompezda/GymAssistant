# GymAssistant

**A full-stack workout tracking application built with .NET 10 and React.**

GymAssistant helps users organize exercises, prepare workout templates, track sets during a training session, and review completed workouts. It is a personal portfolio project and is actively being developed.

> **Project status (24 November 2026):** v1.X release preparation. The [live application](https://www.gymassistant.pl/) and [API](https://api.gymassistant.pl/) are deployed via GitHub Actions. The backend runs on Azure App Service Linux B1 with Always On; Azure SQL contains 100 seeded system exercises. Production session recovery and logout/reload were manually verified after fixing the cross-site cookie issue. Production SQL connection timeouts after idle periods are a release blocker; serverless auto-resume is suspected but not confirmed. Natural JWT expiry, explicit refresh rotation verification, production SMTP, full smoke tests and security hardening remain open.

## Screenshots

The screenshots show the current mobile layout (UI labels are in Polish).

| Main menu | Exercise library | Workout template |
| :---: | :---: | :---: |
| <img src="docs/screenshots/main-menu.webp" alt="Main menu with shortcuts to exercises, templates, sessions and history" width="260"> | <img src="docs/screenshots/exercise-library.webp" alt="Exercise catalog with search and system exercise badges" width="260"> | <img src="docs/screenshots/workout-template.webp" alt="Workout template showing exercises and planned set counts" width="260"> |

| Active workout | Edit exercise |
| :---: | :---: |
| <img src="docs/screenshots/active-workout.webp" alt="Recording weights and repetitions during a workout" width="300"> | <img src="docs/screenshots/edit-exercise.webp" alt="Editing a private exercise and its muscle groups" width="300"> |

## Features

- **Exercise library:** browse system exercises, search exercises in the catalog and picker, and create or edit personal exercises with muscle-group assignments.
- **Workout templates:** create reusable training plans and adjust the included exercises and planned sets.
- **Workout sessions:** start from a template or build a session from scratch; record sets, weights and repetitions; resume an unfinished workout or discard it before starting another; add or remove exercises while the session is active.
- **Training history:** review completed workouts and exercise history (dates, sets, repetitions and weights); expand a workout from an exercise's history to see its exercise list and set counts. Details are fetched on demand and cached for the current page visit. Active and discarded sessions are excluded.
- **User accounts:** register, log in, reset a forgotten password using an email link (currently tested via local Mailpit), and access only your own templates, sessions, and private exercises. System exercises are shared and read-only.

## Tech stack

| Layer | Technologies |
| --- | --- |
| Backend | C#, .NET 10, ASP.NET Core Minimal APIs, Entity Framework Core 10 |
| Database | SQL Server (local), Azure SQL Database (deployment) |
| Frontend | React, TypeScript, Vite |
| Authentication | ASP.NET Core Identity, JWT access tokens, refresh-token rotation, reset tokens |
| Development email | MailKit SMTP + Mailpit (local testing only) |
| Testing | xUnit, `WebApplicationFactory`, SQLite in-memory integration tests |
| API reference | OpenAPI and Scalar in Development |
| Hosting | Azure App Service Linux B1, Azure Static Web Apps, Azure SQL Database Serverless Free Offer |
| CI/CD | GitHub Actions: backend build/tests, frontend build and automated deployments |

## Technical highlights

- **Authentication and session recovery:** short-lived JWT access tokens, refresh tokens stored as hashes in the database, `HttpOnly` refresh cookies, token rotation/revocation, and a centralized `401 → refresh → retry` flow.
- **Authorization:** user ownership enforced server-side for workout templates, sessions, and private exercises; system exercises remain accessible to all authenticated users.
- **Session lifecycle:** `FinishedAt` and `DiscardedAt` distinguish completed, abandoned and active workouts. The API exposes active-session lookup and idempotent discard, prevents a second active session, and blocks modifications to completed or discarded workouts.
- **Active workout save coordination and recovery:** a parent-owned in-flight guard prevents overlapping writes and temporarily disables controls. The current v1.1-in-progress interface separates the exercise list from set-entry details; locally edited sets are PATCHed on return to the list, and failed saves keep the details view open for retry. Finishing retains failed exercise writes for retry. Manual refresh reloads server data with confirmation before discarding unsaved local edits.
- **Integration testing:** real authenticated HTTP requests against a test host built with `WebApplicationFactory` and SQLite in-memory, including ownership and validation scenarios.

For implementation details, see [Authentication.md](Authentication.md) and [Api.md](Api.md).

## Getting started (local development)

**Prerequisites:** .NET 10 SDK, SQL Server, Node.js/npm, and the `dotnet-ef` tool (EF Core 10 compatible).

1. Clone this repository and configure a local SQL Server instance. Adjust `ConnectionStrings:DefaultConnection` as needed using local configuration or user secrets.
2. Configure a strong JWT signing key and a development-user password in ASP.NET Core user secrets (do not commit actual credentials):

   ```powershell
   dotnet user-secrets set "Jwt:SigningKey" "<a-random-secret-of-at-least-32-bytes>" --project backend/GymAssistant.Api/GymAssistant.Api.csproj
   dotnet user-secrets set "DevelopmentUser:Password" "<strong-local-password>" --project backend/GymAssistant.Api/GymAssistant.Api.csproj
   ```

3. Apply database migrations and start the API:

   ```powershell
   dotnet ef database update --project backend/GymAssistant.Api/GymAssistant.Api.csproj --startup-project backend/GymAssistant.Api/GymAssistant.Api.csproj
   dotnet run --project backend/GymAssistant.Api/GymAssistant.Api.csproj --launch-profile http
   ```

4. Create `frontend/.env.local` with the backend URL, then start the frontend:

   ```dotenv
   VITE_API_URL=http://localhost:5235
   ```

   ```powershell
   cd frontend
   npm ci
   npm run dev
   ```

The development frontend is configured for `http://localhost:5173`. The API's HTTP port is defined by its launch profile; update `VITE_API_URL` if yours differs. Local HTTPS/cookie behavior may require using the HTTPS launch profile and matching origins.

### Local password-reset email

Password reset uses MailKit SMTP, with Mailpit as the local test inbox. With Docker Desktop running, start Mailpit:

```powershell
docker run -d --name mailpit -p 127.0.0.1:8025:8025 -p 127.0.0.1:1025:1025 axllent/mailpit
```

The API's `appsettings.Development.json` configures `Smtp:Host=localhost`, `Smtp:Port=1025` and a local-only sender address. Browse `http://localhost:8025` to see reset messages. Use an email that belongs to an Identity user in the local database; Mailpit captures that message even if no real mailbox exists.

The reset URL points to the frontend origin configured by `Frontend:Origin`. Production delivery is **not yet configured**: a real SMTP provider, secure transport, credentials via secrets/environment and abuse protection are required before public release. Do not commit real SMTP passwords or tokens.

### Integration tests

```powershell
dotnet test backend/GymAssistant.sln
```

## Azure deployment status (24 November 2026)

- **Frontend:** [www.gymassistant.pl](https://www.gymassistant.pl/), Azure Static Web Apps.
- **Backend:** `gymassistant-api`, Linux / .NET 10, Basic B1, one instance, Always On, Poland Central; [api.gymassistant.pl](https://api.gymassistant.pl/) with Azure-managed TLS (SNI).
- **Database:** Azure SQL Serverless Free Offer, Managed Identity, 100 seeded system exercises, paid overage disabled.
- **CI/CD:** automated frontend and backend deployments via GitHub Actions. Production frontend build uses `VITE_API_URL=https://api.gymassistant.pl`.
- **Cost control:** monthly EUR 20 subscription budget with actual and forecast email alerts; not a hard spending cap.
- **Verification:** unauthenticated exercise GET on custom API domain returned 401; repeated session recovery and logout/reload succeeded. Natural JWT expiry and explicit refresh-cookie rotation checks remain pending.
- **Known production issue:** intermittent login HTTP 500 caused by SQL connection timeout during post-login; diagnosis and mitigation tracked as P1-12, promoted to P0. See [DEPLOYMENT.md](DEPLOYMENT.md).
- **Next steps:** see [todo.md](todo.md) for open P0–P3 tasks, including production SMTP, Data Protection, firewall and full smoke tests.

See [DEPLOYMENT.md](DEPLOYMENT.md) for infrastructure, DNS, deployment and cost details.

## Documentation and roadmap

- [API reference](Api.md) — endpoints, request/response examples, and authorization rules.
- [Authentication design](Authentication.md) — login, refresh, logout, token storage, and authorization flow.
- [TODO](todo.md) — current 1.X backlog and priorities.
- [Deployment](DEPLOYMENT.md) — production infrastructure and cost controls.
- [Changelog](CHANGELOG.md) — development history.

**Known work in progress:** the active-workout list/details UI refactor requires regression verification and restoration or replacement of the previous horizontal swipe gesture; consistent API error presentation across screens and a backend error contract, guarding against unsaved changes when leaving the workout screen, mobile gesture edge cases, thorough failure-path testing and additional integration tests, and final review of production authentication configuration. This repository demonstrates the current implementation rather than a production-ready service.
