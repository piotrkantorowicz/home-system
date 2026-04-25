---
description: Scaffold a new frontend feature module following the project's architecture conventions
---

Scaffold a new frontend feature module following the project's architecture conventions.

## Usage

```
/scaffold-feature <FeatureName>
```

Example: `/scaffold-feature budget-plans`

## Instructions

Read `.claude/rules/frontend-architecture.md` and `.claude/rules/frontend-react-typescript.md` before generating.

Given `FeatureName` (kebab-case input), derive:
- Folder name: `modules/{featureName}/` (kebab-case)
- Type prefix: PascalCase version (e.g. `budget-plans` → `BudgetPlan`)

### Generate:

**`src/modules/{featureName}/`**

```
components/
  {TypePrefix}List.tsx          — main list component with loading/error states
  {TypePrefix}Card.tsx          — individual item card
hooks/
  use{TypePrefix}.ts            — single item query hook using TanStack Query
  use{TypePrefix}s.ts           — list query hook with filters
api/
  {featureName}.api.ts          — API client functions (get, list, create, update, delete)
  {featureName}.queries.ts      — TanStack Query key factory + hook definitions
types.ts                        — feature-local types (main entity + DTOs)
utils.ts                        — pure helper functions (empty, ready for use)
index.ts                        — public API barrel export
```

### File contents:

**`types.ts`**
```tsx
export type {TypePrefix} = {
  id: string;
  // TODO: add fields
  createdAt: string;
};

export type Create{TypePrefix}Payload = {
  // TODO: add fields
};
```

**`api/{featureName}.api.ts`**
```tsx
import { apiClient } from "@/lib/api";
import type { {TypePrefix}, Create{TypePrefix}Payload } from "../types";

export async function get{TypePrefix}(id: string): Promise<{TypePrefix}> {
  const { data } = await apiClient.get<{TypePrefix}>(`/api/{featureName}/${id}`);
  return data;
}

export async function get{TypePrefix}s(): Promise<{TypePrefix}[]> {
  const { data } = await apiClient.get<{TypePrefix}[]>(`/api/{featureName}`);
  return data;
}

export async function create{TypePrefix}(payload: Create{TypePrefix}Payload): Promise<{TypePrefix}> {
  const { data } = await apiClient.post<{TypePrefix}>(`/api/{featureName}`, payload);
  return data;
}
```

**`api/{featureName}.queries.ts`**
```tsx
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { get{TypePrefix}, get{TypePrefix}s, create{TypePrefix} } from "./{featureName}.api";
import type { Create{TypePrefix}Payload } from "../types";

export const {camelPrefix}Keys = {
  all: ["{featureName}"] as const,
  detail: (id: string) => [...{camelPrefix}Keys.all, id] as const,
};

/** Fetch a single {TypePrefix} by ID */
export function use{TypePrefix}(id: string) {
  return useQuery({
    queryKey: {camelPrefix}Keys.detail(id),
    queryFn: () => get{TypePrefix}(id),
  });
}

/** Fetch all {TypePrefix}s */
export function use{TypePrefix}s() {
  return useQuery({
    queryKey: {camelPrefix}Keys.all,
    queryFn: get{TypePrefix}s,
  });
}

/** Create a new {TypePrefix} */
export function useCreate{TypePrefix}() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: Create{TypePrefix}Payload) => create{TypePrefix}(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: {camelPrefix}Keys.all });
    },
  });
}
```

**`index.ts`**
```tsx
// Public API — only export what other modules need
export { {TypePrefix}List } from "./components/{TypePrefix}List";
export { {TypePrefix}Card } from "./components/{TypePrefix}Card";
export { use{TypePrefix}, use{TypePrefix}s } from "./api/{featureName}.queries";
export type { {TypePrefix}, Create{TypePrefix}Payload } from "./types";
```

**Components** should follow standard patterns:
- Props interface exported
- JSDoc summary
- Loading/error/empty states handled
- Named export only

### After generating:

- Add MSW handlers in `src/test/mocks/handlers.ts` for the new API endpoints
- Add a test factory in `src/test/factories/{featureName}.ts`
- Remind the user to add a route in `app/router.tsx`
