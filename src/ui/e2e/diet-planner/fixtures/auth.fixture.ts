import { test as base, expect } from '@playwright/test';
import { trackCreatedId } from '../utils/test-tracker';

/**
 * Extract entity ID from API URL patterns like /api/v1/products/{id}
 */
function extractIdFromUrl(url: string, entity: string): string | null {
  const pattern = new RegExp(`/api/v1/${entity}/([a-f0-9-]{36})`);
  const match = url.match(pattern);
  return match ? match[1] : null;
}

// Extend base test to auto-track created/deleted entities
export const test = base.extend({
  page: async ({ page }, use) => {
    // Intercept API responses to track created and deleted entity IDs
    page.on('response', async (response) => {
      const url = response.url();
      const method = response.request().method();
      const status = response.status();

      // Only track successful mutations
      if (status < 200 || status >= 300) return;

      try {
        // Track POST (create) responses
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
          } else if (url.includes('/api/v1/diet-plans/import')) {
            const body = await response.json();
            // ImportResultDto contains dietPlanId
            if (body?.dietPlanId) trackCreatedId('plans', body.dietPlanId);
            if (body?.id) trackCreatedId('plans', body.id);
          } else if (
            url.includes('/api/v1/diet-plans') &&
            !url.includes('/import') &&
            !url.includes('/validate')
          ) {
            const body = await response.json();
            if (body?.id) trackCreatedId('plans', body.id);
          }
        }

        // Track DELETE responses — the entity is now soft-deleted and invisible to queries,
        // but we still need its ID for permanent deletion in cleanup
        if (method === 'DELETE') {
          const planId = extractIdFromUrl(url, 'diet-plans');
          if (planId) {
            trackCreatedId('plans', planId);
            return;
          }

          const recipeId = extractIdFromUrl(url, 'recipes');
          if (recipeId) {
            trackCreatedId('recipes', recipeId);
            return;
          }

          const productId = extractIdFromUrl(url, 'products');
          if (productId) {
            trackCreatedId('products', productId);
            return;
          }
        }
      } catch {
        // Ignore JSON parse errors for non-JSON responses
      }
    });

    await use(page);
  },
});

export { expect };
