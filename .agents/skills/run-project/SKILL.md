---
name: run-project
description: "Bring the full stack up locally — Docker infra, backend API, frontend SPA"
---

# run-project

Bring the entire HomeSystem stack up for local development: Docker infrastructure
(Authentik + per-module Postgres), the .NET host API, and the React SPA.

## Usage

```
$run-project [--infra-only | --no-frontend | --stop]
```

- no args: start everything (infra → backend → frontend)
- `--infra-only`: only `docker compose` services
- `--no-frontend`: infra + backend, skip the Vite dev server
- `--stop`: tear the whole stack down (or use `$stop-project` directly)

## Ports

| Service | URL |
|---|---|
| Frontend (Vite) | http://localhost:5173 |
| Backend API | http://localhost:5050 (`https://localhost:7050` via the HTTPS profile) |
| OpenAPI | http://localhost:5050/openapi/v1.json |
| Authentik | http://localhost:9000 |
| DietPlanner Postgres | localhost:5432 |
| Notifications Postgres | localhost:5433 |
| Household Postgres | localhost:5434 |

## Instructions

### 1. Preflight

- Confirm `infrastructure/.env` exists. If not, copy `infrastructure/.env.example` → `infrastructure/.env` and tell the user to fill in real passwords (Authentik will not boot without them). Do not invent secret values.
- Confirm Docker is running (`docker info`).
- If `--stop` was passed: run `cd infrastructure && docker compose --profile diet-planner --profile notifications down`, then kill any `dotnet run` / `vite` processes started for this stack, and stop here.

### 2. Infrastructure (Docker)

```bash
cd infrastructure && docker compose --profile diet-planner --profile notifications --profile household up -d
```

- This starts the always-on services plus all three module databases.
- Check `DIETPLANNER_DB_PASSWORD`, `NOTIFICATIONS_DB_PASSWORD`, and `HOUSEHOLD_DB_PASSWORD` are set in `infrastructure/.env`; older local files may lack Household configuration.
- Wait for `authentik-db`, `authentik-redis`, `dietplanner-db`, `notifications-db`, and `household-db` to report `healthy` in `docker compose ps`.
- The host wires all three modules. Keep all three databases running; HTTP health alone does not prove database readiness.
- Stop here if `--infra-only`.

### 3. Backend API

`appsettings.json` ships the DB connection strings with **empty passwords** and there
are no user-secrets — the host **will not boot** without the passwords injected. Source
them from `infrastructure/.env` and pass as `ConnectionStrings__*` env vars:

```bash
set -a && . infrastructure/.env && set +a && \
ASPNETCORE_ENVIRONMENT=Development \
ConnectionStrings__DietPlanner="Host=localhost;Port=5432;Database=dietplanner;Username=dietplanner;Password=${DIETPLANNER_DB_PASSWORD}" \
ConnectionStrings__Notifications="Host=localhost;Port=5433;Database=notifications;Username=notifications;Password=${NOTIFICATIONS_DB_PASSWORD}" \
ConnectionStrings__Household="Host=localhost;Port=5434;Database=household;Username=household;Password=${HOUSEHOLD_DB_PASSWORD}" \
dotnet run --project src/Apis/HomeSystem.REST
```

Run this in the background (it is long-lived). Notes:

- Development auto-applies EF Core migrations (DietPlanner and Household) and DbUp scripts (Notifications). Confirm successful migration messages: the host may keep serving HTTP after migration failure.
- If the user has set user-secrets or their own `ConnectionStrings__*` env, the plain `dotnet run --project src/Apis/HomeSystem.REST` is enough — try it first, fall back to the block above on `Npgsql ... No password has been provided`.
- Readiness check: `curl -sf http://localhost:5050/openapi/v1.json > /dev/null`.
- Stop here if `--no-frontend`.

### 4. Frontend SPA

```bash
cd src/ui && npm install && npm run dev
```

Run in the background. Vite serves on http://localhost:5173 and proxies `/authentik` → `http://localhost:9000`. `npm install` can be skipped if `node_modules` is present and `package-lock.json` is unchanged.

### 5. Report

Print a table of what is up, each URL, and how to tail logs / stop each piece. If the frontend API types look stale after a backend contract change, mention `npm run generate:api:diet-planner` (backend must be running).

## Related

- E2E tests: `$run-e2e`
- Regenerate API types: `cd src/ui && npm run generate:api:diet-planner`
