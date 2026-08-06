# GymAssistant

GymAssistant is a portfolio project for planning workouts, running workout sessions and reviewing training history.

## Tech stack

- Backend: .NET 9, Minimal API, Entity Framework Core, SQL Server
- Frontend: React, TypeScript, Vite
- Authentication: ASP.NET Core Identity and JWT bearer tokens

## Current authentication status

Implemented:

- development user stored through ASP.NET Core Identity,
- JWT access-token generation and validation,
- `POST /api/auth/login`,
- protected `GET /api/auth/me`,
- frontend login page,
- access-token persistence in `localStorage`,
- `AuthProvider` session validation on application startup.

Still in progress:

- `useAuth()` hook,
- protected frontend routes,
- logout flow,
- shared bearer-token HTTP client,
- authorization and user-data isolation across domain endpoints,
- authentication integration tests.

See [Authentication.md](Authentication.md) for the current authentication flow and [todo.md](todo.md) for the implementation checklist.
