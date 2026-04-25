# React 19 + TypeScript 5 — Core Rules

## TypeScript

### Config baseline (`tsconfig.json`)

```json
{
  "compilerOptions": {
    "strict": true,
    "exactOptionalPropertyTypes": true,
    "noUncheckedIndexedAccess": true,
    "noImplicitReturns": true,
    "noFallthroughCasesInSwitch": true,
    "forceConsistentCasingInFileNames": true,
    "moduleResolution": "bundler",
    "module": "ESNext",
    "target": "ES2022",
    "lib": ["ES2022", "DOM", "DOM.Iterable"],
    "jsx": "react-jsx",
    "paths": {
      "@/*": ["./src/*"]
    }
  }
}
```

### Types

```ts
// ✅ Use `type` for data shapes, `interface` for extensible contracts
type User = {
  id: string;
  name: string;
  email: string;
};

interface Repository<T> {
  findById(id: string): Promise<T | null>;
  save(entity: T): Promise<T>;
}

// ✅ Prefer discriminated unions over optional properties
type Result<T> =
  | { status: "success"; data: T }
  | { status: "error"; error: Error }
  | { status: "loading" };

// ❌ Avoid
type Result<T> = {
  data?: T;
  error?: Error;
  isLoading?: boolean;
};

// ✅ Use `satisfies` to validate shape without widening
const config = {
  apiUrl: "https://api.example.com",
  timeout: 5000,
} satisfies AppConfig;

// ✅ Use `unknown` + type guards instead of `any`
function parseJson(raw: unknown): User {
  if (!isUser(raw)) throw new TypeError("Invalid user payload");
  return raw;
}

function isUser(value: unknown): value is User {
  return (
    typeof value === "object" &&
    value !== null &&
    "id" in value &&
    "name" in value
  );
}

// ✅ Const assertions for literal inference
const ROUTES = {
  home: "/",
  profile: "/profile",
  settings: "/settings",
} as const;

type Route = (typeof ROUTES)[keyof typeof ROUTES];
```

---

## React

### Component authoring

```tsx
// ✅ Function components only (no class components)
// ✅ Named exports only (exception: page-level route components)
// ✅ Props interface above the component

export interface ButtonProps {
  label: string;
  variant?: "primary" | "secondary" | "ghost";
  isLoading?: boolean;
  onClick?: () => void;
}

export function Button({
  label,
  variant = "primary",
  isLoading = false,
  onClick,
}: ButtonProps) {
  return (
    <button
      className={buttonVariants({ variant })}
      disabled={isLoading}
      onClick={onClick}
      type="button"
    >
      {isLoading ? <Spinner aria-hidden /> : null}
      {label}
    </button>
  );
}
```

### React 19 features — use them

```tsx
// ✅ use() hook for async resources and context
import { use } from "react";

function UserProfile({ userPromise }: { userPromise: Promise<User> }) {
  const user = use(userPromise); // suspends automatically
  return <h1>{user.name}</h1>;
}

// ✅ Server Actions (if using Next.js / frameworks with RSC)
async function updateUser(formData: FormData) {
  "use server";
  const name = formData.get("name");
  await db.user.update({ data: { name } });
}

// ✅ useOptimistic for immediate UI feedback
import { useOptimistic } from "react";

function TodoList({ todos }: { todos: Todo[] }) {
  const [optimisticTodos, addOptimistic] = useOptimistic(todos);

  async function addTodo(text: string) {
    addOptimistic([...optimisticTodos, { id: "temp", text, done: false }]);
    await api.addTodo(text);
  }
  // ...
}

// ✅ useTransition for non-urgent state updates
import { useTransition } from "react";

function SearchBar() {
  const [isPending, startTransition] = useTransition();
  const [query, setQuery] = useState("");

  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    startTransition(() => setQuery(e.target.value));
  }
  // ...
}

// ✅ ref as prop (React 19 — no forwardRef needed)
export function Input({ ref, ...props }: React.ComponentProps<"input">) {
  return <input ref={ref} {...props} />;
}
```

### Hooks rules

```tsx
// ✅ Custom hooks: single responsibility, typed return
export function useUser(userId: string) {
  const { data, isLoading, error } = useQuery({
    queryKey: ["user", userId],
    queryFn: () => api.getUser(userId),
  });

  return { user: data, isLoading, error } as const;
}

// ✅ useCallback only when passing to memoised children or in dep arrays
// ✅ useMemo only for expensive computations (benchmark first!)
// ❌ Don't wrap everything in useCallback/useMemo by default

// ✅ Stable references via useRef for callbacks that don't need re-render
function useStableCallback<T extends (...args: unknown[]) => unknown>(fn: T) {
  const ref = useRef(fn);
  useEffect(() => { ref.current = fn; });
  return useCallback((...args: Parameters<T>) => ref.current(...args), []);
}
```

