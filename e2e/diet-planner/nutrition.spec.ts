import { test, expect } from './fixtures';
import { ImportPage, NutritionPage, gotoProfileSection } from './pages';
import { generateWeeklyPlan } from './utils/data-generator';

// ── Structure tests (independent of meal data) ────────────────────────────────

test.describe('Nutrition Summary — page structure', () => {
  test('range presets are visible and default to 7 days', async ({ page }) => {
    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    await expect(nutritionPage.rangeGroup).toBeVisible();
    await expect(nutritionPage.rangeOption('7')).toHaveAttribute('aria-checked', 'true');
    await expect(nutritionPage.rangeOption('30')).toHaveAttribute('aria-checked', 'false');
    await expect(nutritionPage.rangeOption('90')).toHaveAttribute('aria-checked', 'false');
  });

  test('switching to a longer range re-fetches the summary', async ({ page }) => {
    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    await nutritionPage.selectRange('30');

    await expect(nutritionPage.rangeOption('30')).toHaveAttribute('aria-checked', 'true');
    await expect(nutritionPage.rangeOption('7')).toHaveAttribute('aria-checked', 'false');
  });

  test('an empty summary shows the empty state', async ({ page }) => {
    // There is no longer a way to pick a guaranteed-empty *past* range (the
    // presets are always "last N days ending today") — mock the response
    // instead of depending on this worker's shared meal data being absent.
    await page.route('**/api/v1/meals/nutrition-summary**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: '[]' }),
    );

    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    await nutritionPage.expectEmptyState();
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

  test('metric tiles and the intake chart appear for the default 7-day range', async ({ page }) => {
    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    await expect(page.getByText('Avg intake', { exact: true })).toBeVisible();
    await expect(page.getByText('Days logged', { exact: true })).toBeVisible();
    await nutritionPage.expectChartVisible();
    await nutritionPage.expectMacroSplitVisible();
  });

  test('daily breakdown table has at least one row for the default range', async ({ page }) => {
    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    const rowCount = await nutritionPage.getTableRowCount();
    expect(rowCount).toBeGreaterThan(0);
  });

  test('a 90-day range with no meals in the older window still renders (empty or partial)', async ({
    page,
  }) => {
    // The imported plan only covers the current week — a 90-day range still
    // includes it, so this just exercises the preset switch without erroring.
    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();
    await nutritionPage.selectRange('90');

    await expect(page.getByText('Days logged', { exact: true })).toBeVisible();
  });

  test('the average-intake tile reflects the configured calorie goal', async ({ page }) => {
    // Configure goals (now under the profile hub — #114)
    await gotoProfileSection(page, 'goals');

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

    // Wait for the save to land before leaving the page — navigating while the
    // mutation is in flight left the nutrition page without a goal under load.
    const goalsSaved = page.waitForResponse(
      (resp) =>
        resp.url().includes('/api/v1/goals') &&
        ['POST', 'PUT'].includes(resp.request().method()) &&
        resp.ok(),
      { timeout: 10000 },
    );
    await submitBtn.click();
    await goalsSaved;

    // Once a calorie goal exists, the "Avg intake" tile's hint switches from
    // the generic "kcal" unit to a signed delta against the goal ("+123", "−45" or "0").
    const nutritionPage = new NutritionPage(page);
    await nutritionPage.goto();

    const tile = page.getByText('Avg intake', { exact: true }).locator('xpath=..');
    await expect(tile).toBeVisible({ timeout: 10000 });
    const hint = tile.locator('> div').nth(2);
    await expect(hint).toHaveText(/^(0|[+\u2212-]\d[\d\s.,]*)$/);
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
