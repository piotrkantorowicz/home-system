import { test as base } from '@playwright/test';

import { trackCreatedId } from '../utils/test-tracker';

import type { Page } from '@playwright/test';

/**
 * Subscribes to API responses on the given page and records IDs of newly
 * created products and recipes for cleanup by the global teardown.
 *
 * Tracks `POST /api/v1/products` and `POST /api/v1/recipes` 2xx responses
 * (excluding the import / validation sub-routes — those are bulk operations
 * whose component entities are tracked separately by the import flow).
 *
 * DELETE responses are intentionally NOT tracked: the entity was either
 * created in this run (already tracked) or pre-existed and should be
 * left to whatever cleanup process owns it.
 */
function attachEntityTracker(page: Page): void {
  page.on('response', async (response) => {
    const url = response.url();
    const method = response.request().method();
    const status = response.status();

    if (status < 200 || status >= 300) return;
    if (method !== 'POST') return;

    try {
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
    } catch {
      // Ignore JSON parse errors for non-JSON responses
    }
  });
}

interface DataFixtures {
  /**
   * Auto-attached fixture: subscribes the entity tracker to every test's page
   * before the test body runs. Tests don't need to request this — it activates
   * automatically via { auto: true }.
   */
  entityTracker: void;
}

export const test = base.extend<DataFixtures>({
  entityTracker: [
    async ({ page }, use) => {
      attachEntityTracker(page);
      await use();
    },
    { auto: true },
  ],
});

export { expect } from '@playwright/test';
