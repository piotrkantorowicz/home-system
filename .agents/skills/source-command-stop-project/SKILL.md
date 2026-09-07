---
name: "source-command-stop-project"
description: "Stop the local stack — frontend, backend, Docker infra, and free the ports"
---

# source-command-stop-project

Use this skill when the user asks to run the migrated source command `stop-project`.

## Command Template

Tear down whatever `/run-project` (or `/run-e2e`) started: the Vite dev server, the
.NET host, the Playwright web server, and the Docker infrastructure. Leaves data
volumes intact.

## Usage

```
/stop-project [--app-only | --infra-only | --volumes]
```

- no args: stop app processes **and** Docker infra
- `--app-only`: only the local processes (backend, frontend, Playwright) — leave Docker up
- `--infra-only`: only `docker compose down` — leave local processes running
- `--volumes`: also `docker compose down -v` (**destroys** all Postgres data — Authentik config, diet-planner + notifications DBs). Confirm with the user before running this.

## Ports in play

| Port | Process |
|---|---|
| 5173 | Vite dev server |
| 5050 / 7050 | .NET host (`HomeSystem.REST`) |
| 9000 / 9443 | Authentik (Docker) |
| 5432 / 5433 | Postgres (Docker) |

## Instructions

### 1. App processes (skip if `--infra-only`)

- If this session started them as background tasks, prefer stopping those tasks by id
  (KillShell) — cleaner than pattern-killing.
- Otherwise kill by pattern / port:
  ```bash
  pkill -f "HomeSystem.REST"        # .NET host
  pkill -f "dotnet run --project src/Apis/HomeSystem.REST"
  pkill -f "vite"                    # Vite dev server (also covers Playwright's webServer)
  pkill -f "playwright test"         # any running suite
  ```
- Verify the ports are free:
  ```bash
  for p in 5173 5050 7050; do lsof -ti :$p | xargs -r kill; done
  ```
- Report which processes were actually killed vs. already stopped. Do **not** kill an
  unrelated `vite` / `dotnet` from another project — check `lsof -i :5173` / `:5050`
  output first and only kill PIDs whose cwd is this repo.

### 2. Docker infra (skip if `--app-only`)

```bash
cd infrastructure && docker compose --profile diet-planner --profile notifications down
```

- Add `-v` only for `--volumes` (after explicit user confirmation) — this wipes every
  named volume: `authentik_db_data`, `authentik_redis_data`, `authentik_media`,
  `authentik_templates`, `dietplanner_db_data`, `notifications_db_data`. Authentik
  re-bootstraps from blueprints on next `up`; module DBs re-migrate.

### 3. Report

Confirm: app ports free, `docker compose ps` shows nothing (or only what `--app-only`
intentionally left), volumes preserved unless `--volumes` was used.

## Related

- Start it back up: `/run-project`
