import { test as setup, expect } from '@playwright/test';

import { authStatePath, credentialsFor } from './auth-paths';

import type { Page } from '@playwright/test';

const WORKER_COUNT = 4;

async function loginOnce(page: Page, username: string, password: string) {
  await page.goto('/');

  // Stage 1 — username
  // Authentik renders inside shadow DOM, so getByLabel may not work.
  // Use getByPlaceholder as the primary locator with input[name] as fallback.
  const usernameInput = page
    .getByPlaceholder(/email or username/i)
    .or(page.locator('input[name="uidField"]'));
  await usernameInput.waitFor({ state: 'visible', timeout: 10000 });
  await usernameInput.fill(username);
  await page
    .getByRole('button', { name: /log in|continue|sign in|submit/i })
    .or(page.locator('button[type="submit"]'))
    .first()
    .click();

  // Stage 2 — password
  await page.waitForLoadState('networkidle');
  const passwordInput = page
    .getByPlaceholder(/password/i)
    .or(page.locator('input[type="password"]'))
    .first();
  await passwordInput.waitFor({ state: 'visible', timeout: 10000 });
  await passwordInput.click();
  await passwordInput.fill(password);
  await page
    .getByRole('button', { name: /log in|continue|sign in|submit/i })
    .or(page.locator('button[type="submit"]'))
    .first()
    .click();

  // Stage 3 — handle optional post-login screens (app selection / consent)
  await page.waitForLoadState('networkidle', { timeout: 10000 });

  if (page.url().includes(':9000')) {
    const appLink = page.getByText('Diet Planner', { exact: false }).first();
    const consentButton = page
      .getByRole('button', { name: /accept|continue|authorize|allow/i })
      .first();
    const continueButton = page.getByRole('button', { name: /continue/i }).first();

    const appLinkVisible = await appLink.isVisible({ timeout: 2000 }).catch(() => false);
    if (appLinkVisible) {
      await appLink.click();
    } else {
      const consentVisible = await consentButton.isVisible({ timeout: 2000 }).catch(() => false);
      if (consentVisible) {
        await consentButton.click();
      } else {
        const continueVisible = await continueButton
          .isVisible({ timeout: 2000 })
          .catch(() => false);
        if (continueVisible) {
          await continueButton.click();
        }
      }
    }
  }

  // Wait for redirect to the SPA
  await page.waitForURL('http://localhost:5173/**', { timeout: 30000 });
  await page.waitForLoadState('networkidle');

  await expect(page.getByText('Dashboard', { exact: false }).first()).toBeVisible({
    timeout: 10000,
  });
}

for (let workerIndex = 0; workerIndex < WORKER_COUNT; workerIndex++) {
  setup(`authenticate worker ${workerIndex}`, async ({ browser }) => {
    const { username, password } = credentialsFor(workerIndex);

    // Isolated browser context so each login starts from a clean slate and we
    // can save storage state without interference from sibling setup tests.
    const context = await browser.newContext();
    const page = await context.newPage();

    await loginOnce(page, username, password);

    await context.storageState({ path: authStatePath(workerIndex) });
    await context.close();
  });
}
