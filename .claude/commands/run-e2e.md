---
description: Run the Playwright E2E suite against a real backend, frontend, and Authentik
---

Run the end-to-end Playwright suite in `e2e/`. It runs against a **real** stack (no
mocks) — real Authentik, real backend, real Vite frontend. Playwright auto-starts the
frontend; infra + backend must be up first.

## Usage

```
/run-e2e [spec] [--ui | --debug | --headed] [--setup]
```

- no args: full suite (`npm test` in `e2e/`)
- `spec`: pass through to Playwright, e.g. `/run-e2e diet-planner/profile`
- `--ui` / `--debug`: interactive modes (`test:ui` / `test:debug`)
- `--setup`: also run the one-time `npx playwright install --with-deps chromium`

## Prerequisites (must be up before tests)

1. **Docker infra, diet-planner profile** — Authentik + diet-planner Postgres:
   ```bash
   cd infrastructure && docker compose --profile diet-planner up -d
   ```
   `infrastructure/.env` must define `E2E_USER_PASSWORD` — the Authentik blueprint
   provisions `E2eWorker0..E2eWorker3` with it.
2. **Backend API** in Development (registers the `DELETE /api/v1/test-support/purge-my-data`
   cleanup route used by global teardown). Use the password-injecting command from
   `/run-project` step 3. In Staging/Production this route 404s unless
   `E2ETestSupport__Enabled=true`.
3. **Frontend** — leave it to Playwright: `playwright.config.ts` runs
   `npm --prefix ../src/ui run dev` and reuses an existing server on `:5173`.

`/run-project --no-frontend` gets 1–2 up in one shot.

## Instructions

### 1. Preflight

- `e2e/.env` must exist. If not, copy `e2e/.env.example` → `e2e/.env` and tell the user
  to set `TEST_USER_PASSWORD` to the **same value** as `E2E_USER_PASSWORD` in
  `infrastructure/.env`. The auth helpers throw (no committed default) if it is missing.
- Infra healthy: `docker compose ps` in `infrastructure/` — `authentik-server` up,
  `dietplanner-db` healthy. `curl -sf http://localhost:9000/-/health/ready/`.
- Backend up: `curl -sf http://localhost:5050/openapi/v1.json > /dev/null`.
- If a prerequisite is missing, stop and say exactly which — do not run the suite
  against a partial stack.

### 2. Install

```bash
cd e2e && npm install
```

Plus, on `--setup` or first ever run: `npx playwright install --with-deps chromium`.

### 3. Run

| Mode | Command (from `e2e/`) |
|---|---|
| full suite | `npm test` |
| single spec | `npx playwright test <spec>` |
| `--ui` | `npm run test:ui` |
| `--debug` | `npm run test:debug` |
| `--headed` | `npx playwright test <spec> --headed` |

4-worker parallel — each worker logs in as its own `E2eWorker<n>`, writes
`playwright/.auth/user-<n>.json`; the `setup` project logs all workers in first;
`shared/global-teardown.ts` purges each worker's data at the end.

### 4. Report

- Pass/fail counts. On failure: `npx playwright show-report` (`e2e/playwright-report/`);
  traces/screenshots/video are retained only on failure / first retry.
- A teardown **404 warning** in the output = backend was not in Development / the
  test-support flag was off. Call it out.

## Related

- Bring the stack up: `/run-project`  ·  tear it down: `/stop-project`
- Spec reference docs: `docs/e2e/README.md`