### State management

```tsx
// Local: useState / useReducer
// Complex local: useReducer with discriminated union actions
type Action =
  | { type: "increment" }
  | { type: "decrement" }
  | { type: "reset"; payload: number };

function reducer(state: number, action: Action): number {
  switch (action.type) {
    case "increment": return state + 1;
    case "decrement": return state - 1;
    case "reset":     return action.payload;
  }
}

// Global/shared: Zustand
import { create } from "zustand";
import { immer } from "zustand/middleware/immer";

interface CartStore {
  items: CartItem[];
  addItem: (item: CartItem) => void;
  removeItem: (id: string) => void;
}

export const useCartStore = create<CartStore>()(
  immer((set) => ({
    items: [],
    addItem: (item) =>
      set((state) => { state.items.push(item); }),
    removeItem: (id) =>
      set((state) => {
        state.items = state.items.filter((i) => i.id !== id);
      }),
  }))
);

// Server state: TanStack Query
export function useProducts(filters: ProductFilters) {
  return useQuery({
    queryKey: ["products", filters],
    queryFn: () => api.getProducts(filters),
    staleTime: 1000 * 60 * 5, // 5 min
    placeholderData: keepPreviousData,
  });
}

// Mutations
export function useCreateProduct() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: api.createProduct,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["products"] });
    },
  });
}
```

### Forms

```tsx
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";

const loginSchema = z.object({
  email: z.string().email("Invalid email"),
  password: z.string().min(8, "Min 8 characters"),
});

type LoginFormValues = z.infer<typeof loginSchema>;

export function LoginForm() {
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
  });

  const onSubmit = async (data: LoginFormValues) => {
    await auth.login(data);
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate>
      <input {...register("email")} type="email" aria-invalid={!!errors.email} />
      {errors.email && <p role="alert">{errors.email.message}</p>}

      <input {...register("password")} type="password" />
      {errors.password && <p role="alert">{errors.password.message}</p>}

      <button type="submit" disabled={isSubmitting}>
        {isSubmitting ? "Signing in…" : "Sign in"}
      </button>
    </form>
  );
}
```

### Error boundaries

```tsx
// Use react-error-boundary — don't write custom class components
import { ErrorBoundary } from "react-error-boundary";

function ErrorFallback({ error, resetErrorBoundary }: FallbackProps) {
  return (
    <div role="alert">
      <p>Something went wrong: {error.message}</p>
      <button onClick={resetErrorBoundary}>Retry</button>
    </div>
  );
}

// Wrap data-fetching subtrees
<ErrorBoundary FallbackComponent={ErrorFallback} onReset={reset}>
  <Suspense fallback={<Skeleton />}>
    <ProductList />
  </Suspense>
</ErrorBoundary>
```

### Events & callbacks

```tsx
// ✅ Always type event handlers explicitly
function handleChange(event: React.ChangeEvent<HTMLInputElement>) {
  setValue(event.target.value);
}

// ✅ Prefer callback naming: onXxx (props), handleXxx (implementations)
interface FormProps {
  onSubmit: (data: FormData) => void; // prop: onXxx
}

function Form({ onSubmit }: FormProps) {
  function handleSubmit(e: React.FormEvent) { // impl: handleXxx
    e.preventDefault();
    onSubmit(new FormData(e.currentTarget as HTMLFormElement));
  }
  return <form onSubmit={handleSubmit}>…</form>;
}
```

### Accessibility

- Every interactive element must be keyboard-reachable.
- Use semantic HTML first (`<button>`, `<nav>`, `<main>`, `<dialog>`).
- Add `aria-*` only when semantic HTML isn't sufficient.
- Every image needs `alt`; decorative images get `alt=""`.
- Use `role="alert"` for dynamic error messages.
- Use `aria-live="polite"` for non-critical updates.
- Maintain visible focus indicators — never `outline: none` without replacement.

---

## Async patterns

```ts
// ✅ Never swallow errors silently
async function fetchUser(id: string): Promise<User> {
  const response = await fetch(`/api/users/${id}`);
  if (!response.ok) {
    throw new Error(`Failed to fetch user ${id}: ${response.statusText}`);
  }
  return response.json() as Promise<User>;
}

// ✅ Use AbortController for cancellable fetches
function useUserData(id: string) {
  useEffect(() => {
    const controller = new AbortController();

    fetch(`/api/users/${id}`, { signal: controller.signal })
      .then((r) => r.json())
      .then(setUser)
      .catch((e) => {
        if (e.name !== "AbortError") setError(e);
      });

    return () => controller.abort();
  }, [id]);
}
```
