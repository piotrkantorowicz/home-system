# Playwright — E2E Testing Rules

## Philosophy

E2E tests cover **user journeys**, not component internals. One good E2E test
that walks through a real flow is worth more than ten that prod individual
buttons in isolation. Write them from the perspective of a user who knows
nothing about the codebase.

---

## Config

```ts
// playwright.config.ts
import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,   // fail CI if test.only is left in
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [
    ["html", { outputFolder: "playwright-report", open: "never" }],
    ["list"],                      // concise console output
  ],
  use: {
    baseURL: process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:5173",
    trace: "on-first-retry",       // record trace on failure for debugging
    screenshot: "only-on-failure",
    video: "retain-on-failure",
    actionTimeout: 10_000,         // per-action timeout
    navigationTimeout: 30_000,
  },
  projects: [
    // Setup project for auth state (runs once before tests)
    { name: "setup", testMatch: "**/e2e/setup/*.ts" },

    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
      dependencies: ["setup"],
    },
    {
      name: "firefox",
      use: { ...devices["Desktop Firefox"] },
      dependencies: ["setup"],
    },
    {
      name: "webkit",
      use: { ...devices["Desktop Safari"] },
      dependencies: ["setup"],
    },
    // Mobile smoke
    {
      name: "mobile-chrome",
      use: { ...devices["Pixel 5"] },
      dependencies: ["setup"],
      testMatch: "**/e2e/smoke/**",
    },
  ],
  webServer: {
    command: "npm run dev",
    port: 5173,
    reuseExistingServer: !process.env.CI,
  },
});
```

---

## Directory layout

```
e2e/
├── setup/
│   └── auth.setup.ts           # global auth state setup
├── fixtures/
│   ├── index.ts                # barrel — export all custom fixtures
│   ├── auth.fixture.ts
│   └── data.fixture.ts
├── pages/                      # Page Object Models
│   ├── BasePage.ts
│   ├── LoginPage.ts
│   ├── DashboardPage.ts
│   └── index.ts
├── smoke/                      # Fast critical-path tests (run on every deploy)
│   └── critical-paths.spec.ts
├── auth/
│   └── login.spec.ts
├── products/
│   └── product-management.spec.ts
└── utils/
    └── db.ts                   # Test data seeding helpers
```

---

## Authentication — shared state

Never log in inside every test. Set up auth state once and reuse it.

```ts
// e2e/setup/auth.setup.ts
import { test as setup, expect } from "@playwright/test";
import path from "path";

const authFile = path.join(__dirname, "../.auth/user.json");

setup("authenticate as default user", async ({ page }) => {
  await page.goto("/login");
  await page.getByLabel("Email").fill(process.env.TEST_USER_EMAIL!);
  await page.getByLabel("Password").fill(process.env.TEST_USER_PASSWORD!);
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page).toHaveURL("/dashboard");

  // Save signed-in state so tests can reuse it
  await page.context().storageState({ path: authFile });
});
```

```ts
// playwright.config.ts — reference the stored state
{
  name: "chromium",
  use: {
    ...devices["Desktop Chrome"],
    storageState: "e2e/.auth/user.json",
  },
  dependencies: ["setup"],
}
```

```
# .gitignore
e2e/.auth/
```

---

## Page Object Model (POM)

Always encapsulate page interactions in POMs. Tests read as user stories; POMs handle selectors.

```ts
// e2e/pages/BasePage.ts
import type { Page, Locator } from "@playwright/test";

export abstract class BasePage {
  constructor(protected page: Page) {}

  async waitForPageReady() {
    await this.page.waitForLoadState("networkidle");
  }

  async getToast(): Promise<Locator> {
    return this.page.getByRole("status");
  }
}
```

