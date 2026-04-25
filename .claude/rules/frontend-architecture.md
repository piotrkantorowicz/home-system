# Architecture & File Structure

## Project layout

```
src/
├── app/                    # App shell, providers, global layout
│   ├── App.tsx
│   ├── providers.tsx       # All context/query/router providers
│   └── router.tsx          # TanStack Router definition
│
├── pages/                  # Route-level components (lazy loaded)
│   ├── home/
│   │   ├── HomePage.tsx
│   │   └── index.ts        # re-export only
│   └── settings/
│       └── SettingsPage.tsx
│
├── modules/                # ← PRIMARY DOMAIN CODE (one folder per backend module)
│   └── [module]/           # e.g. diet-planner, notifications
│       ├── components/     # UI specific to this module
│       ├── hooks/          # Module-scoped custom hooks
│       ├── api/            # API calls + TanStack Query definitions
│       ├── store/          # Zustand slice (if needed)
│       ├── types.ts        # Module-local types
│       ├── utils.ts        # Pure helpers
│       └── index.ts        # Public API — ONLY export what other modules need
│
├── components/             # Truly shared, domain-agnostic UI
│   ├── ui/                 # Primitive components (Button, Input, Modal…)
│   └── layout/             # Header, Sidebar, PageShell…
│
├── hooks/                  # App-wide custom hooks
├── lib/                    # Third-party wrappers, SDK init, utilities
├── types/                  # Global TS types & declaration files
└── utils/                  # Pure, stateless utility functions
```

---

## Naming conventions

| Thing | Convention | Example |
|---|---|---|
| Component file | PascalCase | `UserCard.tsx` |
| Hook file | camelCase | `useUserCard.ts` |
| Utility file | camelCase | `formatCurrency.ts` |
| Type/Interface | PascalCase | `UserCard`, `ApiResponse<T>` |
| Constant | SCREAMING_SNAKE | `MAX_RETRIES` |
| Enum | PascalCase keys | `Status.Active` |
| CSS variable | kebab-case | `--color-primary` |
| Test file | `*.test.tsx` | `UserCard.test.tsx` |
| Story file | `*.stories.tsx` | `UserCard.stories.tsx` |

---

## Module rules

### Public API via `index.ts`

Every module exposes **only** what other modules need through its `index.ts`:

```ts
// modules/auth/index.ts
export { LoginForm } from "./components/LoginForm";
export { useCurrentUser } from "./hooks/useCurrentUser";
export type { User, AuthState } from "./types";

// NOT exported (internal):
// - useLoginMutation (internal hook)
// - authStore (internal state)
// - loginFormSchema (internal validation)
```

### Cross-module imports

```ts
// ✅ Import from module public API
import { useCurrentUser } from "@/modules/auth";

// ❌ Never reach into module internals
import { useLoginMutation } from "@/modules/auth/hooks/useLoginMutation";
```

### Dependency direction

```
pages → modules → components/ui → lib → utils
```

- Pages import from modules.
- Modules import from shared `components`, `hooks`, `lib`, `utils`.
- Shared components must **never** import from modules.
- `utils` has zero React dependencies.

---

## File structure within a component

Order within a `.tsx` file:

1. Imports (external → internal, types last)
2. Constants / static data
3. Types & interfaces
4. Helper functions (pure, not hooks)
5. Custom hooks used only by this component
6. Component function
7. Subcomponents (if small enough to co-locate)

```tsx
// 1. Imports
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";

import { api } from "@/lib/api";
import { formatDate } from "@/utils/date";
import type { User } from "@/modules/auth";

// 2. Constants
const STALE_TIME = 1000 * 60 * 5;

// 3. Types
export interface UserCardProps {
  userId: string;
  onSelect?: (user: User) => void;
}

// 4. Helper (pure)
function formatUserLabel(user: User) {
  return `${user.name} (${user.email})`;
}

// 5. Component
export function UserCard({ userId, onSelect }: UserCardProps) {
  // hooks first, in order of dependency
  const { data: user, isLoading } = useQuery({ ... });

  // derived state / memos
  const label = user ? formatUserLabel(user) : "—";

  // handlers
  function handleClick() {
    if (user) onSelect?.(user);
  }

  // early returns (guards)
  if (isLoading) return <Skeleton />;
  if (!user) return null;

  // render
  return (
    <div onClick={handleClick}>
      {label}
    </div>
  );
}
```

---

## Module boundaries & barrel files

- **Use `index.ts` only at module boundaries**, not inside module subdirectories.
- Avoid deep barrel chains — they destroy tree-shaking and slow down TS Language Server.
- Path aliases (`@/`) should point to `src/` only.

```ts
// tsconfig paths
"paths": {
  "@/*": ["./src/*"]
}

// vite.config.ts
resolve: {
  alias: { "@": path.resolve(__dirname, "src") }
}
```

---

## Environment variables

```ts
// lib/env.ts — validate at startup, never scatter process.env throughout code
import { z } from "zod";

const envSchema = z.object({
  VITE_API_URL: z.string().url(),
  VITE_APP_ENV: z.enum(["development", "staging", "production"]),
});

export const env = envSchema.parse(import.meta.env);
```

---

## API layer

```ts
// lib/api/client.ts — single Axios / fetch instance
import axios from "axios";
import { env } from "@/lib/env";

export const apiClient = axios.create({
  baseURL: env.VITE_API_URL,
  timeout: 10_000,
});

// Attach auth token
apiClient.interceptors.request.use((config) => {
  const token = tokenStore.get();
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// modules/users/api/users.api.ts
export async function getUser(id: string): Promise<User> {
  const { data } = await apiClient.get<User>(`/users/${id}`);
  return data;
}

// modules/users/api/users.queries.ts  ← TanStack Query definitions
export const userKeys = {
  all: ["users"] as const,
  detail: (id: string) => [...userKeys.all, id] as const,
};

export function useUser(id: string) {
  return useQuery({
    queryKey: userKeys.detail(id),
    queryFn: () => getUser(id),
  });
}
```
