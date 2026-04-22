import { test as setup, expect } from '@playwright/test';

const authFile = 'playwright/.auth/user.json';

const USERNAME = process.env['TEST_USER_EMAIL'] ?? 'E2eTestsUser';
const PASSWORD = process.env['TEST_USER_PASSWORD'] ?? 'Password321!';

setup('authenticate', async ({ page }) => {
  await page.goto('/');

  // Stage 1 — username
  // Authentik renders inside shadow DOM, so getByLabel may not work.
  // Use getByPlaceholder as the primary locator with input[name] as fallback.
  const usernameInput = page
    .getByPlaceholder(/email or username/i)
    .or(page.locator('input[name="uidField"]'));
  await usernameInput.waitFor({ state: 'visible', timeout: 10000 });
  await usernameInput.fill(USERNAME);
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
  await passwordInput.fill(PASSWORD);
  await page
    .getByRole('button', { name: /log in|continue|sign in|submit/i })
    .or(page.locator('button[type="submit"]'))
    .first()
    .click();

  // Stage 3 — handle optional post-login screens (app selection / consent)
  await page.waitForLoadState('networkidle', { timeout: 10000 });

  if (page.url().includes(':9000')) {
    const appLink = page.getByText('Diet Planner', { exact: false }).first();
    const consentButton = page.getByRole('button', { name: /accept|continue|authorize|allow/i }).first();
    const continueButton = page.getByRole('button', { name: /continue/i }).first();

    const appLinkVisible = await appLink.isVisible({ timeout: 2000 }).catch(() => false);
    if (appLinkVisible) {
      await appLink.click();
    } else {
      const consentVisible = await consentButton.isVisible({ timeout: 2000 }).catch(() => false);
      if (consentVisible) {
        await consentButton.click();
      } else {
        const continueVisible = await continueButton.isVisible({ timeout: 2000 }).catch(() => false);
        if (continueVisible) {
          await continueButton.click();
        }
      }
    }
  }

  // Wait for redirect to the SPA
  await page.waitForURL('http://localhost:5173/**', { timeout: 30000 });
  await page.waitForLoadState('networkidle');

  await expect(page.getByText('Dashboard', { exact: false }).first()).toBeVisible({ timeout: 10000 });

  await page.context().storageState({ path: authFile });
});
