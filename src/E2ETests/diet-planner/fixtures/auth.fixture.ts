import * as fs from 'fs';

import { test as base, expect } from '@playwright/test';

import { trackCreatedId } from '../utils/test-tracker';

// ── OIDC token refresh ────────────────────────────────────────────────────────

const AUTH_FILE = 'playwright/.auth/user.json';
const OIDC_AUTHORITY = 'http://localhost:9000/application/o/home-system/';
const CLIENT_ID = 'I476Ik4ahckZS00sx9zmad8ennJdhDr7Fb1LpoMH';
const TOKEN_ENDPOINT = 'http://localhost:9000/application/o/token/';
const STORAGE_KEY = `oidc.user:${OIDC_AUTHORITY}:${CLIENT_ID}`;
const APP_ORIGIN = 'http://localhost:5173';

interface OidcUser {
  access_token: string;
  refresh_token?: string;
  expires_at: number;
  id_token?: string;
  token_type: string;
  scope: string;
  profile: Record<string, unknown>;
}

interface TokenResponse {
  access_token: string;
  refresh_token?: string;
  expires_in: number;
  id_token?: string;
  token_type: string;
}

interface StorageState {
  cookies: unknown[];
  origins: Array<{
    origin: string;
    localStorage: Array<{ name: string; value: string }>;
  }>;
}

function readOidcEntry(
  state: StorageState,
): { entry: { name: string; value: string }; user: OidcUser } | null {
  const origin = state.origins?.find((o) => o.origin === APP_ORIGIN);
  const entry = origin?.localStorage?.find((e) => e.name === STORAGE_KEY);
  if (!entry) return null;

  try {
    const user = JSON.parse(entry.value) as OidcUser;
    return { entry, user };
  } catch {
    return null;
  }
}

/**
 * Attempts to refresh the stored OIDC tokens using the refresh token grant.
 * Updates user.json on success so subsequent test contexts also benefit.
 * Returns the refreshed OidcUser on success, null if the refresh token is
 * missing or Authentik rejects it (e.g. after a server restart).
 */
async function tryRefreshTokens(): Promise<OidcUser | null> {
  if (!fs.existsSync(AUTH_FILE)) return null;

  let state: StorageState;
  try {
    state = JSON.parse(fs.readFileSync(AUTH_FILE, 'utf-8')) as StorageState;
  } catch {
    return null;
  }

  const found = readOidcEntry(state);
  if (!found?.user.refresh_token) return null;

  let response: Response;
  try {
    response = await fetch(TOKEN_ENDPOINT, {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: new URLSearchParams({
        grant_type: 'refresh_token',
        refresh_token: found.user.refresh_token,
        client_id: CLIENT_ID,
      }),
    });
  } catch {
    return null;
  }

  if (!response.ok) return null;

  const tokens = (await response.json()) as TokenResponse;

  const updatedUser: OidcUser = {
    ...found.user,
    access_token: tokens.access_token,
    refresh_token: tokens.refresh_token ?? found.user.refresh_token,
    expires_at: Math.floor(Date.now() / 1000) + tokens.expires_in,
    ...(tokens.id_token ? { id_token: tokens.id_token } : {}),
  };

  // Persist fresh tokens so future test contexts start with a valid state
  found.entry.value = JSON.stringify(updatedUser);
  fs.writeFileSync(AUTH_FILE, JSON.stringify(state, null, 2));

  return updatedUser;
}

// ── Login fallback ────────────────────────────────────────────────────────────

const USERNAME = process.env['TEST_USER_EMAIL'] ?? 'E2eTestsUser';
const PASSWORD = process.env['TEST_USER_PASSWORD'] ?? 'Password321!';

async function performLogin(page: import('@playwright/test').Page): Promise<void> {
  await page.goto('/');

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

  await page.waitForURL(`${APP_ORIGIN}/**`, { timeout: 30000 });
  await page.waitForLoadState('networkidle');
  await expect(page.getByText('Dashboard', { exact: false }).first()).toBeVisible({
    timeout: 10000,
  });

  await page.context().storageState({ path: AUTH_FILE });
}

// ── Entity-tracking helper ────────────────────────────────────────────────────

function extractIdFromUrl(url: string, entity: string): string | null {
  const pattern = new RegExp(`/api/v1/${entity}/([a-f0-9-]{36})`);
  const match = url.match(pattern);
  return match ? match[1] : null;
}

// ── Fixture ───────────────────────────────────────────────────────────────────

export const test = base.extend({
  page: async ({ page }, use) => {
    // Before each test, try to refresh the OIDC access token so the test
    // doesn't fail due to token expiry during a long suite run.
    // If the refresh token is still valid (normal case), this takes ~200 ms.
    // If Authentik was restarted and all tokens are revoked, fall back to a
    // full interactive login and update user.json for subsequent tests.
    const refreshedUser = await tryRefreshTokens();

    if (refreshedUser) {
      // Inject fresh tokens before any navigation so oidc-client-ts picks them
      // up immediately when the app loads.
      await page.context().addInitScript(
        ({ key, value }: { key: string; value: string }) => {
          localStorage.setItem(key, value);
        },
        { key: STORAGE_KEY, value: JSON.stringify(refreshedUser) },
      );
    } else {
      // Refresh token missing or rejected — re-authenticate from scratch.
      console.warn('[auth] Token refresh failed — falling back to full login');
      await performLogin(page);
    }

    // Intercept API responses to track created and deleted entity IDs
    page.on('response', async (response) => {
      const url = response.url();
      const method = response.request().method();
      const status = response.status();

      if (status < 200 || status >= 300) return;

      try {
        if (method === 'POST') {
          if (
            url.includes('/api/v1/products') &&
            !url.includes('/import') &&
            !url.includes('/validate')
          ) {
            const body = await response.json();
            if (body?.id) trackCreatedId('products', body.id);
          } else if (
            url.includes('/api/v1/recipes') &&
            !url.includes('/import') &&
            !url.includes('/validate')
          ) {
            const body = await response.json();
            if (body?.id) trackCreatedId('recipes', body.id);
          }
        }

        // Note: DELETE is intentionally not tracked — the item was either
        // created in this session (already tracked) or pre-existed and
        // should not be targeted by cleanup.
      } catch {
        // Ignore JSON parse errors for non-JSON responses
      }
    });

    await use(page);
  },
});

export { expect };