```ts
// e2e/pages/LoginPage.ts
import { type Page, expect } from "@playwright/test";
import { BasePage } from "./BasePage";

export class LoginPage extends BasePage {
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly submitButton: Locator;
  readonly errorAlert: Locator;

  constructor(page: Page) {
    super(page);
    this.emailInput    = page.getByLabel("Email");
    this.passwordInput = page.getByLabel("Password");
    this.submitButton  = page.getByRole("button", { name: "Sign in" });
    this.errorAlert    = page.getByRole("alert");
  }

  async goto() {
    await this.page.goto("/login");
  }

  async login(email: string, password: string) {
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    await this.submitButton.click();
  }

  async expectError(message: string) {
    await expect(this.errorAlert).toContainText(message);
  }
}
```

```ts
// e2e/pages/index.ts
export { LoginPage } from "./LoginPage";
export { DashboardPage } from "./DashboardPage";
export { ProductsPage } from "./ProductsPage";
```

---

## Custom fixtures

Extend Playwright's `test` with app-specific helpers rather than repeating setup in every spec.

```ts
// e2e/fixtures/auth.fixture.ts
import { test as base } from "@playwright/test";
import { LoginPage, DashboardPage } from "@/e2e/pages";

type AuthFixtures = {
  loginPage: LoginPage;
  dashboardPage: DashboardPage;
  loginAs: (role: "admin" | "viewer" | "editor") => Promise<void>;
};

export const test = base.extend<AuthFixtures>({
  loginPage: async ({ page }, use) => {
    await use(new LoginPage(page));
  },

  dashboardPage: async ({ page }, use) => {
    await use(new DashboardPage(page));
  },

  loginAs: async ({ page }, use) => {
    await use(async (role) => {
      const credentials = {
        admin:  { email: process.env.TEST_ADMIN_EMAIL!,  password: process.env.TEST_ADMIN_PW! },
        viewer: { email: process.env.TEST_VIEWER_EMAIL!, password: process.env.TEST_VIEWER_PW! },
        editor: { email: process.env.TEST_EDITOR_EMAIL!, password: process.env.TEST_EDITOR_PW! },
      }[role];

      await page.goto("/login");
      await page.getByLabel("Email").fill(credentials.email);
      await page.getByLabel("Password").fill(credentials.password);
      await page.getByRole("button", { name: "Sign in" }).click();
      await page.waitForURL("/dashboard");
    });
  },
});

export { expect } from "@playwright/test";
```

```ts
// e2e/fixtures/index.ts — merge all fixture extensions
import { mergeTests } from "@playwright/test";
import { test as authTest } from "./auth.fixture";
import { test as dataTest } from "./data.fixture";

export const test = mergeTests(authTest, dataTest);
export { expect } from "@playwright/test";
```

---

## Writing tests

### Import from fixtures, not from `@playwright/test`

```ts
// ✅
import { test, expect } from "../fixtures";

// ❌ — loses custom fixtures
import { test, expect } from "@playwright/test";
```

### Structure: describe → arrange → act → assert

```ts
// e2e/auth/login.spec.ts
import { test, expect } from "../fixtures";

test.describe("Login", () => {
  test.beforeEach(async ({ loginPage }) => {
    await loginPage.goto();
  });

  test("successful login redirects to dashboard", async ({ loginPage, page }) => {
    await loginPage.login("user@example.com", "password123");

    await expect(page).toHaveURL("/dashboard");
    await expect(page.getByRole("heading", { name: "Dashboard" })).toBeVisible();
  });

  test("shows error for invalid credentials", async ({ loginPage }) => {
    await loginPage.login("user@example.com", "wrong-password");

    await loginPage.expectError("Invalid credentials");
  });

  test("shows validation errors for empty form submission", async ({ loginPage, page }) => {
    await page.getByRole("button", { name: "Sign in" }).click();

    await expect(page.getByRole("alert")).toContainText("Email is required");
  });

  test("locks account after 5 failed attempts", async ({ loginPage }) => {
    for (let i = 0; i < 5; i++) {
      await loginPage.login("user@example.com", "wrong");
    }

    await loginPage.expectError("Account locked");
  });
});
```

