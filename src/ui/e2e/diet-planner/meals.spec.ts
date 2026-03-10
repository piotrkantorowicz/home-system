import { test, expect } from './fixtures/auth.fixture';
import { DietPlansPage } from './pages/diet-plans.page';
import { ImportPage } from './pages/import.page';
import { generateWeeklyPlan } from './utils/data-generator';

test.describe.configure({ mode: 'serial', timeout: 180000 });

/**
 * E2E tests for meal management CRUD (add / edit / delete) on the calendar.
 *
 * Strategy:
 *  1. Import meals. The import puts Morning Bowl on even days (breakfast)
 *     and Chicken Rice on odd days (lunch). Monday gets Morning Bowl (breakfast only).
 *  2. Add Chicken Rice to Monday's Snack slot (Chicken Rice not present on Monday from import).
 *     This makes the meal uniquely identifiable within Monday's column.
 *  3. Edit that snack entry (change servings, add note).
 *  4. Delete that snack entry and assert it is gone from Monday.
 */
test.describe('Meal management CRUD', () => {
  const planData = generateWeeklyPlan(new Date());
  const recipeName = planData.recipeNames[1]; // Chicken Rice
  const targetDay = 'Mon';
  const mealTypeLabel = 'Snack';

  test('setup: import meals', async ({ page }) => {
    const importPage = new ImportPage(page);
    await importPage.goto();
    await importPage.runImportWizard(planData);
    await page.waitForURL(/\/diet-planner\/calendar/);
  });

  test('can add a meal to a day', async ({ page }) => {
    const calendarPage = new DietPlansPage(page);
    await calendarPage.goto();

    await calendarPage.clickAddMeal(targetDay, mealTypeLabel);
    await calendarPage.fillMealForm(recipeName, 2.5);
    await calendarPage.submitMealForm();

    await calendarPage.expectMealInDay(targetDay, recipeName);
  });

  test('can edit a meal', async ({ page }) => {
    const calendarPage = new DietPlansPage(page);
    await calendarPage.goto();

    const dayCol = calendarPage.getDayColumn(targetDay).first();
    const mealCard = dayCol.locator('div.group').filter({ hasText: recipeName }).first();
    await mealCard.hover();
    const editBtn = mealCard.locator('button').filter({ has: page.locator('svg.lucide-pencil') });
    await editBtn.click();
    await expect(calendarPage.mealFormDialog).toBeVisible({ timeout: 5000 });

    await calendarPage.fillMealForm(recipeName, 3.5, 'edited');
    await calendarPage.submitMealForm();

    await calendarPage.expectMealInDay(targetDay, recipeName);
  });

  test('can delete a meal', async ({ page }) => {
    const calendarPage = new DietPlansPage(page);
    await calendarPage.goto();

    const dayCol = calendarPage.getDayColumn(targetDay).first();
    const mealCard = dayCol.locator('div.group').filter({ hasText: recipeName }).first();
    await mealCard.hover();
    const deleteBtn = mealCard
      .locator('button')
      .filter({ has: page.locator('svg.lucide-trash-2') });
    await deleteBtn.click();

    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible({ timeout: 5000 });
    await dialog.locator('button.bg-destructive, button[class*="destructive"]').click();
    await expect(dialog).not.toBeVisible({ timeout: 5000 });

    await expect(dayCol.getByText(recipeName)).not.toBeVisible({ timeout: 5000 });
  });
});
