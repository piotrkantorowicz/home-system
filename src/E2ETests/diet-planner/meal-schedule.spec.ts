import { test, expect } from './fixtures';

import { MealSchedulePage } from './pages/meal-schedule.page';

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

  test('saving shows success message', async ({ page }) => {
    const schedulePage = new MealSchedulePage(page);
    await schedulePage.goto();

    // Toggle between two names so the form is always dirty regardless of prior run state
    const currentName = await schedulePage.slotNameInput(0).inputValue();
    await schedulePage.slotNameInput(0).fill(currentName === 'Morning Meal' ? 'Breakfast' : 'Morning Meal');
    await schedulePage.save();

    await expect(schedulePage.successMessage).toBeVisible({ timeout: 10_000 });
  });
});