---

## Locator rules

Use in this priority order — same reasoning as Testing Library:

| Priority | Selector | When |
|---|---|---|
| 1 | `getByRole` | Always first choice |
| 2 | `getByLabel` | Form inputs |
| 3 | `getByPlaceholder` | Inputs without visible label |
| 4 | `getByText` | Static content |
| 5 | `getByAltText` | Images |
| 6 | `getByTitle` | Tooltip targets |
| 7 | `getByTestId` | Last resort |

```ts
// ✅ Role + name — resilient to text changes
page.getByRole("button", { name: "Submit order" })
page.getByRole("dialog", { name: "Confirm deletion" })
page.getByRole("row", { name: "Product A" })

// ✅ Label — ideal for form fields
page.getByLabel("Email address")

// ✅ Test ID — acceptable for dynamic content with no stable text
page.getByTestId("product-card-42")

// ❌ CSS selectors — fragile, couples tests to implementation
page.locator(".btn-primary")
page.locator("form > div:nth-child(2) > input")

// ❌ XPath — even more fragile
page.locator("//button[@class='submit']")
```

---

## Assertions

Always use **web-first assertions** — they auto-retry until the condition is met or timeout fires.

```ts
// ✅ Web-first — retries automatically
await expect(locator).toBeVisible();
await expect(locator).toBeHidden();
await expect(locator).toBeEnabled();
await expect(locator).toBeDisabled();
await expect(locator).toHaveText("Hello");
await expect(locator).toContainText("Hello");
await expect(locator).toHaveValue("some value");
await expect(locator).toHaveCount(3);
await expect(locator).toHaveAttribute("aria-expanded", "true");
await expect(page).toHaveURL("/dashboard");
await expect(page).toHaveTitle("Dashboard");

// ❌ Non-retrying — race conditions waiting to happen
const text = await locator.textContent();
expect(text).toBe("Hello");
```

### Custom timeout for slow operations

```ts
await expect(page.getByTestId("report-table")).toBeVisible({ timeout: 30_000 });
```

---

## Waiting — never use fixed waits

```ts
// ❌ Never
await page.waitForTimeout(2000);

// ✅ Wait for network
await page.waitForResponse((r) => r.url().includes("/api/products") && r.status() === 200);

// ✅ Wait for navigation
await page.waitForURL("/dashboard");

// ✅ Wait for element state
await page.getByRole("progressbar").waitFor({ state: "hidden" });

// ✅ Wait for load state
await page.waitForLoadState("networkidle");
```

---

## API interception & mocking

Use route interception to test edge cases that are hard to reproduce with real data.

```ts
test("shows empty state when no products exist", async ({ page }) => {
  // Intercept before navigation
  await page.route("**/api/products*", (route) =>
    route.fulfill({ status: 200, json: { items: [], total: 0 } })
  );

  await page.goto("/products");

  await expect(page.getByText("No products found")).toBeVisible();
});

test("shows error banner when API fails", async ({ page }) => {
  await page.route("**/api/products*", (route) =>
    route.fulfill({ status: 503, json: { message: "Service unavailable" } })
  );

  await page.goto("/products");

  await expect(page.getByRole("alert")).toContainText("Something went wrong");
});

test("retries failed request", async ({ page }) => {
  let callCount = 0;
  await page.route("**/api/products*", (route) => {
    callCount++;
    if (callCount === 1) return route.fulfill({ status: 500 });
    return route.continue();
  });

  await page.goto("/products");
  await expect(page.getByRole("list")).toBeVisible();
  expect(callCount).toBe(2);
});
```

---

## Visual regression

