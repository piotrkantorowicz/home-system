import { test, expect } from './fixtures/auth.fixture';
import { DietPlansPage } from './pages/diet-plans.page';
import { ImportPage } from './pages/import.page';
import { generateWeeklyPlan } from './utils/data-generator';

test.describe.configure({ mode: 'serial', timeout: 120000 });

test.describe('Calendar', () => {
  const planData = generateWeeklyPlan(new Date());

  test('can import meals and see them on calendar', async ({ page }) => {
    const importPage = new ImportPage(page);
    await importPage.goto();
    await importPage.runImportWizard(planData);

    // Should redirect to calendar
    await page.waitForURL(/\/diet-planner\/calendar/);
  });

  test('shows current week with 7 days', async ({ page }) => {
    const calendarPage = new DietPlansPage(page);
    await calendarPage.goto();

    // All 7 day columns should be rendered
    await calendarPage.expectAllDaysRendered();

    // Week header is visible
    await expect(page.getByText(/week of/i)).toBeVisible();
  });

  test('imported meals are visible on calendar', async ({ page }) => {
    const calendarPage = new DietPlansPage(page);
    await calendarPage.goto();

    // Data generator: Mon (i=0) gets Morning Bowl (breakfast)
    // Tue (i=1) gets Chicken Rice (lunch)
    await calendarPage.expectMealInDay('Mon', planData.targetRecipe);
    await calendarPage.expectMealInDay('Tue', planData.recipeNames[1]);
  });
});
