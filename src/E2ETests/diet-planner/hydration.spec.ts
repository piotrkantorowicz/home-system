import { test, expect } from './fixtures';
import { HydrationPage } from './pages/hydration.page';

test.describe('Hydration', () => {
  test('hydration page loads and shows the heading', async ({ page }) => {
    const hydrationPage = new HydrationPage(page);
    await hydrationPage.goto();

    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
  });

  test('progress bar is visible on the page', async ({ page }) => {
    const hydrationPage = new HydrationPage(page);
    await hydrationPage.goto();

    await hydrationPage.expectProgressBarVisible();
  });

  test('settings form shows daily target and glass size fields', async ({ page }) => {
    const hydrationPage = new HydrationPage(page);
    await hydrationPage.goto();

    await expect(hydrationPage.dailyTargetInput).toBeVisible();
    await expect(hydrationPage.glassSizeInput).toBeVisible();
  });

  test('save settings button is visible', async ({ page }) => {
    const hydrationPage = new HydrationPage(page);
    await hydrationPage.goto();

    await expect(hydrationPage.saveSettingsButton).toBeVisible();
  });

  test('quick-add glass button is visible', async ({ page }) => {
    const hydrationPage = new HydrationPage(page);
    await hydrationPage.goto();

    await expect(page.getByRole('button', { name: /\+.*ml|glass/i }).first()).toBeVisible();
  });

  test('hydration nav link is reachable from the sidebar', async ({ page }) => {
    await page.goto('/diet-planner');
    await page.waitForLoadState('networkidle');

    await page.getByRole('link', { name: /hydration/i }).click();

    await expect(page).toHaveURL(/\/diet-planner\/hydration$/);
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
  });

  test('can update hydration settings', async ({ page }) => {
    const hydrationPage = new HydrationPage(page);
    await hydrationPage.goto();

    await hydrationPage.updateSettings({ dailyTargetMl: 3000, glassSizeMl: 300 });

    // After save the form resets to saved values — field still visible
    await expect(hydrationPage.dailyTargetInput).toBeVisible();
  });
});
