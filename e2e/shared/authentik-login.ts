import { expect } from '@playwright/test';

import type { Page } from '@playwright/test';

export const APP_ORIGIN = 'http://localhost:5173';

/**
 * Drives the interactive Authentik login from the SPA root and waits until the
 * app is back with an authenticated shell.
 *
 * Shared by the setup project (one login per worker) and the per-test fixture
 * (fallback when the stored refresh token was revoked). Readiness is a locator
 * at every stage — never a network-quiet state, which the OIDC redirect chain
 * and the SignalR connection keep from ever settling.
 *
 * The provider uses the implicit-consent authorization flow (see
 * `infrastructure/authentik/blueprints/home-system.yaml`): after the password
 * stage Authentik either redirects straight back to the SPA or, when the
 * user's stored consent has lapsed, shows its consent stage first.
 */
export async function loginViaAuthentik(page: Page, username: string, password: string) {
  await page.goto('/');

  // Stage 1 — username
  // Authentik renders inside shadow DOM, so getByLabel may not work.
  // Use getByPlaceholder as the primary locator with input[name] as fallback.
  // This first wait also covers the SPA's cold start on a fresh Vite dev
  // server (dependency pre-bundling before the OIDC redirect can fire), so it
  // gets the navigation budget rather than the per-stage one.
  const usernameInput = page
    .getByPlaceholder(/email or username/i)
    .or(page.locator('input[name="uidField"]'));
  await usernameInput.waitFor({ state: 'visible', timeout: 30000 });
  await usernameInput.fill(username);
  await page
    .getByRole('button', { name: /log in|continue|sign in|submit/i })
    .or(page.locator('button[type="submit"]'))
    .first()
    .click();

  // Stage 2 — password. The stage is ready when its input is on screen.
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

  // Stage 3 — either the consent stage ("You're about to sign into …") or
  // straight back to the SPA. Key the race on the consent copy, not on its
  // "Continue" button: the password stage's submit button carries the same
  // label and is still on screen for a moment after the click above.
  const userMenu = page.getByRole('button', { name: /user menu/i });
  const consentPrompt = page.getByText(/about to sign into/i);
  await expect(consentPrompt.or(userMenu).first()).toBeVisible({ timeout: 30000 });
  if (await consentPrompt.isVisible()) {
    await page.getByRole('button', { name: /^continue$/i }).click();
  }

  // Stage 4 — back in the SPA with an authenticated shell.
  await page.waitForURL(`${APP_ORIGIN}/**`, { timeout: 30000 });
  await expect(userMenu).toBeVisible({ timeout: 10000 });
}
