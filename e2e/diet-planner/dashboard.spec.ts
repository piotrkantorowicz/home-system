import { test, expect } from './fixtures';
import { DashboardPage } from './pages';

test.describe('Dashboard', () => {
  test('shows the today hero and quick actions on load', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.goto();

    await expect(dashboard.heading).toBeVisible();
    await expect(dashboard.logWaterLink).toBeVisible();
    await expect(dashboard.logMealButton).toBeVisible();
  });

  test('water card and week review card are visible', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.goto();

    await expect(page.getByText('Water', { exact: true })).toBeVisible();
    await expect(page.getByText('This week', { exact: true })).toBeVisible();
  });

  test('the "Full plan" link on the Next up card opens the calendar', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.goto();

    await expect(page.getByText('Next up', { exact: true })).toBeVisible();
    await dashboard.fullPlanLink.click();
    await expect(page).toHaveURL('/diet-planner/calendar');
  });

  test('"Log water" opens the hydration page', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.goto();

    await dashboard.logWaterLink.click();
    await expect(page).toHaveURL('/diet-planner/hydration');
  });

  test('"Log a meal" opens the meal form sheet', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.goto();

    await dashboard.logMealButton.click();
    await expect(page.getByRole('dialog')).toBeVisible();
  });
});
