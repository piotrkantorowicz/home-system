# Frontend — ESLint, Prettier, Husky

Source of truth is the config in `src/ui/`. This page explains the intent; do not copy the
snippets back into the repo — edit the real files.

## ESLint (`src/ui/eslint.config.ts`, flat config, ESLint 9)

```
js.configs.recommended
typescript-eslint  strictTypeChecked + stylisticTypeChecked   (projectService: true)
react-hooks v7     recommended  + exhaustive-deps: error      (includes React Compiler rules)
react-refresh      only-export-components: warn (allowConstantExport)
jsx-a11y           recommended
import             no-cycle: error, no-duplicates: error, order: warn (alphabetised, groups, newlines)
project rules      no-explicit-any, consistent-type-imports, no-non-null-assertion,
                   prefer-nullish-coalescing, prefer-optional-chain, no-floating-promises,
                   await-thenable, no-unused-vars (^_ ignored), no-console (warn/error allowed),
                   prefer-const, no-var, eqeqeq
e2e override       relaxes unsafe-* / floating-promises for Playwright specs
eslint-config-prettier last — formatting is Prettier's job
```

Rules that bite most often and how to satisfy them:

| Rule | Do |
|---|---|
| `consistent-type-imports` (+ `verbatimModuleSyntax`) | `import type { X }` for types; `import { type X, y }` when mixed |
| `no-floating-promises` | `await`, `void promise`, or return it |
| `no-non-null-assertion` | narrow with a guard; never `!` |
| `no-explicit-any` | `unknown` + guard; if truly unavoidable, `// REASON: …` on the same line |
| `import/order` | external → `@shared` → `@modules` → parent → sibling → `import type` (auto-fixable) |
| `react-hooks/exhaustive-deps` | fix the deps; move the callback inside the effect; never disable |
| `react-refresh/only-export-components` | keep non-component exports (schemas, constants) in separate files |

`eslint-disable` is allowed only with `// REASON:` on the same or previous line. Existing
disables without a reason are cleaned up when a file is touched.

## Prettier (`src/ui/.prettierrc`)

```json
{ "semi": true, "singleQuote": true, "trailingComma": "all", "printWidth": 100,
  "tabWidth": 2, "arrowParens": "always", "endOfLine": "lf",
  "plugins": ["prettier-plugin-tailwindcss"] }
```

Single quotes. Tailwind classes are sorted by the plugin — never hand-order them. The
`format-on-edit.sh` hook runs Prettier on every file an agent writes under `src/ui` and `e2e`.

## TypeScript

`tsconfig.app.json` is strict (see `frontend-react-typescript.md`). `npm run type-check` is
`tsc -b` over the project references (`tsconfig.app.json` + `tsconfig.node.json`, both `noEmit`); `npm run build` is `tsc -b && vite build`, so a type error fails both.

## Scripts (`src/ui/package.json`)

| Script | Purpose |
|---|---|
| `dev` | Vite dev server on `:5173`, proxies `/authentik` to `:9000` |
| `build` | `tsc -b && vite build` |
| `lint` / `lint:fix` | ESLint |
| `format` / `format:check` | Prettier over `src/**/*.{ts,tsx,css,json}` |
| `type-check` | `tsc -b` — the root `tsconfig.json` is references-only, so `tsc --noEmit` on it checks nothing |
| `test` / `test:ui` / `test:coverage` | Vitest watch / UI / single run + v8 coverage |
| `check` | type-check + lint + test:coverage — what CI runs |
| `generate:api:<module>` | `openapi-typescript` from the running backend → `modules/<module>/api/generated/schema.ts` |
| `prepare` | points `core.hooksPath` at `.husky` |

## Git hooks (`.husky/`, repo root — POSIX `sh`, not bash)

| Hook | What it does |
|---|---|
| `pre-commit` | `lint-staged` in `src/ui` (ESLint `--fix` + Prettier on staged `ts/tsx`, Prettier on `json/css/md`), then `scripts/verify.sh --staged` — path-aware backend/frontend/e2e checks |
| `commit-msg` | `commitlint` (`@commitlint/config-conventional`, lower-case subject, header ≤ 72) |
| `pre-push` | branch name must match `<type>/<issue>-<kebab-slug>` |

Hooks need `src/ui/node_modules` — a fresh clone or a new git worktree must run
`cd src/ui && npm ci` before the first commit.

## CI (`.github/workflows/`)

- `frontend-ci.yml` — type-check, lint, format:check, test:coverage, build (on `src/ui/**` changes)
- `backend-ci.yml` — restore, `dotnet format --verify-no-changes`, build, `dotnet test` on the whole solution
- `pr-hygiene.yml` — every PR: commitlint on commits + PR title, branch name, linked issue, `CLAUDE.md` ↔ `AGENTS.md` sync
- `e2e-nightly.yml` — nightly (03:00 UTC) + `workflow_dispatch` + PRs targeting `main`: the Playwright suite against the real stack (Docker infra, backend, Vite); the PR run is report-only, not yet a required check — see `docs/e2e/README.md` § Nightly CI

Anything green locally via `scripts/verify.sh --branch` is green in CI; the two run the same commands.
