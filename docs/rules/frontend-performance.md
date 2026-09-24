# Frontend — Performance

## Rendering — let the compiler do it

The React Compiler (`babel-plugin-react-compiler`) memoises components and hooks automatically
when they follow the Rules of React. Enablement is tracked in #269; write code
as if it is on:

- **No hand memoisation by default.** `useMemo` / `useCallback` / `React.memo` only after
  profiling shows a specific re-render the compiler cannot prevent, with a comment stating the
  measurement. Today's 24 `useMemo` / 6 `useCallback` are removed opportunistically.
- **Keep components compilable:** no prop/state mutation, no `ref.current` reads during render,
  hooks unconditional, no side effects in render. `eslint-plugin-react-hooks` v7 flags what the
  compiler would skip.
- Stable references still matter for **third-party** children that compare by identity
  (Radix `onOpenChange`, chart libraries). Define constants outside the component; for
  callbacks the compiler handles it.

## Avoid the classic re-render traps

```tsx
// ❌ new object every render, defeats any memoisation
<MacroBar config={{ showLabels: true }} />
// ✅
const MACRO_BAR_CONFIG = { showLabels: true } as const;
<MacroBar config={MACRO_BAR_CONFIG} />
```

- Split contexts by update frequency (`ThemeContext` rarely, `ToastContext` often). A component
  that only needs `theme` must not re-render on every toast.
- Lift expensive derived data into the query layer (`select` on `queryOptions`) so it is computed
  once per fetch, not once per render.

## Code splitting

- Every page is `React.lazy` in the module's `index.ts`; the router wraps it in
  `SuspenseWrapper`. Nothing else needs manual splitting unless a dependency is heavy
  (`recharts` already sits behind the pages that use it).
- `vite.config.ts` splits `react`, `@tanstack/react-query`, `react-router-dom` into vendor chunks.
  Add a chunk only when the bundle visualiser shows a >100 kB dependency shared by few routes.

## Data fetching

```ts
// prefetch on intent
const queryClient = useQueryClient();
<Link to={`/diet-planner/products/${id}`} onMouseEnter={() => void queryClient.prefetchQuery(productOptions(id))} />

// parallel, not waterfall
const [goals, schedule] = useQueries({ queries: [goalsOptions(), mealScheduleOptions()] });
```

- `staleTime` defaults to 5 min (`shared/api/queryClient.ts`); `refetchOnWindowFocus` is off.
  Override per query only for data that must be fresh (notifications: 30 s + SignalR push).
- `placeholderData: keepPreviousData` on paginated lists — no flash of empty table.
- Mutations invalidate by key prefix (`queryKeys.products.all()`), never `queryClient.clear()`.
- Lists are paged server-side (`PagedList<T>`); never fetch "all" and filter in the client.

## Assets

- `<img loading="lazy" decoding="async" width height>` — dimensions always, to avoid layout shift.
- Icons: `lucide-react` named imports only (tree-shakeable); no icon fonts.

## Measuring before optimising

1. React DevTools Profiler — find the wasted render first.
2. Lighthouse / Web Vitals targets: LCP < 2.5 s, INP < 200 ms, CLS < 0.1.
3. `npx vite-bundle-visualizer` — what is in each chunk.
4. TanStack Query DevTools — cache hits, refetch storms.

An optimisation PR includes the before/after numbers from one of these.