```ts
// Opt-in per test — not global
test("product card matches snapshot", async ({ page }) => {
  await page.goto("/products/42");
  await page.getByTestId("product-card").waitFor();

  await expect(page.getByTestId("product-card")).toMatchAriaSnapshot();
});

// Full page — only for stable, data-static pages
test("marketing homepage", async ({ page }) => {
  await page.goto("/");
  await page.waitForLoadState("networkidle");
  await expect(page).toHaveScreenshot("homepage.png", {
    maxDiffPixelRatio: 0.02,
  });
});
```

---

## Test data

```ts
// e2e/utils/db.ts — seed/teardown helpers via API or direct DB
export async function seedProduct(overrides: Partial<ProductPayload> = {}) {
  const response = await fetch(`${process.env.TEST_API_URL}/test/products`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${process.env.TEST_API_KEY}` },
    body: JSON.stringify({ name: "Test Product", price: 9.99, ...overrides }),
  });
  return response.json() as Promise<Product>;
}

export async function cleanupProducts(ids: string[]) {
  await Promise.all(
    ids.map((id) =>
      fetch(`${process.env.TEST_API_URL}/test/products/${id}`, {
        method: "DELETE",
        headers: { Authorization: `Bearer ${process.env.TEST_API_KEY}` },
      })
    )
  );
}
```

```ts
// Usage in a spec
test.describe("Product management", () => {
  let productId: string;

  test.beforeEach(async () => {
    const product = await seedProduct({ name: "E2E Product" });
    productId = product.id;
  });

  test.afterEach(async () => {
    await cleanupProducts([productId]);
  });

  test("editor can archive a product", async ({ page, loginAs }) => {
    await loginAs("editor");
    await page.goto(`/products/${productId}`);
    await page.getByRole("button", { name: "Archive" }).click();
    await page.getByRole("button", { name: "Confirm" }).click();
    await expect(page.getByRole("status")).toContainText("Product archived");
  });
});
```

---

## Environment variables

```env
# .env.test
PLAYWRIGHT_BASE_URL=http://localhost:5173
TEST_API_URL=http://localhost:3000
TEST_API_KEY=test-secret

TEST_USER_EMAIL=user@test.local
TEST_USER_PASSWORD=Test1234!

TEST_ADMIN_EMAIL=admin@test.local
TEST_ADMIN_PW=Admin1234!

TEST_VIEWER_EMAIL=viewer@test.local
TEST_VIEWER_PW=Viewer1234!
```

```
# .gitignore
.env.test.local
e2e/.auth/
playwright-report/
test-results/
```

---

## CI integration

```yaml
# .github/workflows/e2e.yml
name: E2E

on:
  push:
    branches: [main]
  pull_request:

jobs:
  e2e:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: 22 }

      - run: npm ci
      - run: npx playwright install --with-deps chromium

      - name: Run E2E
        run: npx playwright test --project=chromium
        env:
          PLAYWRIGHT_BASE_URL: http://localhost:5173
          TEST_USER_EMAIL: ${{ secrets.TEST_USER_EMAIL }}
          TEST_USER_PASSWORD: ${{ secrets.TEST_USER_PASSWORD }}

      - uses: actions/upload-artifact@v4
        if: failure()
        with:
          name: playwright-report
          path: playwright-report/
          retention-days: 7
```

---

## Hard rules summary

| Rule | Reason |
|---|---|
| No `page.waitForTimeout()` | Race conditions — always wait for state |
| No CSS selectors in locators | Couples tests to implementation |
| No XPath | Brittle and unreadable |
| Import `test` from fixtures, not Playwright directly | Ensures custom fixtures are available |
| POMs for every page | Selectors in one place; tests read as stories |
| Auth via stored state, not per-test login | Speed + reliability |
| Web-first assertions only | Built-in auto-retry, no manual polling |
| `forbidOnly: true` on CI | Prevents `test.only` being accidentally committed |
| Secrets via env vars, never hardcoded | Security |
| `retries: 2` on CI | Absorbs genuine flakiness; investigate if >0 retries are frequent |
