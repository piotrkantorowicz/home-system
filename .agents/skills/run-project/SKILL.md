---
name: run-project
description: "Bring the full stack up locally — Docker infra, observability (Aspire dashboard), backend API, frontend SPA"
---

# run-project

Bring the entire HomeSystem stack up for local development: Docker infrastructure
(Authentik + per-module Postgres + the Aspire dashboard), the .NET host API exporting
OTLP, and the React SPA with browser tracing on.

## Usage

```
$run-project [--infra-only | --no-frontend | --no-observability | --stop]
```

- no args: start everything (infra + dashboard → backend → frontend), telemetry on
- `--infra-only`: only `docker compose` services
- `--no-frontend`: infra + backend, skip the Vite dev server
- `--no-observability`: skip the `observability` profile, `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTLP_PROXY_TARGET` and `VITE_TRACING_ENABLED`
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
| Budget Postgres | localhost:5435 |
| Investments Postgres | localhost:5436 |
| Aspire dashboard (traces, metrics, logs) | http://localhost:18888 |
| OTLP gRPC (API export) | localhost:4317 |
| OTLP HTTP (browser, via Vite `/otlp`) | localhost:4318 |

## Instructions

### 1. Preflight

- Confirm `infrastructure/.env` exists. If not, copy `infrastructure/.env.example` → `infrastructure/.env` and tell the user to fill in real passwords (Authentik will not boot without them). Do not invent secret values.
- Confirm Docker is running (`docker info`).
- If `--stop` was passed: run `$stop-project` (it downs every profile, `observability` included), then kill any `dotnet run` / `vite` processes started for this stack, and stop here.

### 2. Infrastructure (Docker)

```bash
cd infrastructure && docker compose --profile diet-planner --profile notifications --profile household --profile budget --profile investments --profile observability up -d
```

- This starts the always-on services, all five module databases and the Aspire dashboard. Drop `--profile observability` for `--no-observability`.
- The dashboard keeps telemetry in memory only and has auth off; its ports bind to 127.0.0.1. It may already be running from an earlier session (`docker ps --filter name=aspire-dashboard`) — `up -d` leaves it as is.
- Check `DIETPLANNER_DB_PASSWORD`, `NOTIFICATIONS_DB_PASSWORD`, `HOUSEHOLD_DB_PASSWORD`, `BUDGET_DB_PASSWORD`, and `INVESTMENTS_DB_PASSWORD` are set in `infrastructure/.env`; older local files may lack Household, Budget or Investments configuration.
- Wait for `authentik-db`, `authentik-redis`, `dietplanner-db`, `notifications-db`, `household-db`, `budget-db`, and `investments-db` to report `healthy` in `docker compose ps`.
- The host wires all five modules. Keep all five databases running; HTTP health alone does not prove database readiness.
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
ConnectionStrings__Budget="Host=localhost;Port=5435;Database=budget;Username=budget;Password=${BUDGET_DB_PASSWORD}" \
ConnectionStrings__Investments="Host=localhost;Port=5436;Database=investments;Username=investments;Password=${INVESTMENTS_DB_PASSWORD}" \
Mcp__Enabled=true Mcp__PublicBaseUrl=http://localhost:5050 Mcp__ImportsEnabled=true \
Authentication__Mcp__Authority=http://localhost:9000/application/o/home-system-mcp/ \
Authentication__Mcp__Audience=home-system-mcp \
Assistant__Enabled=true Assistant__Provider=Fake \
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317 \
dotnet run --project src/Apis/HomeSystem.REST
```

`Assistant__*` turns on the in-app assistant with the scripted `Fake` model the E2E
`assistant.spec.ts` needs (no API key, refused in Production). For a real model set
`Assistant__Provider` / `Assistant__ApiKey` instead (`docs/design/assistant/README.md` §7).

The `Mcp__*` / `Authentication__Mcp__*` lines turn on the MCP endpoint at
`http://localhost:5050/mcp` for Claude Code (setup: `docs/design/mcp/CLIENT-SETUP.md`).

`OTEL_EXPORTER_OTLP_ENDPOINT` turns on OTLP export of traces, metrics and logs to the
dashboard; nothing is exported without it. Omit it for `--no-observability`.

Run this in the background (it is long-lived). Notes:

- Development auto-applies EF Core migrations (DietPlanner, Household, Budget and Investments) and DbUp scripts (Notifications). Confirm successful migration messages: the host may keep serving HTTP after migration failure.
- If the user has set user-secrets or their own `ConnectionStrings__*` env, the plain `dotnet run --project src/Apis/HomeSystem.REST` is enough — try it first, fall back to the block above on `Npgsql ... No password has been provided`.
- Readiness check: `curl -sf http://localhost:5050/health/ready > /dev/null`.
- Stop here if `--no-frontend`.

### 4. Frontend SPA

```bash
cd src/ui && npm install && OTLP_PROXY_TARGET=http://localhost:4318 VITE_TRACING_ENABLED=true npm run dev
```

Run in the background. Vite serves on http://localhost:5173 and proxies `/authentik` → `http://localhost:9000` and, when `OTLP_PROXY_TARGET` is set, `/otlp` → the dashboard's OTLP HTTP port (prefix stripped). `VITE_TRACING_ENABLED=true` lazy-loads the browser OTel SDK; for `--no-observability` drop both variables. `npm install` can be skipped if `node_modules` is present and `package-lock.json` is unchanged.

### 5. Check telemetry (skip with `--no-observability`)

Hit any API route, then
`curl -s http://localhost:18888/api/telemetry/resources` lists `home-system-api`, and
`curl -s -o /dev/null -w '%{http_code}' -X POST -H 'Content-Type: application/json' --data '{"resourceSpans":[]}' http://localhost:5173/otlp/v1/traces`
returns 200 (a 404 means the `/otlp` proxy is missing or not stripping the prefix). Browser
spans (`home-system-ui`) appear after the first page load in a signed-in session. If Bash
blocks `curl`, run the same through `ctx_execute` with `language: "shell"`.

### 6. Report

Print a table of what is up, each URL, and how to tail logs / stop each piece. If the frontend API types look stale after a backend contract change, mention `npm run generate:api:diet-planner` (backend must be running).

## Related

- E2E tests: `$run-e2e`
- Regenerate API types: `cd src/ui && npm run generate:api:diet-planner`
