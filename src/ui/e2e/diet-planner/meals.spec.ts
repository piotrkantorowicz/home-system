import { test, expect } from './fixtures/auth.fixture';
import { DietPlansPage } from './pages/diet-plans.page';
import { ImportPage } from './pages/import.page';
import { generateWeeklyPlan } from './utils/data-generator';

test.describe.configure({ mode: 'serial', timeout: 180000 });

/**
 * E2E tests for meal management CRUD (add / edit / delete) on the diet plan detail calendar.
 *
 * Strategy:
 *  1. Import a plan with known recipes (Morning Bowl, Chicken Rice).
 *  2. Add a dinner entry on Monday using Morning Bowl (Monday already has breakfast — we test dinner slot).
 *  3. Edit that dinner to change the servings.
 *  4. Delete that dinner and assert it is gone.
 *  5. Clean up by deleting the plan.
 */
test.describe('Meal management CRUD', () => {
  const planName = `Meal CRUD Plan ${Date.now()}`;
  const planData = generateWeeklyPlan(planName, new Date());

  // The recipe we'll use for manual add/edit (index 0: Morning Bowl)
  const recipeName = planData.recipeNames[0];

  // We add a dinner on Monday (abbr "Mon"). Monday already has breakfast via import,
  // so we pick the "dinner" slot to avoid conflict.
  const targetDay = 'Mon';
  const mealTypeLabel = 'Dinner';

  test('setup: import a diet plan', async ({ page }) => {
    const importPage = new ImportPage(page);
    await importPage.goto();
    await importPage.runImportWizard(planData);
    await page.waitForURL('/diet-plans');
    await expect(page.getByText(planName)).toBeVisible();
  });

  test('can add a meal to a day', async ({ page }) => {
    const dietPlansPage = new DietPlansPage(page);
    await dietPlansPage.goto();
    await expect(page.getByText(planName)).toBeVisible({ timeout: 10000 });
    await dietPlansPage.openPlan(planName);

    // Add a dinner on Monday
    await dietPlansPage.clickAddMeal(targetDay, mealTypeLabel);
    await dietPlansPage.fillMealForm(recipeName, 2);
    await dietPlansPage.submitMealForm();

    // Verify the new meal appears in the Monday column
    await dietPlansPage.expectMealInDay(targetDay, recipeName);
  });

  test('can edit a meal', async ({ page }) => {
    const dietPlansPage = new DietPlansPage(page);
    await dietPlansPage.goto();
    await expect(page.getByText(planName)).toBeVisible({ timeout: 10000 });
    await dietPlansPage.openPlan(planName);

    // Edit the dinner we just added — change servings to 3
    await dietPlansPage.openEditMeal(recipeName);
    await dietPlansPage.fillMealForm(recipeName, 3, 'edited note');
    await dietPlansPage.submitMealForm();

    // Meal should still be visible (same recipe name)
    await dietPlansPage.expectMealInDay(targetDay, recipeName);
    // Servings count should reflect the update
    const dayCol = dietPlansPage.getDayColumn(targetDay).first();
    await expect(dayCol.getByText(/3 servings/i)).toBeVisible({ timeout: 5000 });
  });

  test('can delete a meal', async ({ page }) => {
    const dietPlansPage = new DietPlansPage(page);
    await dietPlansPage.goto();
    await expect(page.getByText(planName)).toBeVisible({ timeout: 10000 });
    await dietPlansPage.openPlan(planName);

    // Monday has both a breakfast (Morning Bowl, from import) and the dinner we added.
    // We need to identify the dinner card specifically.
    // Since both are the same recipe name, we delete by hovering over the meal
    // that shows "3 servings" (the one we edited).
    const dayCol = dietPlansPage.getDayColumn(targetDay).first();
    const dinnerCard = dayCol
      .locator('div.group')
      .filter({ hasText: /3 servings/i })
      .first();
    await dinnerCard.hover();
    const deleteBtn = dinnerCard.locator('button').filter({
      has: page.locator('svg.lucide-trash-2'),
    });
    await deleteBtn.click();

    await expect(page.getByText(/delete meal/i)).toBeVisible({ timeout: 5000 });
    await page.getByRole('button', { name: /^delete$/i }).click();
    await expect(page.getByText(/delete meal/i)).not.toBeVisible({ timeout: 5000 });

    // The dinner slot should now show "No meal" (the breakfast entry is separate)
    await expect(dayCol.getByText(/3 servings/i)).not.toBeVisible({ timeout: 5000 });
  });

  test('cleanup: delete the diet plan', async ({ page }) => {
    const dietPlansPage = new DietPlansPage(page);
    await dietPlansPage.goto();
    await expect(page.getByText(planName)).toBeVisible({ timeout: 10000 });
    await dietPlansPage.deletePlan(planName);
    await expect(page.getByText(planName)).not.toBeVisible();
  });
});
