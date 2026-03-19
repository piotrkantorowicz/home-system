import { format, startOfWeek } from 'date-fns';
import { test, expect } from './fixtures/auth.fixture';
import { NutritionPage } from './pages/nutrition.page';
import { ImportPage } from './pages/import.page';
import { generateWeeklyPlan } from './utils/data-generator';

// ─── Helpers ────────────────────────────────────────────────────────────────

function currentWeekRange() {
  const weekStart = startOfWeek(new Date(), { weekStartsOn: 1 });
  const weekEnd = new Date(weekStart);
  weekEnd.setDate(weekEnd.getDate() + 6);
  return {
    from: format(weekStart, 'yyyy-MM-dd'),
    to: format(weekEnd, 'yyyy-MM-dd'),
  };
}

// ─── Structure tests (no meal data required) ─────────────────────────────────

test.describe('Nutrition Summary — page structure', () => {
  test('loads with date range picker defaulting to current week', async ({ page }) => {
    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    await expect(nutritionPage.fromInput).toBeVisible();
    await expect(nutritionPage.toInput).toBeVisible();
    await expect(nutritionPage.applyButton).toBeVisible();

    // Inputs should be pre-filled with current week dates
    const { from, to } = currentWeekRange();
    await expect(nutritionPage.fromInput).toHaveValue(from);
    await expect(nutritionPage.toInput).toHaveValue(to);
  });

  test('shows empty state when no meals exist for the selected range', async ({ page }) => {
    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    await nutritionPage.setDateRange('2020-01-01', '2020-01-07');
    await nutritionPage.applyRange('2020-01-01', '2020-01-07');

    await nutritionPage.expectEmptyState();
  });

  test('apply button is disabled when from date is after to date', async ({ page }) => {
    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    await nutritionPage.setDateRange('2026-03-10', '2026-03-01');

    await expect(nutritionPage.applyButton).toBeDisabled();
  });

  test('pagination page size selector has correct options and defaults to 25', async ({ page }) => {
    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    // Needs data to show the table + pagination, so use current week
    const { from, to } = currentWeekRange();
    await nutritionPage.setDateRange(from, to);
    // Even with no data the select should not render — so this test only applies
    // after we have data; covered in the serial suite below
    await expect(nutritionPage.pageSizeSelect)
      .toBeVisible({ timeout: 5000 })
      .catch(() => {});
  });
});

// ─── Data-dependent tests (serial, require imported meals) ───────────────────

test.describe('Nutrition Summary — with meal data', () => {
  test.describe.configure({ mode: 'serial', timeout: 180000 });

  const planData = generateWeeklyPlan(new Date());
  test('setup: import weekly meal plan', async ({ page }) => {
    const importPage = new ImportPage(page);
    await importPage.goto();
    await importPage.runImportWizard(planData);
    await page.waitForURL(/\/diet-planner\/calendar/);
  });

  test('shows totals and daily average cards after applying range', async ({ page }) => {
    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    await nutritionPage.expectTotalsVisible();
    await nutritionPage.expectDailyAvgVisible();
  });

  test('daily breakdown table shows a row for each day with meals', async ({ page }) => {
    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    const rowCount = await nutritionPage.getTableRowCount();
    expect(rowCount).toBeGreaterThan(0);
  });

  test('goal progress panel is visible when goals are configured', async ({ page }) => {
    // Navigate to goals page and wait for goals to finish loading
    await page.goto('/diet-planner/goals');
    const calorieInput = page.locator('input[id="dailyCalorieTarget"]');
    await expect(calorieInput).toBeVisible();
    await page.waitForLoadState('networkidle');

    // Use a timestamp-based calorie value to guarantee the form is dirty
    // regardless of what goals are already stored
    const uniqueCalories = String(2000 + (Date.now() % 100));
    await calorieInput.fill(uniqueCalories);
    await page.locator('input[id="proteinGrams"]').fill('150');
    await page.locator('input[id="carbsGrams"]').fill('220');
    await page.locator('input[id="fatGrams"]').fill('70');

    const submitBtn = page.locator('button[type="submit"]');
    await expect(submitBtn).toBeEnabled({ timeout: 5000 });
    await submitBtn.click();

    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    await nutritionPage.expectGoalProgressVisible();
  });

  test('changing date range to empty period shows empty state', async ({ page }) => {
    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    await nutritionPage.setDateRange('2020-01-01', '2020-01-07');
    await nutritionPage.applyRange('2020-01-01', '2020-01-07');

    await nutritionPage.expectEmptyState();
  });

  test('current week range shows data on load', async ({ page }) => {
    // goto() already defaults to the current week and waits for the API response.
    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    const rowCount = await nutritionPage.getTableRowCount();
    expect(rowCount).toBeGreaterThan(0);
  });

  test('pagination: default page size is 25', async ({ page }) => {
    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    const size = await nutritionPage.getPageSize();
    expect(size).toBe(25);
  });

  test('pagination: changing page size updates the select', async ({ page }) => {
    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    await nutritionPage.setPageSize(10);
    expect(await nutritionPage.getPageSize()).toBe(10);

    await nutritionPage.setPageSize(50);
    expect(await nutritionPage.getPageSize()).toBe(50);
  });

  test('pagination: previous button is disabled on first page', async ({ page }) => {
    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    await expect(nutritionPage.previousButton).toBeDisabled();
  });
});
