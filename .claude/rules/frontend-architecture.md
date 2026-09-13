# Frontend — Architecture & File Structure

Vite 7 SPA. React 19, TypeScript 5.9, **React Router 7** (`react-router-dom`), TanStack Query
v5, Tailwind v4, Radix primitives, react-hook-form + Zod 4, i18next, `openapi-fetch` against
generated OpenAPI types, `oidc-client-ts` for Authentik.

## Project layout (`src/ui/src`)

```
app/                         # Bootstrap only
  main.tsx                   # providers: QueryClient, OIDC, Theme, Toast, i18n, RouterProvider
  router.tsx                 # createBrowserRouter — routes assembled from the module registry
  SuspenseWrapper.tsx
  RootRedirect.tsx

modules/                     # ← PRIMARY DOMAIN CODE — one folder per backend module
  diet-planner/
    api/
      client.ts              # openapi-fetch client + auth middleware (one per module)
      generated/schema.ts    # `npm run generate:api:diet-planner` — never edit by hand
      queryKeys.ts           # key factory for this module
      hooks/                 # useProducts.ts, useGoals.ts, … (queries + mutations per resource)
    components/              # UI specific to this module
    pages/                   # route components — the ONLY default exports (React.lazy)
    hooks/                   # non-API module hooks
    locales/{en,pl}.json     # module i18n namespace
    utils/
    index.ts                 # exports the AppModule (routes, navItems, i18nResources, widgets)
  notifications/
  household/                 # same shape

shared/                      # Domain-agnostic — never imports from modules/
  api/                       # queryClient.ts, tokenInterceptor.ts
  auth/                      # userManager, ProtectedRoute, authConfig
  components/
    ui/                      # primitives: Button, Card, Dialog, Sheet, Field, Select, … + index.ts
    layout/                  # AppShell, ModuleRail, SectionPanel, Header, BottomTabBar, CommandPalette
    ErrorBoundary.tsx
  context/                   # ThemeContext, ToastContext
  hooks/                     # usePreferences, …
  lib/                       # module-registry.ts, i18n.ts, utils.ts (cn, formatNumber)
  locales/                   # shared i18n keys

test/                        # Vitest infrastructure
  setup.ts                   # jest-dom/vitest + MSW server lifecycle
  mocks/{handlers,server}.ts
  factories/                 # productFactory, …
  utils/queryWrapper.tsx
```

Path aliases: `@/*` → `src/*`, `@shared/*` → `src/shared/*`, `@modules/*` → `src/modules/*`
(declared in `tsconfig.app.json`, `vite.config.ts`, `vitest.config.ts` — keep all three in sync).

## Module registry

Every module exports one `AppModule` from its `index.ts` and registers it once in
`app/router.tsx`. Sidebar, routes, command palette, dashboard widgets and i18n namespaces are all
derived from the registry — a module never touches the shell directly.

```ts
// modules/household/index.ts
export const householdModule: AppModule = {
  name: 'household',
  translationKey: 'common.household',
  basePath: '/household',
  icon: Users,
  localeNamespaces: ['household'],
  i18nResources: { en: { household: en }, pl: { household: pl } },
  navItems: [{ name: 'Members', href: '/household/members', icon: Users, translationKey: 'household.members' }],
  routes: [{ path: 'members', element: <Suspense><MembersPage /></Suspense> }],
};
```

Routes are module-prefixed (`/diet-planner/products`, `/household/members`). Pages are
`React.lazy` imports — the one sanctioned use of `export default`.

## Naming conventions

| Thing | Convention | Example |
|---|---|---|
| Component file | PascalCase | `ProductCard.tsx` |
| Hook file | camelCase, `use` prefix | `useProducts.ts` |
| Page file | PascalCase, under `pages/` | `pages/products/ProductList.tsx` |
| Utility file | camelCase | `unitLabel.ts` |
| Types / interfaces | PascalCase | `ProductDto`, `AppModule` |
| Constants | SCREAMING_SNAKE | `TOKEN_EXPIRY_BUFFER_SECONDS` |
| Test file | co-located `*.test.ts(x)` | `useGoals.test.ts` |
| i18n key | snake_case, namespaced | `diet-planner:products.create_title` |
| Query key | via `queryKeys.<resource>.<shape>()` | `queryKeys.products.detail(id)` |

## Module boundaries

```ts
// ✅ another module consumes only the public API
import { useHousehold } from '@modules/household';

// ❌ reaching into internals
import { useHouseholdQuery } from '@modules/household/api/hooks/useHouseholdQuery';
```

- `index.ts` exists **only** at the module root and in `shared/components/ui`. No barrel files
  inside `api/`, `components/`, `pages/` — they break tree-shaking and slow the TS server.
- Dependency direction: `app → modules → shared`. `shared/` never imports `modules/`.
  `shared/lib/utils.ts` has no React import.
- A backend Contracts-level relationship (e.g. diet-planner consuming household context) is
  expressed the same way here: through the other module's `index.ts` exports.

## API layer

One `openapi-fetch` client per module, typed from that module's generated schema. The client
carries the auth middleware (bearer from `tokenInterceptor`, silent renew + retry on 401).

```ts
// modules/diet-planner/api/client.ts
const baseClient = createClient<paths>({ baseUrl });
baseClient.use(authMiddleware);
export const api = baseClient;

// modules/diet-planner/api/queryKeys.ts
export const queryKeys = {
  products: {
    all: () => ['products'] as const,
    list: (params: ProductListParams) => ['products', params] as const,
    detail: (id: string) => ['products', id] as const,
  },
} as const;

// modules/diet-planner/api/hooks/useProducts.ts
export function productOptions(id: string) {
  return queryOptions({
    queryKey: queryKeys.products.detail(id),
    queryFn: async () => {
      const { data, error } = await api.GET('/api/v1/products/{id}', { params: { path: { id } } });
      if (error) throw error;
      return mapProduct(data);
    },
  });
}

export function useProduct(id: string) {
  return useQuery(productOptions(id));
}

export function useCreateProduct() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateProductRequest) => api.POST('/api/v1/products', { body }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.products.all() }),
  });
}
```

- Generated `components['schemas']['XDto']` types are the wire shape. Map them into a
  module-local type at the hook boundary when the UI needs different names or numeric coercion
  (the backend serialises `decimal` as string) — never spread raw DTOs through components.
- `queryOptions()` (TanStack v5) is the unit of reuse: the same object feeds `useQuery`,
  `useSuspenseQuery`, `prefetchQuery` and route loaders.
- Never call `fetch` directly for API calls; the client is the only place that knows about tokens.
- After any backend contract change: `npm run generate:api:<module>` and commit `schema.ts`.

## Environment

`import.meta.env.VITE_*` is read in exactly two places: `authConfig.ts` (OIDC) and each
module's `api/client.ts` (`VITE_API_BASE_URL`). Nowhere else.

## File order inside a component file

1. Imports (external → `@shared` → `@modules` → relative → `import type` last; ESLint enforces)
2. Constants
3. Types / interfaces (`export interface XProps`)
4. Pure helpers
5. Component (`export function X`)
6. Small co-located subcomponents
