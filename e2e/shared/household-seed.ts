import { request } from '@playwright/test';

import { extractAccessToken } from './auth-state';
import { authStatePath } from './auth-paths';

const API_ORIGIN = 'http://localhost:5050';

/**
 * Makes sure the worker's Authentik user belongs to a household.
 *
 * `HouseholdRequired` in the SPA redirects any signed-in user without a
 * household to `/household` ("Give your home a name"), so a bare worker user
 * would never reach a module page. Runs once per worker from the setup
 * project, right after the storage state is written. Idempotent: an existing
 * household is left untouched, so re-runs and the fixture's login fallback
 * never hit "You already belong to a household."
 */
export async function ensureHousehold(workerIndex: number): Promise<void> {
  const token = extractAccessToken(authStatePath(workerIndex));
  if (!token) {
    throw new Error(
      `[worker ${workerIndex}] No access token in ${authStatePath(workerIndex)} — cannot seed a household.`,
    );
  }

  const api = await request.newContext({
    baseURL: API_ORIGIN,
    extraHTTPHeaders: { Authorization: `Bearer ${token}` },
  });

  try {
    const mine = await api.get('/api/households/me');
    if (mine.ok()) return;
    if (mine.status() !== 404) {
      throw new Error(
        `[worker ${workerIndex}] GET /api/households/me failed: HTTP ${mine.status()}`,
      );
    }

    // The Person row is created lazily from the OIDC claims; CreateHousehold
    // requires it to exist first.
    const sync = await api.post('/api/persons/me/sync');
    if (!sync.ok()) {
      throw new Error(
        `[worker ${workerIndex}] POST /api/persons/me/sync failed: HTTP ${sync.status()}`,
      );
    }

    const created = await api.post('/api/households', {
      data: { name: `E2E worker ${workerIndex} home` },
    });
    if (created.status() !== 201) {
      throw new Error(
        `[worker ${workerIndex}] POST /api/households failed: HTTP ${created.status()} ${await created.text()}`,
      );
    }
  } finally {
    await api.dispose();
  }
}
