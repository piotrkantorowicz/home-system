# Performance

## Rendering

### Memoisation — only when measured

```tsx
// ✅ memo: only for components that re-render often with same props
const ProductCard = memo(function ProductCard({ product }: ProductCardProps) {
  return …;
});

// ✅ useMemo: only for expensive pure computations
const sortedItems = useMemo(
  () => [...items].sort(compareByDate),
  [items]
);

// ✅ useCallback: only for stable references passed to memo'd children
const handleSelect = useCallback((id: string) => {
  dispatch({ type: "select", id });
}, [dispatch]);

// ❌ Premature: don't memo trivial renders — profiler first
const Label = memo(({ text }: { text: string }) => <span>{text}</span>);
```

### Avoid unnecessary re-renders

```tsx
// ❌ Object / array created in render → new reference every time
<List config={{ sort: "asc", filter: "active" }} />

// ✅ Stable reference
const LIST_CONFIG = { sort: "asc", filter: "active" } as const;
<List config={LIST_CONFIG} />

// ❌ Inline function prop recreated on every parent render
<Button onClick={() => doSomething(id)} />

// ✅ Stable via useCallback (only if Button is memo'd)
const handleClick = useCallback(() => doSomething(id), [id]);
<Button onClick={handleClick} />
```

### Context performance

```tsx
// ❌ Single context for everything = entire tree re-renders on any change
const AppContext = createContext<AppState>(…);

// ✅ Split contexts by update frequency
const UserContext  = createContext<User | null>(null);    // rarely changes
const ThemeContext = createContext<Theme>("light");         // occasionally
const FilterContext = createContext<Filters>(…);           // often
```

---

## Code splitting

```tsx
// Lazy-load route-level components
const SettingsPage = lazy(() => import("@/pages/settings/SettingsPage"));
const DashboardPage = lazy(() => import("@/pages/dashboard/DashboardPage"));

// Wrap with Suspense at the router level
<Suspense fallback={<PageSkeleton />}>
  <Routes>…</Routes>
</Suspense>

// Lazy-load heavy feature components
const RichTextEditor = lazy(() =>
  import("@/modules/editor/components/RichTextEditor")
);
```

---

## Data fetching

```ts
// Prefetch on hover / intent
function ProductLink({ id }: { id: string }) {
  const queryClient = useQueryClient();

  function prefetch() {
    queryClient.prefetchQuery({
      queryKey: productKeys.detail(id),
      queryFn: () => getProduct(id),
    });
  }

  return (
    <Link to={`/products/${id}`} onMouseEnter={prefetch}>
      View product
    </Link>
  );
}

// Parallel queries instead of waterfall
const [user, permissions] = await Promise.all([
  getUser(id),
  getPermissions(id),
]);

// Appropriate stale times
useQuery({
  queryKey: ["config"],
  queryFn: fetchConfig,
  staleTime: Infinity,     // config rarely changes
});

useQuery({
  queryKey: ["notifications"],
  queryFn: fetchNotifications,
  staleTime: 1000 * 30,   // 30 s
  refetchInterval: 1000 * 60, // poll every 60 s
});
```

---

## Bundle

```ts
// vite.config.ts — manual chunk splitting
build: {
  rollupOptions: {
    output: {
      manualChunks: {
        "react-vendor": ["react", "react-dom"],
        "query-vendor": ["@tanstack/react-query"],
        "router-vendor": ["@tanstack/react-router"],
      },
    },
  },
},
```

### Image optimisation

```tsx
// Use native lazy loading
<img src={src} alt={alt} loading="lazy" decoding="async" />

// Provide dimensions to avoid layout shift
<img src={src} alt={alt} width={800} height={600} loading="lazy" />

// Prefer <picture> for art direction or format switching
<picture>
  <source srcSet="hero.avif" type="image/avif" />
  <source srcSet="hero.webp" type="image/webp" />
  <img src="hero.jpg" alt="Hero" width={1920} height={1080} />
</picture>
```

---

## Measuring

1. **React DevTools Profiler** — identify wasted renders before optimising.
2. **Lighthouse / Web Vitals** — LCP < 2.5 s, INP < 200 ms, CLS < 0.1.
3. **`vite-bundle-visualizer`** — understand what is in each chunk.
4. **TanStack Query DevTools** — cache state and refetch behaviour.

```ts
// Track Core Web Vitals
import { onCLS, onINP, onLCP } from "web-vitals";

onCLS(console.log);
onINP(console.log);
onLCP(console.log);
```
