import { test, expect } from './fixtures/auth.fixture';
import { DashboardPage } from './pages/dashboard.page';

test.describe('Dashboard', () => {
  test('displays statistics cards', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.goto();

    // Cards should be visible
    await expect(dashboard.productCard).toBeVisible();
    await expect(dashboard.recipeCard).toBeVisible();
    await expect(dashboard.calendarCard).toBeVisible();
  });

  test('shows counts from API', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.goto();

    // Wait for loading to complete
    await expect(dashboard.productCount).toBeVisible();
    await expect(dashboard.recipeCount).toBeVisible();
    await expect(dashboard.calendarCount).toBeVisible();

    // Counts should be numbers (not loading spinners)
    const productCount = await dashboard.getProductCount();
    const recipeCount = await dashboard.getRecipeCount();
    const calendarCount = await dashboard.getCalendarCount();

    expect(typeof productCount).toBe('number');
    expect(typeof recipeCount).toBe('number');
    expect(typeof calendarCount).toBe('number');
  });

  test('cards link to correct pages', async ({ page }) => {
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
