import { format, startOfWeek } from 'date-fns';

import { test, expect } from './fixtures';
import { ImportPage } from './pages/import.page';
import { NutritionPage } from './pages/nutrition.page';
import { generateWeeklyPlan } from './utils/data-generator';

function currentWeekRange() {
  const weekStart = startOfWeek(new Date(), { weekStartsOn: 1 });
  const weekEnd = new Date(weekStart);
  weekEnd.setDate(weekEnd.getDate() + 6);
  return {
    from: format(weekStart, 'yyyy-MM-dd'),
    to: format(weekEnd, 'yyyy-MM-dd'),
  };
}

// ── Structure tests (independent of meal data) ────────────────────────────────

test.describe('Nutrition Summary — page structure', () => {
  test('date range inputs are visible and default to the current week', async ({ page }) => {
    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    await expect(nutritionPage.fromInput).toBeVisible();
    await expect(nutritionPage.toInput).toBeVisible();
    await expect(nutritionPage.applyButton).toBeVisible();

    const { from, to } = currentWeekRange();
    await expect(nutritionPage.fromInput).toHaveValue(from);
    await expect(nutritionPage.toInput).toHaveValue(to);
  });

  test('selecting a past range with no meals shows the empty state', async ({ page }) => {
    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    await nutritionPage.setDateRange('2020-01-01', '2020-01-07');
    await nutritionPage.applyRange('2020-01-01', '2020-01-07');

    await nutritionPage.expectEmptyState();
  });

  test('Apply button is disabled when the from date is later than the to date', async ({ page }) => {
    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    await nutritionPage.setDateRange('2026-12-31', '2026-01-01');

    await expect(nutritionPage.applyButton).toBeDisabled();
  });
});

// ── Data-dependent tests (serial, require imported meals) ─────────────────────

test.describe('Nutrition Summary — with meal data', () => {
  test.describe.configure({ mode: 'serial', timeout: 180000 });

  const planData = generateWeeklyPlan(new Date());

  test('setup: import a weekly meal plan', async ({ page }) => {
    const importPage = new ImportPage(page);
    await importPage.goto();
    await importPage.runImportWizard(planData);
    await expect(page).toHaveURL(/\/diet-planner\/calendar/);
  });

  test('totals and daily average cards appear after applying the current week range', async ({
    page,
  }) => {
    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    await nutritionPage.expectTotalsVisible();
    await nutritionPage.expectDailyAvgVisible();
  });

  test('daily breakdown table has at least one row for the current week', async ({ page }) => {
    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    await expect(nutritionPage.tableRows.first()).toBeVisible({ timeout: 8000 });
  });

  test('switching to an empty date range shows the empty state', async ({ page }) => {
    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    await nutritionPage.setDateRange('2020-01-01', '2020-01-07');
    await nutritionPage.applyRange('2020-01-01', '2020-01-07');

    await nutritionPage.expectEmptyState();
  });

  test('goal progress panel appears when nutrition goals are configured', async ({ page }) => {
    // Configure goals
    await page.goto('/diet-planner/goals');
    await page.waitForLoadState('networkidle');

    // Alternate protein to guarantee the form is always dirty regardless of prior run state
    const currentProtein = await page.locator('#proteinGrams').inputValue();
    const newProtein = currentProtein === '150' ? '140' : '150';

    await page.locator('#dailyCalorieTarget').fill('2000');
    await page.locator('#proteinGrams').fill(newProtein);
    await page.locator('#carbsGrams').fill('220');
    await page.locator('#fatGrams').fill('70');

    // Fill fiber if the field exists (some form versions require it)
    const fiberField = page.locator('#fiberGrams');
    if (await fiberField.isVisible({ timeout: 1000 }).catch(() => false)) {
      await fiberField.fill('30');
    }

    const submitBtn = page.getByRole('button', { name: /save goals/i });
    await expect(submitBtn).toBeEnabled({ timeout: 8000 });
    await submitBtn.click();

    // Navigate to nutrition page and verify goals panel
    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    await nutritionPage.expectGoalProgressVisible();
  });

  test('pagination defaults to page size 25', async ({ page }) => {
    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    await expect(nutritionPage.pageSizeSelect).toHaveValue('25');
  });

  test('pagination: changing page size updates the selector value', async ({ page }) => {
    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    await nutritionPage.setPageSize(10);
    await expect(nutritionPage.pageSizeSelect).toHaveValue('10');

    await nutritionPage.setPageSize(50);
    await expect(nutritionPage.pageSizeSelect).toHaveValue('50');
  });

  test('previous page button is disabled when on the first page', async ({ page }) => {
    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    await expect(nutritionPage.previousButton).toBeDisabled();
  });
});
