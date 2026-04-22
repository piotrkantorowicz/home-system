import { test, expect } from './fixtures';
import { DashboardPage } from './pages';

test.describe('Dashboard', () => {
  test('all three stat cards are visible on load', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.goto();

    await expect(dashboard.productCard).toBeVisible();
    await expect(dashboard.recipeCard).toBeVisible();
    await expect(dashboard.calendarCard).toBeVisible();
  });

  test('stat cards show numeric counts after data loads', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.goto();

    await dashboard.expectStatsLoaded();

    // Verify each stat card contains a numeric value (web-first assertions)
    await expect(dashboard.productCount).toHaveText(/\d+/);
    await expect(dashboard.recipeCount).toHaveText(/\d+/);
    await expect(dashboard.calendarCount).toHaveText(/\d+/);
  });

  test('each stat card links to its respective page', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.goto();

    await dashboard.productCard.click();
    await expect(page).toHaveURL('/diet-planner/products');

    await page.goBack();
    await dashboard.recipeCard.click();
    await expect(page).toHaveURL('/diet-planner/recipes');

    await page.goBack();
    await dashboard.calendarCard.click();
    await expect(page).toHaveURL('/diet-planner/calendar');
  });
});
