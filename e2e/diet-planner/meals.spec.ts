import { format, startOfWeek } from 'date-fns';

import { test, expect } from './fixtures';
import { CalendarPage, ImportPage } from './pages';
import { generateWeeklyPlan } from './utils/data-generator';
import { clearMealsInSlot, createApiContext, seedMealSchedule } from './utils/seed';

test.describe.configure({ mode: 'serial', timeout: 180000 });

test.describe('Calendar CRUD & Navigation', () => {
  const planData = generateWeeklyPlan(new Date());
  const recipeName = planData.recipeNames[1]; // Chicken Rice

  // ── Setup ────────────────────────────────────────────────────────────────────

  test('setup: import a weekly meal plan', async ({ page }) => {
    // Slot names are worker-shared state; make sure the Breakfast/Lunch/Snack/
    // Dinner slots the tests below click on exist before the import maps
    // meals onto them (unmatched types would otherwise land in "Other").
    const importPage = new ImportPage(page);
    await importPage.goto();
    await seedMealSchedule(page);
    // The add-meal test below needs an empty "Mon / Snack" cell — the week grid
    // only offers "Add meal" on empty cells, and an earlier spec on this worker
    // may have imported a full week into it.
    await clearMealsInSlot(
      page,
      format(startOfWeek(new Date(), { weekStartsOn: 1 }), 'yyyy-MM-dd'),
      'Snack',
    );
    await importPage.runImportWizard(planData);
    await expect(page).toHaveURL(/\/diet-planner\/calendar/);
  });

  // ── Navigation ───────────────────────────────────────────────────────────────

  test('calendar shows current week with 7 day columns and a week header', async ({ page }) => {
    const calendar = new CalendarPage(page);
    await calendar.goto();

    // Week header is visible
    await expect(page.getByText(/week of/i)).toBeVisible();

    // All 7 weekday column headers are rendered
    const weekdays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
    for (const day of weekdays) {
      await expect(page.getByRole('columnheader', { name: day })).toBeVisible();
    }
  });

  test('previous/next buttons navigate between weeks', async ({ page }) => {
    const calendar = new CalendarPage(page);
    await calendar.goto();

    // Capture the current week header text
    const weekHeader = page.getByRole('heading', { name: /week of/i });
    const currentWeekText = await weekHeader.textContent();

    // Navigate to next week
    await page
      .getByRole('button', { name: /next/i })
      .or(page.locator('button').filter({ has: page.locator('svg.lucide-chevron-right') }))
      .last()
      .click();

    // Week header should change
    await expect(weekHeader).not.toHaveText(currentWeekText ?? '', {
      timeout: 5000,
    });
    const nextWeekText = await weekHeader.textContent();

    // Navigate back with previous button
    await page
      .getByRole('button', { name: /previous/i })
      .or(page.locator('button').filter({ has: page.locator('svg.lucide-chevron-left') }))
      .last()
      .click();

    // Should be back to original week
    await expect(weekHeader).toHaveText(currentWeekText ?? '', {
      timeout: 5000,
    });

    // Navigate forward again then use Today button to snap back
    await page
      .getByRole('button', { name: /next/i })
      .or(page.locator('button').filter({ has: page.locator('svg.lucide-chevron-right') }))
      .last()
      .click();
    await expect(weekHeader).toHaveText(nextWeekText ?? '');

    // "Today" snaps to the day view for the current date (#241); switching
    // back to the week view must land on the week we started from.
    await page.getByRole('button', { name: /today/i }).click();
    const todayHeading = new Date().toLocaleDateString('en', {
      weekday: 'long',
      month: 'long',
      day: 'numeric',
    });
    await expect(page.getByRole('heading', { name: todayHeading, exact: true })).toBeVisible();

    await page.getByRole('radio', { name: /^week$/i }).click();
    await expect(weekHeader).toHaveText(currentWeekText ?? '', {
      timeout: 5000,
    });
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

    // Count meal chips with this recipe name before deletion
    const chipsBefore = page.getByRole('button', { name: recipeName });
    const countBefore = await chipsBefore.count();
    expect(countBefore).toBeGreaterThan(0);

    await calendar.deleteMeal(recipeName);

    // One fewer meal chip after deletion
    await expect(page.getByRole('button', { name: recipeName })).toHaveCount(countBefore - 1, {
      timeout: 5000,
    });
  });

  test('day navigation and meal status changes persist with consumed nutrition', async ({
    page,
  }) => {
    const monday = format(startOfWeek(new Date(), { weekStartsOn: 1 }), 'yyyy-MM-dd');
    const calendar = new CalendarPage(page);
    await calendar.goto();
    await clearMealsInSlot(page, monday, 'Snack');
    await calendar.clickAddMeal('Mon', 'Lunch');
    await calendar.fillMealForm(recipeName);
    await calendar.submitMealForm();

    await page.goto(`/diet-planner/calendar?view=week&date=${monday}`);
    await expect(calendar.weekGrid).toBeVisible();
    await calendar.dayViewRadio.click();
    await expect(page).toHaveURL(new RegExp(`view=day&date=${monday}`));
    await page.goBack();
    await expect(page).toHaveURL(new RegExp(`view=week&date=${monday}`));
    await expect(calendar.weekGrid).toBeVisible();

    const completion = page.waitForResponse(
      (response) => response.request().method() === 'PATCH' && response.url().endsWith('/complete'),
    );
    await calendar.openMealAction('Mon', 'Breakfast', planData.recipeNames[0], 'Mark done');
    expect((await completion).ok()).toBe(true);
    await calendar.dayViewRadio.click();
    await expect(page.getByLabel('Done', { exact: true })).toHaveCount(1);

    const api = await createApiContext(page);
    type MealState = {
      id: string;
      recipeName: string;
      status: string;
      calories: number | string;
    };
    const readMeals = async (): Promise<MealState[]> => {
      const response = await api.get('/api/v1/meals', {
        params: { From: monday, To: monday },
      });
      expect(response.ok()).toBe(true);
      return (await response.json()) as MealState[];
    };
    try {
      const done = await readMeals();
      expect(done.find((meal) => meal.recipeName === planData.recipeNames[0])?.status).toBe('Done');

      await calendar.dayAction('Record what was actually eaten').first().click();
      const override = page.getByRole('dialog', {
        name: 'What did you eat instead?',
      });
      await override.getByPlaceholder('Search recipes…').fill(recipeName);
      await expect(override.getByRole('button', { name: 'Save override' })).toBeEnabled();
      await override.getByRole('button', { name: 'Save override' }).click();
      await expect(override).toBeHidden();
      await expect(page.getByLabel('Modified', { exact: true })).toHaveCount(1);

      await page.reload();
      await expect(page.getByLabel('Modified', { exact: true })).toHaveCount(1);
      const modified = await readMeals();
      const actual = modified.find((meal) => meal.recipeName === planData.recipeNames[0]);
      expect(actual?.status).toBe('Modified');
      await expect(calendar.dayEaten).toHaveText(String(Math.round(Number(actual?.calories))));

      await calendar.dayAction('Revert to planned').click();
      await expect(page.getByLabel('Planned', { exact: true })).toHaveCount(2);
      await page.reload();
      await expect(page.getByLabel('Planned', { exact: true })).toHaveCount(2);
      await expect(calendar.dayEaten).toHaveText('0');

      await calendar.weekViewRadio.click();
      await expect(calendar.weekGrid).toBeVisible();
      const bulkCompletion = page.waitForResponse(
        (response) =>
          response.request().method() === 'POST' && response.url().endsWith('/bulk-complete'),
      );
      await calendar.dayAction('✓ all').first().click();
      expect((await bulkCompletion).ok()).toBe(true);
      await calendar.dayViewRadio.click();
      await expect(page.getByLabel('Done', { exact: true })).toHaveCount(2);
      await page.reload();
      await expect(page.getByLabel('Done', { exact: true })).toHaveCount(2);
      const completed = await readMeals();
      expect(completed).toHaveLength(2);
      expect(completed.every((meal) => meal.status === 'Done')).toBe(true);
      await expect(calendar.dayEaten).toHaveText(
        String(Math.round(completed.reduce((total, meal) => total + Number(meal.calories), 0))),
      );
    } finally {
      await api.dispose();
    }
  });
});
