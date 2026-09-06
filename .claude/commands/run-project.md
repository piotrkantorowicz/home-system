---
description: Bring the full stack up locally — Docker infra, backend API, frontend SPA
---

Bring the entire HomeSystem stack up for local development: Docker infrastructure
(Authentik + per-module Postgres), the .NET host API, and the React SPA.

## Usage

```
/run-project [--infra-only | --no-frontend | --stop]
```

- no args: start everything (infra → backend → frontend)
- `--infra-only`: only `docker compose` services
- `--no-frontend`: infra + backend, skip the Vite dev server
- `--stop`: tear the whole stack down (or use `/stop-project` directly)

## Ports

| Service | URL |
|---|---|
| Frontend (Vite) | http://localhost:5173 |
| Backend API | http://localhost:5050 (`https://localhost:7050` via the HTTPS profile) |
| OpenAPI | http://localhost:5050/openapi/v1.json |
| Authentik | http://localhost:9000 |
| DietPlanner Postgres | localhost:5432 |
| Notifications Postgres | localhost:5433 |

## Instructions

### 1. Preflight

- Confirm `infrastructure/.env` exists. If not, copy `infrastructure/.env.example` → `infrastructure/.env` and tell the user to fill in real passwords (Authentik will not boot without them). Do not invent secret values.
- Confirm Docker is running (`docker info`).
- If `--stop` was passed: run `cd infrastructure && docker compose --profile diet-planner --profile notifications down`, then kill any `dotnet run` / `vite` processes started for this stack, and stop here.

### 2. Infrastructure (Docker)

```bash
cd infrastructure && docker compose --profile diet-planner --profile notifications up -d
```

- This starts the always-on services (Authentik server + worker, its Postgres + Redis) plus both module databases.
- Wait for health: poll `docker compose ps` until `authentik-db`, `authentik-redis`, `dietplanner-db`, and `notifications-db` report `healthy`. Authentik first-run applies blueprints from `infrastructure/authentik/blueprints/` and can take 30–60s.
- If only the diet-planner module is needed, drop `--profile notifications` (and vice-versa). The backend host wires both modules, so a missing DB for a wired module fails startup — keep both unless the user asks otherwise.
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
dotnet run --project src/Apis/HomeSystem.REST
```

Run this in the background (it is long-lived). Notes:

- Development auto-applies EF Core migrations (DietPlanner) and DbUp scripts (Notifications) on startup.
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

- E2E tests: `/run-e2e`
- Regenerate API types: `cd src/ui && npm run generate:api:diet-planner`
