# Testing Standards

## Stack

| Layer | Tool |
|---|---|
| Unit / integration | Vitest + `@testing-library/react` |
| User events | `@testing-library/user-event` |
| E2E | Playwright |
| API mocking | MSW v2 (Mock Service Worker) |
| Assertions | `@testing-library/jest-dom` (auto-imported) |

---

## Vitest config

```ts
// vitest.config.ts
import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import path from "path";

export default defineConfig({
  plugins: [react()],
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: ["./src/test/setup.ts"],
    coverage: {
      provider: "v8",
      thresholds: { lines: 80, functions: 80, branches: 70 },
    },
  },
  resolve: {
    alias: { "@": path.resolve(__dirname, "src") },
  },
});
```

```ts
// src/test/setup.ts
import "@testing-library/jest-dom";
import { server } from "./mocks/server";

beforeAll(() => server.listen({ onUnhandledRequest: "error" }));
afterEach(() => server.resetHandlers());
afterAll(() => server.close());
```

---

## MSW handlers

```ts
// src/test/mocks/handlers.ts
import { http, HttpResponse } from "msw";
import { userFactory } from "../factories/user";

export const handlers = [
  http.get("/api/users/:id", ({ params }) => {
    return HttpResponse.json(userFactory({ id: String(params.id) }));
  }),

  http.post("/api/users", async ({ request }) => {
    const body = await request.json();
    return HttpResponse.json(userFactory(body as Partial<User>), { status: 201 });
  }),
];
```

```ts
// src/test/mocks/server.ts
import { setupServer } from "msw/node";
import { handlers } from "./handlers";

export const server = setupServer(...handlers);
```

---

## Test factories

```ts
// src/test/factories/user.ts
import type { User } from "@/features/auth";

let idCounter = 0;

export function userFactory(overrides: Partial<User> = {}): User {
  return {
    id: String(++idCounter),
    name: "Test User",
    email: "test@example.com",
    role: "viewer",
    ...overrides,
  };
}
```

---

## Component tests

```tsx
// UserCard.test.tsx
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { UserCard } from "./UserCard";
import { userFactory } from "@/test/factories/user";

describe("UserCard", () => {
  it("renders the user's name and email", () => {
    const user = userFactory({ name: "Ada Lovelace", email: "ada@example.com" });
    render(<UserCard user={user} onSelect={vi.fn()} />);

    expect(screen.getByText("Ada Lovelace")).toBeInTheDocument();
    expect(screen.getByText("ada@example.com")).toBeInTheDocument();
  });

  it("calls onSelect with the user id when clicked", async () => {
    const user = userFactory({ id: "u_123" });
    const onSelect = vi.fn();
    render(<UserCard user={user} onSelect={onSelect} />);

    await userEvent.click(screen.getByRole("button", { name: /select/i }));

    expect(onSelect).toHaveBeenCalledOnce();
    expect(onSelect).toHaveBeenCalledWith("u_123");
  });

  it("shows a loading skeleton while data is pending", () => {
    render(<UserCard user={undefined} isLoading onSelect={vi.fn()} />);
    expect(screen.getByTestId("skeleton")).toBeInTheDocument();
  });
});
```

### Query priorities (use in this order)

1. `getByRole` — most resilient to implementation changes
2. `getByLabelText` — for form fields
3. `getByPlaceholderText`
4. `getByText`
5. `getByDisplayValue`
6. `getByAltText`, `getByTitle`
7. `getByTestId` — last resort; add `data-testid` only when nothing else works

---

## Hook tests

```tsx
import { renderHook, waitFor } from "@testing-library/react";
import { createWrapper } from "@/test/utils/queryWrapper";
import { useUser } from "./useUser";

describe("useUser", () => {
  it("returns user data after successful fetch", async () => {
    const { result } = renderHook(() => useUser("u_1"), {
      wrapper: createWrapper(),
    });

    expect(result.current.isLoading).toBe(true);

    await waitFor(() => expect(result.current.isLoading).toBe(false));

    expect(result.current.user?.name).toBe("Test User");
  });
});
```

```tsx
// src/test/utils/queryWrapper.tsx
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

export function createWrapper() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return function Wrapper({ children }: { children: React.ReactNode }) {
    return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
  };
}
```

---

## What to test

| Worth testing | Skip / trust the library |
|---|---|
| User interactions & flows | Implementation details |
| Business logic in utils/hooks | Styling classes |
| Error states & edge cases | Library internals (React Query cache) |
| Accessibility attributes | Exact HTML structure |
| Data transformations | `console.log` calls |

---

## E2E — Playwright

```ts
// e2e/auth.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Authentication", () => {
  test("user can log in with valid credentials", async ({ page }) => {
    await page.goto("/login");

    await page.getByLabel("Email").fill("user@example.com");
    await page.getByLabel("Password").fill("password123");
    await page.getByRole("button", { name: "Sign in" }).click();

    await expect(page).toHaveURL("/dashboard");
    await expect(page.getByRole("heading", { name: "Dashboard" })).toBeVisible();
  });

  test("shows validation errors for empty form", async ({ page }) => {
    await page.goto("/login");
    await page.getByRole("button", { name: "Sign in" }).click();

    await expect(page.getByRole("alert")).toContainText("Invalid email");
  });
});
```

```ts
// playwright.config.ts
export default defineConfig({
  testDir: "./e2e",
  use: { baseURL: "http://localhost:5173", trace: "on-first-retry" },
  webServer: { command: "vite", port: 5173, reuseExistingServer: true },
});
```
