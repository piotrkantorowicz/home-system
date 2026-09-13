# React 19 + TypeScript 5.9 — Core Rules

## TypeScript

Baseline is `tsconfig.app.json`. Do not weaken any of these:

```jsonc
"strict": true,
"exactOptionalPropertyTypes": true,     // `foo?: string` ≠ `foo: string | undefined`
"noUncheckedIndexedAccess": true,       // arr[i] is T | undefined
"noImplicitReturns": true,
"noUnusedLocals": true, "noUnusedParameters": true,
"noFallthroughCasesInSwitch": true,
"noUncheckedSideEffectImports": true,
"verbatimModuleSyntax": true,           // `import type` is mandatory for types
"erasableSyntaxOnly": true,             // no enums, no namespaces, no parameter properties
"moduleResolution": "bundler", "module": "ESNext", "target": "ES2022", "jsx": "react-jsx"
```

Consequences worth knowing:

```ts
// ❌ enum — not erasable. Use a const object + derived union.
enum Role { Owner, Adult }
// ✅
export const ROLES = { owner: 'Owner', adult: 'Adult', child: 'Child', guest: 'Guest' } as const;
export type Role = (typeof ROLES)[keyof typeof ROLES];

// ✅ `import type` for anything only used as a type (verbatimModuleSyntax)
import type { Page, Locator } from '@playwright/test';

// ✅ optional prop means "may be absent", not "may be undefined"
interface Props { onClose?: () => void }
<Dialog {...(onClose ? { onClose } : {})} />        // not onClose={maybeUndefined}
```

### Types

- `interface` for props and object shapes that may be extended; `type` for unions, tuples,
  mapped types. Either is fine for plain shapes — be consistent within a file.
- Discriminated unions over optional-property soup for state (`{ status: 'success'; data }`).
- `unknown` + a type guard instead of `any`. `any` needs a `// REASON:` comment on the same line
  and is the only way past `@typescript-eslint/no-explicit-any`.
- `satisfies` to validate a literal against a type without widening.
- `as const` for lookup tables and route maps.
- No non-null assertions (`!`) — lint error. Narrow, or throw with a message.

## React

### Components

```tsx
export interface ProductCardProps {
  product: Product;
  onSelect?: (id: string) => void;
}

export function ProductCard({ product, onSelect }: ProductCardProps) {
  const { t } = useTranslation('diet-planner');
  return (
    <Card>
      <CardTitle>{product.name}</CardTitle>
      {onSelect && (
        <Button size="sm" onClick={() => onSelect(product.id)}>{t('products.select')}</Button>
      )}
    </Card>
  );
}
```

- Function components, named exports. `export default` **only** for `pages/*` consumed by
  `React.lazy`.
- No `React.FC`. Props interface named `<Component>Props`, exported when reused.
- One component per file, except tiny private subcomponents used only there.
- Hooks first, derived values next, handlers, early returns, render.

### React 19 — ref is a prop

`forwardRef` is deprecated in React 19. Components accept `ref` like any other prop.

```tsx
// ✅
export function Input({ ref, className, ...props }: React.ComponentProps<'input'>) {
  return <input ref={ref} className={cn(inputVariants(), className)} {...props} />;
}

// ❌ legacy (still present in shared/components/ui — migration tracked in #269)
export const Input = forwardRef<HTMLInputElement, InputProps>((props, ref) => …);
Input.displayName = 'Input';
```

Radix `asChild` composition works unchanged with ref-as-prop.

### React 19 — other features to use

| Need | Use |
|---|---|
| Read a promise or context in render | `use(promise)` / `use(Context)` inside `Suspense` |
| Instant feedback before a mutation resolves | `useOptimistic` |
| Form submit state without hand-rolled `isSubmitting` | `useActionState` (only for simple forms; react-hook-form forms already expose `formState`) |
| Non-urgent updates (search filter, tab switch) | `useTransition` |
| Document title / meta | render `<title>` / `<meta>` in the page component — React 19 hoists them |

### React Compiler — no hand memoisation

