# Playwright — E2E Rules

E2E tests cover **user journeys** through the real stack: Vite dev server, backend API,
PostgreSQL, Authentik. One journey per spec beats ten specs that prod one button each.

## Setup that exists (`e2e/`)

```
e2e/
  playwright.config.ts        chromium only, 4 workers, fullyParallel, dotenv → e2e/.env
  shared/
    auth.setup.ts             logs each worker's Authentik user in once, saves storage state
    auth-paths.ts             authStatePath(workerIndex), credentialsFor(workerIndex)
    global-teardown.ts
  <module>/
    fixtures/
      index.ts                `test` / `expect` — import from HERE, never from @playwright/test
      auth.fixture.ts         per-worker storageState + silent OIDC refresh when the token is stale
      test-data.ts
    pages/                    Page Object Models, one class per screen, `index.ts` barrel
      BasePage.ts
      products.page.ts
    utils/                    seed.ts, cleanup.ts, data-generator.ts (API-level helpers)
    products.spec.ts
docs/e2e/README.md            prerequisites, env vars, per-area notes
```

- **Chromium only.** Cross-browser is not a goal for a self-hosted home app; do not add
  projects without a reason.
- **One Authentik user per worker** (`E2eWorker0..3`). Tests in different workers never see
  each other's data; tests in the same worker must still use unique names/ids.
- `webServer` reuses a running Vite on `:5173`. Backend + Authentik must already be up —
  `/run-project` first, or see `docs/e2e/README.md`.
- `timeout: 60s`, `actionTimeout: 15s`, `trace: on-first-retry`, `retries: 2` on CI.

## Writing a spec

```ts
import { expect, test } from '../fixtures';
import { ProductsPage } from '../pages';

test.describe('Products', () => {
  test('owner creates a product and sees it in the list', async ({ page, testData }) => {
    const products = new ProductsPage(page);
    await products.goto();

    await products.create({ name: testData.unique('Oats'), calories: 380 });

    await expect(products.row(testData.last)).toBeVisible();
  });
});
```

- `test` / `expect` from `../fixtures` — that is where auth state and helpers are wired.
- Arrange through the API (`utils/seed.ts`) when the UI path is not what the test is about.
- Clean up what you create (`utils/cleanup.ts` in `afterEach`) — the databases are shared
  across runs.

## Page Object Models

```ts
export class ProductsPage extends BasePage {
  readonly createButton: Locator;
  readonly nameInput: Locator;

  constructor(page: Page) {
    super(page);
    this.createButton = page.getByRole('button', { name: /add product/i });
    this.nameInput = page.getByLabel(/name/i);
  }

  async goto() {
    await this.page.goto('/diet-planner/products');
    await this.createButton.waitFor();          // page is ready when its primary action is
  }

  row(name: string) {
    return this.page.getByRole('row', { name });
  }
}
```

- Selectors live in POMs only. Specs read as user stories.
- Readiness = a locator on the screen, not `waitForLoadState('networkidle')`. Playwright
  documents `networkidle` as discouraged; with SignalR + TanStack refetches the network never
  idles. Every `goto()` waits for the screen's primary locator (a form's submit button, the
  week grid, the page heading); `grep -r networkidle e2e` must stay empty.

## Locators — priority

| # | Locator | Use for |
|---|---|---|
| 1 | `getByRole(role, { name })` | everything interactive |
| 2 | `getByLabel` | form fields |
| 3 | `getByPlaceholder` | inputs without a visible label |
| 4 | `getByText` | static content |
| 5 | `getByTestId` | dynamic widgets with no accessible identity — coordinate the id with the component test |

No CSS or XPath selectors. Prefer regex names (`/save profile/i`) over exact strings so copy
edits do not break the suite — but keep them specific enough not to match two buttons.

## Assertions & waiting

- Web-first assertions only: `await expect(locator).toBeVisible()` / `toHaveText` /
  `toHaveCount` / `toHaveURL`. They retry; `expect(await locator.textContent())` does not.
- No `page.waitForTimeout()`. Wait for a response (`page.waitForResponse`), a URL, or a
  locator state.
- Radix/react-day-picker quirks (a selected day toggles off when clicked again, selects open
  in a portal) belong in the POM as documented helpers, not in every spec.

## Mocking

Route interception (`page.route`) is for edge cases the real backend cannot produce on demand
(503, empty pages, slow responses). Happy paths always hit the real API.

## Environment

`e2e/.env` (git-ignored, `.env.example` committed): `TEST_USER_PASSWORD` for the worker users,
optional `PLAYWRIGHT_BASE_URL`. No secrets in specs.

## Running

```bash
cd e2e
npm test                        # whole suite (~4 min)
npm test diet-planner/profile   # one spec
npm run test:ui                 # interactive
npm run test:debug              # step-through
```

`/run-e2e` wraps the prerequisites. E2E is **not** in CI yet (needs Authentik + backend in the
runner) — tracked in #269; run it locally before shipping UI flows.
