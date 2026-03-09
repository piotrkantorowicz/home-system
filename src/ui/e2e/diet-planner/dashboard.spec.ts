import { test, expect } from './fixtures/auth.fixture';
import { DashboardPage } from './pages/dashboard.page';

test.describe('Dashboard', () => {
  test('displays statistics cards', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.goto();

    // Cards should be visible
    await expect(dashboard.productCard).toBeVisible();
    await expect(dashboard.recipeCard).toBeVisible();
    await expect(dashboard.dietPlanCard).toBeVisible();
  });

  test('shows counts from API', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.goto();

    // Wait for loading to complete
    await expect(dashboard.productCount).toBeVisible();
    await expect(dashboard.recipeCount).toBeVisible();
    await expect(dashboard.dietPlanCount).toBeVisible();

    // Counts should be numbers (not loading spinners)
    const productCount = await dashboard.getProductCount();
    const recipeCount = await dashboard.getRecipeCount();
    const dietPlanCount = await dashboard.getDietPlanCount();

    expect(typeof productCount).toBe('number');
    expect(typeof recipeCount).toBe('number');
    expect(typeof dietPlanCount).toBe('number');
  });

  test('cards link to correct pages', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.goto();

    await dashboard.productCard.click();
    await expect(page).toHaveURL('/products');

    await page.goBack();
    await dashboard.recipeCard.click();
    await expect(page).toHaveURL('/recipes');

    await page.goBack();
    await dashboard.dietPlanCard.click();
    await expect(page).toHaveURL('/diet-plans');
  });
});
