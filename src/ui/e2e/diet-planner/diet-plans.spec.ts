import { test, expect } from './fixtures/auth.fixture';
import { DietPlansPage } from './pages/diet-plans.page';
import { ImportPage } from './pages/import.page';
import { generateWeeklyPlan } from './utils/data-generator';

test.describe.configure({ mode: 'serial', timeout: 120000 });

test.describe('Diet Plans CRUD', () => {
  const planName = `CRUD Plan ${Date.now()}`;
  const planData = generateWeeklyPlan(planName, new Date());

  test('can import (create) a diet plan', async ({ page }) => {
    const importPage = new ImportPage(page);
    await importPage.goto();
    await importPage.runImportWizard(planData);

    await page.waitForURL('/diet-planner/diet-plans');
    await expect(page.getByText(planName)).toBeVisible();
  });

  test('can view diet plan details with all 7 days', async ({ page }) => {
    const dietPlansPage = new DietPlansPage(page);
    await dietPlansPage.goto();

    await expect(page.getByText(planName)).toBeVisible({ timeout: 10000 });
    await dietPlansPage.openPlan(planName);

    // Verify week header is visible
    await expect(page.getByText(/week of/i)).toBeVisible();

    // All 7 day columns should be rendered
    await dietPlansPage.expectAllDaysRendered();

    // Verify meals exist across the week (not just Monday)
    // Data generator: Mon/Wed/Fri/Sun = breakfast (Morning Bowl), Tue/Thu/Sat = lunch (Chicken Rice)
    // i % 2 === 0 → Mon(0), Wed(2), Fri(4), Sun(6) get breakfast
    // i % 2 === 1 → Tue(1), Thu(3), Sat(5) get lunch
    await dietPlansPage.expectMealInDay('Mon', planData.targetRecipe);
    await dietPlansPage.expectMealInDay('Sat', planData.recipeNames[1]);
  });

  test('can delete a diet plan', async ({ page }) => {
    const dietPlansPage = new DietPlansPage(page);
    await dietPlansPage.goto();

    await expect(page.getByText(planName)).toBeVisible({ timeout: 10000 });
    await dietPlansPage.deletePlan(planName);
    await expect(page.getByText(planName)).not.toBeVisible();
  });
});
