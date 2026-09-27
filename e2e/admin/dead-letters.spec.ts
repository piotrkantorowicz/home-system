import { tryRefreshTokens } from '../diet-planner/fixtures/auth.fixture';
import { test, expect } from '../diet-planner/fixtures';
import { adminAuthStatePath } from '../shared/auth-paths';

// Smoke coverage only: seeding real dead letters would mean breaking a consumer
// on the shared stack. Retry and table rendering are covered by Vitest and the
// API integration tests.
test.describe('Admin dead letters', () => {
  test('an admin sees the counters and both sections', async ({ browser }) => {
    await tryRefreshTokens(adminAuthStatePath());
    const context = await browser.newContext({ storageState: adminAuthStatePath() });
    const page = await context.newPage();

    try {
      await page.goto('/admin');

      await expect(page.getByRole('heading', { level: 1, name: 'Dead letters' })).toBeVisible();
      await expect(page.getByText('Dead deliveries')).toBeVisible();
      await expect(page.getByText('Dead events')).toBeVisible();
      await expect(page.getByRole('heading', { name: 'Notification deliveries' })).toBeVisible();
      await expect(page.getByRole('heading', { name: 'Integration events' })).toBeVisible();
    } finally {
      await context.close();
    }
  });

  test('a non-admin gets the admins-only state', async ({ page }) => {
    await page.goto('/admin');

    await expect(page.getByText('Admins only')).toBeVisible();
    await expect(page.getByRole('heading', { level: 1, name: 'Dead letters' })).toHaveCount(0);
  });
});