The project targets the React Compiler (`babel-plugin-react-compiler` via
`@vitejs/plugin-react`; enablement is tracked in #269, the lint rules from
`eslint-plugin-react-hooks` v7 are already active). Write components as plain functions and let
the compiler memoise:

- No `useMemo` / `useCallback` / `React.memo` **unless** profiling shows a specific problem the
  compiler cannot solve (a genuinely expensive pure computation, a third-party child that
  requires referential stability). Leave a comment with the measured reason.
- Follow the Rules of React strictly — the compiler skips components that break them:
  no mutation of props/state, no reading refs during render, no conditional hooks.
- Existing `useMemo` for cheap derived values is removed opportunistically when a file is touched.

### Hooks

```ts
export function useProducts(params: ProductListParams) {
  return useQuery({
    ...productListOptions(params),
    placeholderData: keepPreviousData,
  });
}
```

- Custom hooks: single responsibility, typed return, `use` prefix.
- `useEffect` is for synchronising with something outside React (subscriptions, the OIDC
  manager, a DOM API). Not for deriving state, not for "run on mount" data loading — that is a
  query.
- `react-hooks/exhaustive-deps` is an error. Fix the dependency, do not disable the rule.

### State

| Kind | Where |
|---|---|
| Server state | TanStack Query — `queryOptions` + `useQuery` / `useSuspenseQuery` / `useMutation` |
| Global UI state (theme, toasts, current household) | React Context in `shared/context` or a module's provider; split contexts by update frequency |
| Local | `useState`; `useReducer` with a discriminated-union action for multi-field state |
| URL state (filters, page, tab) | search params via React Router — not `useState` |
| Persisted preferences | `usePreferences` (localStorage-backed) |

No Zustand / Redux. If a context re-renders too much, split it — do not add a store.

### Forms — react-hook-form + Zod 4

```tsx
const schema = z.object({
  name: z.string().trim().min(1, t('validation.required')),
  email: z.email(t('validation.email')),          // Zod 4: top-level validators, not z.string().email()
  targetMl: z.coerce.number().int().min(500).max(6000),
});
type FormValues = z.infer<typeof schema>;

const form = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues });
```

- Schema per form, co-located with the form component. Messages via `t()`.
- Use the shared `Field` component for label + input + error wiring (`aria-invalid`,
  `aria-describedby`, `role="alert"` on the message).
- Submit handlers `async`, errors surfaced through `ToastContext`, never `alert()`.

### Suspense & errors

- Every lazy page is wrapped in `SuspenseWrapper`; data-heavy sections may use
  `useSuspenseQuery` inside their own `<Suspense fallback={<Skeleton />}>`.
- `react-error-boundary` (`shared/components/ErrorBoundary`) around each module route tree.
  No custom class boundaries.

### Events

- Handler props are `onXxx`; implementations are `handleXxx`.
- Type events explicitly: `React.ChangeEvent<HTMLInputElement>`.

### Accessibility

- Semantic elements first (`button`, `nav`, `main`, `dialog`); Radix for anything with focus
  management. `aria-*` only when semantics are insufficient.
- Every interactive element reachable by keyboard with a visible focus ring
  (`focus-visible:ring-2` is in the primitives — do not remove it).
- Dynamic error text: `role="alert"`; polite updates: `aria-live="polite"`.
- `eslint-plugin-jsx-a11y` recommended rules are errors.

## i18n

- All user-visible text through `useTranslation('<namespace>')` — no literals in JSX.
- Add both `en` and `pl` keys in the same commit. Missing-key warnings fail the Vitest run
  when `i18n.ts` runs in test mode.
- One label per element in the active language — never render both languages.

## Async

```ts
// ✅ typed error from openapi-fetch — throw so TanStack Query sees it
const { data, error } = await api.GET('/api/v1/goals');
if (error) throw error;

// ✅ AbortController for anything outside TanStack Query (rare)
useEffect(() => {
  const controller = new AbortController();
  void subscribe(controller.signal);
  return () => controller.abort();
}, []);
```

- No floating promises (lint error) — `await`, `void`, or return them.
- Never swallow errors; log with `console.error` only in infrastructure code (`client.ts`,
  `tokenInterceptor.ts`). Components surface errors through UI.
