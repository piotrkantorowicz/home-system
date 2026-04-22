import { test, expect } from './fixtures';
import { CalendarPage, ImportPage } from './pages';
import { generateWeeklyPlan } from './utils/data-generator';

test.describe.configure({ mode: 'serial', timeout: 180000 });

test.describe('Calendar CRUD & Navigation', () => {
  const planData = generateWeeklyPlan(new Date());
  const recipeName = planData.recipeNames[1]; // Chicken Rice

  // ── Setup ────────────────────────────────────────────────────────────────────

  test('setup: import a weekly meal plan', async ({ page }) => {
    const importPage = new ImportPage(page);
    await importPage.goto();
    await importPage.runImportWizard(planData);
    await expect(page).toHaveURL(/\/diet-planner\/calendar/);
  });

  // ── Navigation ───────────────────────────────────────────────────────────────

  test('calendar shows current week with 7 day columns and a week header', async ({ page }) => {
    const calendar = new CalendarPage(page);
    await calendar.goto();

    // Week header is visible
    await expect(page.getByText(/week of/i)).toBeVisible();

    // All 7 weekdays are rendered
    const weekdays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
    for (const day of weekdays) {
      await expect(
        page.getByRole('heading', { name: new RegExp(`^${day}\\b`, 'i') }),
      ).toBeVisible();
    }
  });

  test('previous/next buttons navigate between weeks', async ({ page }) => {
    const calendar = new CalendarPage(page);
    await calendar.goto();

    // Capture the current week header text
    const weekHeader = page.getByRole('heading', { name: /week of/i });
    const currentWeekText = await weekHeader.textContent();

    // Navigate to next week
    await page.getByRole('button', { name: /next/i }).or(
      page.locator('button').filter({ has: page.locator('svg.lucide-chevron-right') }),
    ).last().click();
    await page.waitForLoadState('networkidle');

    // Week header should change
    await expect(weekHeader).not.toHaveText(currentWeekText ?? '', { timeout: 5000 });
    const nextWeekText = await weekHeader.textContent();

    // Navigate back with previous button
    await page.getByRole('button', { name: /previous/i }).or(
      page.locator('button').filter({ has: page.locator('svg.lucide-chevron-left') }),
    ).last().click();
    await page.waitForLoadState('networkidle');

    // Should be back to original week
    await expect(weekHeader).toHaveText(currentWeekText ?? '', { timeout: 5000 });

    // Navigate forward again then use Today button to snap back
    await page.getByRole('button', { name: /next/i }).or(
      page.locator('button').filter({ has: page.locator('svg.lucide-chevron-right') }),
    ).last().click();
    await page.waitForLoadState('networkidle');
    await expect(weekHeader).toHaveText(nextWeekText ?? '');

    await page.getByRole('button', { name: /today/i }).click();
    await page.waitForLoadState('networkidle');
    await expect(weekHeader).toHaveText(currentWeekText ?? '', { timeout: 5000 });
  });

  // ── Add Meal ─────────────────────────────────────────────────────────────────

  test('user can add a meal to a day slot', async ({ page }) => {
    const calendar = new CalendarPage(page);
    await calendar.goto();

    await calendar.clickAddMeal('Mon', 'Snack');
    await calendar.fillMealForm(recipeName, 2.5);
    await calendar.submitMealForm();

    await calendar.expectMealInDay('Mon', recipeName);
  });

  // ── Edit Meal ────────────────────────────────────────────────────────────────

  test('user can edit an existing meal to change servings', async ({ page }) => {
    const calendar = new CalendarPage(page);
    await calendar.goto();

    await calendar.openEditMeal(recipeName);
    await calendar.fillMealForm(recipeName, 3.5, 'updated note');
    await calendar.submitMealForm();

    await calendar.expectMealInDay('Mon', recipeName);
  });

  // ── Delete Meal ──────────────────────────────────────────────────────────────

  test('user can delete a meal and it disappears from the calendar', async ({ page }) => {
    const calendar = new CalendarPage(page);
    await calendar.goto();

    // Count meals with this recipe name before deletion
    const mealLinksBefore = page.getByRole('link', { name: recipeName });
    const countBefore = await mealLinksBefore.count();
    expect(countBefore).toBeGreaterThan(0);

    await calendar.deleteMeal(recipeName);

    // One fewer meal link after deletion
    await expect(page.getByRole('link', { name: recipeName })).toHaveCount(countBefore - 1, {
      timeout: 5000,
    });
  });
});
