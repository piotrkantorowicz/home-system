import { Page, Locator, expect } from '@playwright/test';

export class DietPlansPage {
  readonly page: Page;
  readonly mealFormDialog: Locator;

  constructor(page: Page) {
    this.page = page;
    this.mealFormDialog = page.getByRole('dialog');
  }

  async goto() {
    await this.page.goto('/diet-planner/calendar');
  }

  /**
   * Get all weekday column cards on the calendar page.
   * Returns an array of 7 locators (Mon-Sun).
   */
  getWeekDayColumns() {
    // The calendar page renders a 7-column grid of Cards
    return this.page.locator('.lg\\:grid-cols-7 > div');
  }

  /**
   * Get the day column for a specific weekday abbreviation (e.g., 'Mon', 'Tue', 'Sat').
   */
  getDayColumn(weekdayAbbr: string) {
    return this.getWeekDayColumns().filter({ hasText: weekdayAbbr });
  }

  /**
   * Assert that a specific meal is visible in a day column.
   */
  async expectMealInDay(weekdayAbbr: string, recipeName: string) {
    const dayCol = this.getDayColumn(weekdayAbbr).first();
    await expect(dayCol).toBeVisible({ timeout: 10000 });
    await expect(dayCol.getByText(recipeName)).toBeVisible({ timeout: 5000 });
  }

  /**
   * Assert that a day column shows "No meal" for a given meal type.
   */
  async expectNoMealInDay(weekdayAbbr: string) {
    const dayCol = this.getDayColumn(weekdayAbbr).first();
    await expect(dayCol).toBeVisible({ timeout: 10000 });
    await expect(dayCol.getByText(/no meal/i).first()).toBeVisible();
  }

  /**
   * Assert that all 7 day columns are rendered.
   */
  async expectAllDaysRendered() {
    const columns = this.getWeekDayColumns();
    await expect(columns).toHaveCount(7, { timeout: 10000 });
  }

  /**
   * Get the meal-type section within a day column.
   * e.g., getDayMealTypeSection('Mon', 'Dinner')
   */
  getDayMealTypeSection(weekdayAbbr: string, mealTypeLabel: string) {
    return this.getDayColumn(weekdayAbbr)
      .first()
      .locator('div')
      .filter({ hasText: new RegExp(`^${mealTypeLabel}$`, 'i') })
      .first();
  }

  /**
   * Click the "+" button for a given meal type within a day column.
   */
  async clickAddMeal(weekdayAbbr: string, mealTypeLabel: string) {
    const dayCol = this.getDayColumn(weekdayAbbr).first();
    await expect(dayCol).toBeVisible({ timeout: 10000 });

    // Find the section header row that contains the meal type label and the + button
    const sectionRow = dayCol.locator('div.flex.items-center.justify-between').filter({
      hasText: new RegExp(mealTypeLabel, 'i'),
    });
    const addButton = sectionRow.locator('button[title]');
    // Use JS .click() to bypass layout-based pointer-event interception.
    // The untranslated key text overflows in narrow columns and visually covers the button.
    await addButton.evaluate((el) => (el as HTMLButtonElement).click());
    await expect(this.mealFormDialog).toBeVisible({ timeout: 5000 });
  }

  /**
   * Fill in the meal form. Assumes the dialog is already open.
   * - recipeName: typed in the search box; the matching suggestion is clicked.
   * - servings: overrides the default value if provided.
   * - notes: optional notes text.
   */
  async fillMealForm(recipeName: string, servings?: number, notes?: string) {
    const dialog = this.mealFormDialog;

    // Recipe search
    const recipeInput = dialog.locator('input#recipe-search');
    await recipeInput.fill(recipeName);
    // Wait for the dropdown suggestion and click it
    const suggestion = dialog.locator('button').filter({ hasText: recipeName }).first();
    await expect(suggestion).toBeVisible({ timeout: 5000 });
    await suggestion.click();

    if (servings !== undefined) {
      const servingsInput = dialog.locator('input#servings');
      await servingsInput.fill(String(servings));
    }

    if (notes !== undefined) {
      const notesInput = dialog.locator('input#notes');
      await notesInput.fill(notes);
    }
  }

  /**
   * Submit the open meal form.
   */
  async submitMealForm() {
    // Use the native submit button type — avoids depending on translated button labels.
    const submitBtn = this.mealFormDialog.locator('button[type="submit"]');
    await submitBtn.click();
    await expect(this.mealFormDialog).not.toBeVisible({ timeout: 10000 });
  }

  /**
   * Open the edit form for an existing meal identified by recipe name.
   */
  async openEditMeal(recipeName: string) {
    const mealCard = this.page.locator('div.group').filter({ hasText: recipeName }).first();
    await mealCard.hover();
    const editBtn = mealCard
      .locator('button')
      .filter({ has: this.page.locator('svg.lucide-pencil') });
    await editBtn.click();
    await expect(this.mealFormDialog).toBeVisible({ timeout: 5000 });
  }

  /**
   * Delete a meal identified by recipe name, confirming the dialog.
   */
  async deleteMeal(recipeName: string) {
    const mealCard = this.page.locator('div.group').filter({ hasText: recipeName }).first();
    await mealCard.hover();
    const deleteBtn = mealCard
      .locator('button')
      .filter({ has: this.page.locator('svg.lucide-trash-2') });
    await deleteBtn.click();

    // Wait for the confirmation dialog, then click the destructive button
    const dialog = this.page.getByRole('dialog');
    await expect(dialog).toBeVisible({ timeout: 5000 });
    await dialog.locator('button.bg-destructive, button[class*="destructive"]').click();
    await expect(dialog).not.toBeVisible({ timeout: 5000 });
  }
}
