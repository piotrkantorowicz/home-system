import * as fs from 'fs';

import { test as base, expect } from '@playwright/test';

import { authStatePath, credentialsFor } from '../../shared/auth-paths';
import { APP_ORIGIN, loginViaAuthentik } from '../../shared/authentik-login';

import type { Page } from '@playwright/test';

// ── OIDC token refresh ────────────────────────────────────────────────────────

const OIDC_AUTHORITY = 'http://localhost:9000/application/o/home-system/';
const CLIENT_ID = 'I476Ik4ahckZS00sx9zmad8ennJdhDr7Fb1LpoMH';
const TOKEN_ENDPOINT = 'http://localhost:9000/application/o/token/';
const STORAGE_KEY = `oidc.user:${OIDC_AUTHORITY}:${CLIENT_ID}`;

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
 * Updates the worker's auth file on success so subsequent test contexts on
 * the same worker benefit from the fresh tokens. Returns the refreshed
 * OidcUser on success, or null if the refresh token is missing or Authentik
 * rejects it (e.g. after a server restart).
 */
async function tryRefreshTokens(authFile: string): Promise<OidcUser | null> {
  if (!fs.existsSync(authFile)) return null;

  let state: StorageState;
  try {
    state = JSON.parse(fs.readFileSync(authFile, 'utf-8')) as StorageState;
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

  found.entry.value = JSON.stringify(updatedUser);
  fs.writeFileSync(authFile, JSON.stringify(state, null, 2));

  return updatedUser;
}

// ── Login fallback ────────────────────────────────────────────────────────────

async function performLogin(
  page: Page,
  authFile: string,
  workerIndex: number,
): Promise<void> {
  const { username, password } = credentialsFor(workerIndex);

  await loginViaAuthentik(page, username, password);

  await page.context().storageState({ path: authFile });
}

// ── Fixture ───────────────────────────────────────────────────────────────────

export const test = base.extend({
  // Each worker owns a distinct auth-state file so Playwright loads the right
  // identity into the browser context. Overridden from the default fixture so
  // playwright.config.ts doesn't need to know about per-worker paths.
  storageState: [
    async ({}, use, testInfo) => {
      await use(authStatePath(testInfo.parallelIndex));
    },
    { scope: 'test' },
  ],

  page: async ({ page }, use, testInfo) => {
    const authFile = authStatePath(testInfo.parallelIndex);

    // Before each test, try to refresh the OIDC access token so the test
    // doesn't fail due to token expiry during a long suite run.
    // If the refresh token is still valid (normal case), this takes ~200 ms.
    // If Authentik was restarted and all tokens are revoked, fall back to a
    // full interactive login and update the worker's auth file.
    const refreshedUser = await tryRefreshTokens(authFile);

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
      console.warn(
        `[auth][worker ${testInfo.parallelIndex}] Token refresh failed — falling back to full login`,
      );
      // A logout test can revoke the stored refresh token while its access
      // token still looks valid locally. Start the fallback with a clean login.
      await page.context().clearCookies();
      await page.addInitScript(
        ({ origin, key }) => {
          if (window.location.origin === origin && !sessionStorage.getItem('e2e-auth-reset')) {
            localStorage.removeItem(key);
            sessionStorage.setItem('e2e-auth-reset', 'done');
          }
        },
        { origin: APP_ORIGIN, key: STORAGE_KEY },
      );
      await performLogin(page, authFile, testInfo.parallelIndex);
    }

    await use(page);
  },
});

export { expect };
