# Home System UI

React 19 + Vite + TypeScript SPA for the home-system platform.

## Requirements

- Node.js 22+
- Diet Planner API running on http://localhost:5000 (see `src/apis/diet-planner/README.md`)
- Authentik running on http://localhost:9000 (see `infrastructure/README.md`)

## Setup

```bash
cp .env.example .env
# Fill in VITE_OIDC_CLIENT_ID with the client ID from your Authentik application
```

| Variable | Description |
|---|---|
| `VITE_API_BASE_URL` | Diet Planner API base URL (default: `http://localhost:5000`) |
| `VITE_AUTHENTIK_DOMAIN` | Authentik domain (default: `http://localhost:9000`) |
| `VITE_OIDC_CLIENT_ID` | OIDC client ID from Authentik |
| `VITE_REDIRECT_URI` | Auth redirect URI (default: `http://localhost:5173`) |

## Running

```bash
npm install
npm run dev       # Vite dev server on http://localhost:5173
```

## Commands

```bash
npm run build          # Production build (tsc + vite)
npm run type-check     # Type check without emitting
npm run lint           # ESLint
npm run lint:fix       # ESLint with auto-fix
npm run format         # Prettier
npm run format:check   # Check formatting

# Regenerate OpenAPI client (API must be running)
npm run generate:api:diet-planner
```

## E2E Tests

```bash
npm run test:e2e          # Run all Playwright tests
npm run test:e2e:ui       # UI mode
npm run test:e2e:debug    # Debug mode

# Single file or test
npx playwright test e2e/diet-planner/products.spec.ts
npx playwright test -g "test name"
```

Tests run serially (workers: 1) since they share backend state. Auth is set up once in `e2e/shared/auth.setup.ts` and reused across all tests. Global teardown cleans up test data after the run.

## Architecture

### Module System

The app is module-based. Each module is registered in `src/app/main.tsx` via `registerModule()` and exports an `AppModule` from `src/modules/<module>/index.ts`:

```typescript
export const myModule: AppModule = {
  name: 'my-module',
  basePath: '/my-module',
  icon: SomeIcon,
  localeNamespaces: ['my-module'],
  i18nResources: { en: { 'my-module': en }, pl: { 'my-module': pl } },
  navItems: [...],
  routes: [
    { index: true, Component: Dashboard },
    { path: 'items', Component: ItemList },
  ],
};
```

Routes are lazy-loaded automatically. The sidebar and router are module-aware and built dynamically from the registry.

### API Client

Each module has a typed API client in `api/client.ts` built on `openapi-fetch` with the schema generated from `api/generated/schema.ts`. The shared interceptor in `src/shared/api/tokenInterceptor.ts` handles JWT injection and auto-renewal with a silent retry on 401.

Regenerate the schema after backend changes: `npm run generate:api:diet-planner`.

### Data Fetching

TanStack Query v5 with `openapi-fetch`. Query keys are arrays namespaced by resource. Mutations invalidate relevant queries on success.

### Path Aliases

| Alias | Resolves to |
|---|---|
| `@/*` | `src/*` |
| `@shared/*` | `src/shared/*` |
| `@modules/*` | `src/modules/*` |

### Shared Components

`src/shared/components/ui/` — Button, Card, Dialog, Input, Table, Badge, Label, Textarea. Module-specific components go in `src/modules/<module>/components/`.

### i18n

Shared keys in `src/shared/locales/{en,pl}.json`. Module keys in `src/modules/<module>/locales/`. Access with `t('common.key')` or `t('diet-planner:key')`.

## Tech Stack

| | |
|---|---|
| Framework | React 19 |
| Build | Vite 7 |
| Language | TypeScript 5.9 |
| Routing | React Router v7 |
| Data fetching | TanStack Query v5 + openapi-fetch |
| Forms | React Hook Form + Zod |
| Styling | Tailwind CSS v4 |
| Auth | oidc-client-ts + react-oidc-context |
| E2E | Playwright |
