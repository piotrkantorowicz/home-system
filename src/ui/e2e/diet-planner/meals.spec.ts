import { test, expect } from './fixtures/auth.fixture';
import { DietPlansPage } from './pages/diet-plans.page';
import { ImportPage } from './pages/import.page';
import { generateWeeklyPlan } from './utils/data-generator';

test.describe.configure({ mode: 'serial', timeout: 180000 });

/**
 * E2E tests for meal management CRUD (add / edit / delete) on the diet plan detail calendar.
 *
 * Strategy:
 *  1. Import a plan. The import puts Morning Bowl on even days (breakfast)
 *     and Chicken Rice on odd days (lunch). Monday gets Morning Bowl (breakfast only).
 *  2. Add Chicken Rice to Monday's Snack slot (Chicken Rice not present on Monday from import).
 *     This makes the meal uniquely identifiable within Monday's column.
 *  3. Edit that snack entry (change servings, add note).
 *  4. Delete that snack entry and assert it is gone from Monday.
 *  5. Clean up by deleting the plan.
 */
test.describe('Meal management CRUD', () => {
  const planName = `Meal CRUD Plan ${Date.now()}`;
  const planData = generateWeeklyPlan(planName, new Date());

  // Use Chicken Rice (recipeNames[1]) — not present on Monday from the import data.
  const recipeName = planData.recipeNames[1];

  const targetDay = 'Mon';
  const mealTypeLabel = 'Snack';

  test('setup: import a diet plan', async ({ page }) => {
    const importPage = new ImportPage(page);
    await importPage.goto();
    await importPage.runImportWizard(planData);
    await expect(page.getByText(planName)).toBeVisible();
  });

  test('can add a meal to a day', async ({ page }) => {
    const dietPlansPage = new DietPlansPage(page);
    await dietPlansPage.goto();
    await expect(page.getByText(planName)).toBeVisible({ timeout: 10000 });
    await dietPlansPage.openPlan(planName);

    // Add Chicken Rice to Monday's Snack slot (empty from import)
    await dietPlansPage.clickAddMeal(targetDay, mealTypeLabel);
    await dietPlansPage.fillMealForm(recipeName, 2.5);
    await dietPlansPage.submitMealForm();

    // Chicken Rice should now appear in Monday's column
    await dietPlansPage.expectMealInDay(targetDay, recipeName);
  });

  test('can edit a meal', async ({ page }) => {
    const dietPlansPage = new DietPlansPage(page);
    await dietPlansPage.goto();
    await expect(page.getByText(planName)).toBeVisible({ timeout: 10000 });
    await dietPlansPage.openPlan(planName);

    // Monday's column contains Morning Bowl (breakfast) and Chicken Rice (snack).
    // openEditMeal scoped to Monday's column finds Chicken Rice uniquely.
    const dayCol = dietPlansPage.getDayColumn(targetDay).first();
    const mealCard = dayCol.locator('div.group').filter({ hasText: recipeName }).first();
    await mealCard.hover();
    const editBtn = mealCard
      .locator('button')
      .filter({ has: page.locator('svg.lucide-pencil') });
    await editBtn.click();
    await expect(dietPlansPage.mealFormDialog).toBeVisible({ timeout: 5000 });

    await dietPlansPage.fillMealForm(recipeName, 3.5, 'edited');
    await dietPlansPage.submitMealForm();

    // Meal should still be visible in Monday
    await dietPlansPage.expectMealInDay(targetDay, recipeName);
  });

  test('can delete a meal', async ({ page }) => {
    const dietPlansPage = new DietPlansPage(page);
    await dietPlansPage.goto();
    await expect(page.getByText(planName)).toBeVisible({ timeout: 10000 });
    await dietPlansPage.openPlan(planName);

    // Find the Chicken Rice card in Monday's column (unique there) and delete it
    const dayCol = dietPlansPage.getDayColumn(targetDay).first();
    const mealCard = dayCol.locator('div.group').filter({ hasText: recipeName }).first();
    await mealCard.hover();
    const deleteBtn = mealCard.locator('button').filter({
      has: page.locator('svg.lucide-trash-2'),
    });
    await deleteBtn.click();

    // Confirm via destructive button in the dialog (translation-independent)
    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible({ timeout: 5000 });
    await dialog.locator('button.bg-destructive, button[class*="destructive"]').click();
    await expect(dialog).not.toBeVisible({ timeout: 5000 });

    // Chicken Rice should no longer appear in Monday's column
    await expect(dayCol.getByText(recipeName)).not.toBeVisible({ timeout: 5000 });
  });

  test('cleanup: delete the diet plan', async ({ page }) => {
    const dietPlansPage = new DietPlansPage(page);
    await dietPlansPage.goto();
    await expect(page.getByText(planName)).toBeVisible({ timeout: 10000 });
    await dietPlansPage.deletePlan(planName);
    // Check the plan card heading specifically (avoids matching dialog description text)
    await expect(page.locator('h3').filter({ hasText: planName })).not.toBeVisible();
  });
});
