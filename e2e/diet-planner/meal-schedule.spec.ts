import { test, expect } from './fixtures';

import { MealSchedulePage } from './pages';
import { seedMealSchedule } from './utils/seed';

test.describe('Meal Schedule', () => {
  test('navigates to meal schedule page', async ({ page }) => {
    const schedulePage = new MealSchedulePage(page);
    await schedulePage.goto();

    await expect(page).toHaveURL(/\/diet-planner\/profile\?section=meal-schedule/);
  });

  test('shows default meal slots on first visit', async ({ page }) => {
    const schedulePage = new MealSchedulePage(page);
    await schedulePage.goto();

    // Default slots from the component: Breakfast, Lunch, Snack, Dinner
    const slotCount = await schedulePage.slotCount();
    expect(slotCount).toBeGreaterThanOrEqual(1);

    await expect(schedulePage.slotNameInput(0)).toBeVisible();
    await expect(schedulePage.slotTimeInput(0)).toBeVisible();
  });

  test('save button is disabled when form is not dirty', async ({ page }) => {
    const schedulePage = new MealSchedulePage(page);
    await schedulePage.goto();

    await schedulePage.expectSaveButtonDisabled();
  });

  test('save button is enabled after editing a slot name', async ({ page }) => {
    const schedulePage = new MealSchedulePage(page);
    await schedulePage.goto();

    await schedulePage.slotNameInput(0).fill('Updated Breakfast');

    await schedulePage.expectSaveButtonEnabled();
  });

  test('can add a new slot', async ({ page }) => {
    const schedulePage = new MealSchedulePage(page);
    await schedulePage.goto();

    const initialCount = await schedulePage.slotCount();

    await schedulePage.addSlot('Late Snack', '21:00');

    expect(await schedulePage.slotCount()).toBe(initialCount + 1);
  });

  test('add slot button is disabled when 8 slots exist', async ({ page }) => {
    const schedulePage = new MealSchedulePage(page);
    await schedulePage.goto();

    // Add slots until we reach 8
    while ((await schedulePage.slotCount()) < 8) {
      await schedulePage.addSlotButton.click();
    }

    await expect(schedulePage.addSlotButton).toBeDisabled();
  });

  test('can remove a slot', async ({ page }) => {
    const schedulePage = new MealSchedulePage(page);
    await schedulePage.goto();

    const initialCount = await schedulePage.slotCount();

    // Only remove if we have more than 1 slot (remove button is disabled at 1)
    if (initialCount > 1) {
      await schedulePage.slotRemoveButton(0).click();
      expect(await schedulePage.slotCount()).toBe(initialCount - 1);
    }
  });

  test('remove button is disabled when only one slot remains', async ({ page }) => {
    const schedulePage = new MealSchedulePage(page);
    await schedulePage.goto();

    // Remove down to 1 slot
    while ((await schedulePage.slotCount()) > 1) {
      await schedulePage.slotRemoveButton(0).click();
    }

    await expect(schedulePage.slotRemoveButton(0)).toBeDisabled();
  });

  test('empty slot name or time blocks save and does not persist', async ({ page }) => {
    const schedulePage = new MealSchedulePage(page);
    await schedulePage.goto();

    // Known-good, guaranteed-dirty baseline first — a fresh worker's slot can
    // start out empty (the component's own placeholder default), and toggling
    // between two names guards against a leftover value from a previous run
    // making the fill below a no-op that never marks the form dirty.
    const currentName = await schedulePage.slotNameInput(0).inputValue();
    const baselineName = currentName === 'Breakfast' ? 'Morning Meal' : 'Breakfast';
    await schedulePage.slotNameInput(0).fill(baselineName);
    await schedulePage.slotTimeInput(0).fill('08:00');
    await schedulePage.save();
    await expect(schedulePage.successMessage).toBeVisible({ timeout: 10000 });

    await schedulePage.slotNameInput(0).fill('');
    await schedulePage.save();
    await expect(schedulePage.fieldError).toBeVisible();
    await expect(schedulePage.successMessage).toBeHidden();

    await page.goto('/diet-planner');
    await schedulePage.goto();
    await expect(schedulePage.slotNameInput(0)).toHaveValue(baselineName);

    await schedulePage.slotTimeInput(0).fill('');
    await schedulePage.save();
    await expect(schedulePage.fieldError).toBeVisible();
    await expect(schedulePage.successMessage).toBeHidden();

    await page.goto('/diet-planner');
    await schedulePage.goto();
    await expect(schedulePage.slotTimeInput(0)).toHaveValue('08:00');
  });

  test('failed schedule save shows error feedback without persisting', async ({ page }) => {
    const schedulePage = new MealSchedulePage(page);
    await schedulePage.goto();

    const currentName = await schedulePage.slotNameInput(0).inputValue();
    const baselineName = currentName === 'Breakfast' ? 'Morning Meal' : 'Breakfast';
    await schedulePage.slotNameInput(0).fill(baselineName);
    // A fresh worker's slot can start with an empty time too — fill it so the
    // baseline save below isn't blocked by the *other* field's own validation.
    await schedulePage.slotTimeInput(0).fill('08:00');
    await schedulePage.save();
    await expect(schedulePage.successMessage).toBeVisible({ timeout: 10000 });

    await page.route('**/api/v1/meal-schedule', (route) => {
      if (route.request().method() === 'GET') return route.continue();
      return route.fulfill({ status: 500, contentType: 'application/json', body: '{}' });
    });

    await schedulePage.slotNameInput(0).fill(`${baselineName} (edited)`);
    await schedulePage.save();

    await expect(page.getByText(/failed to save meal schedule/i)).toBeVisible();

    await page.unroute('**/api/v1/meal-schedule');
    await page.goto('/diet-planner');
    await schedulePage.goto();
    await expect(schedulePage.slotNameInput(0)).toHaveValue(baselineName);
  });

  test('saving shows success message', async ({ page }) => {
    const schedulePage = new MealSchedulePage(page);
    await schedulePage.goto();

    // Seed a known schedule so the first input has a deterministic starting
    // value. Without a seeded config the UI falls back to hard-coded defaults
    // that don't always hydrate before the inputValue() read below.
    await seedMealSchedule(page);
    await schedulePage.goto();

    // Toggle between two names so the form is always dirty regardless of prior run state
    const currentName = await schedulePage.slotNameInput(0).inputValue();
    await schedulePage
      .slotNameInput(0)
      .fill(currentName === 'Morning Meal' ? 'Breakfast' : 'Morning Meal');
    await schedulePage.save();

    await expect(schedulePage.successMessage).toBeVisible({ timeout: 10_000 });
  });
});
