import { expect } from '@playwright/test';

import type { Page, Locator } from '@playwright/test';

export class CalendarPage {
  readonly page: Page;
  readonly mealFormDialog: Locator;

  constructor(page: Page) {
    this.page = page;
    this.mealFormDialog = page.getByRole('dialog');
  }

  async goto() {
    await this.page.goto('/diet-planner/calendar');
    await this.page.waitForLoadState('networkidle');
  }

  // ── Week display ────────────────────────────────────────────────────────────

  async expectMealInDay(weekdayAbbr: string, recipeName: string) {
    // Find a cell in the calendar that matches the weekday header text
    const dayCard = this.page
      .locator('[class*="grid"] > *')
      .filter({ has: this.page.getByText(new RegExp(`^${weekdayAbbr}$`, 'i')) })
      .first();
    await expect(dayCard).toBeVisible({ timeout: 10000 });
    await expect(dayCard.getByText(recipeName)).toBeVisible({ timeout: 10000 });
  }

  async expectNoMealInDay(weekdayAbbr: string) {
    const dayCard = this.page
      .locator('[class*="grid"] > *')
      .filter({ has: this.page.getByText(new RegExp(`^${weekdayAbbr}$`, 'i')) })
      .first();
    await expect(dayCard).toBeVisible({ timeout: 10000 });
    await expect(dayCard.getByText(/no meal/i).first()).toBeVisible();
  }

  // ── Add meal ────────────────────────────────────────────────────────────────

  /**
   * Click the "+" button for a given meal type within a day column.
   * mealTypeLabel: the translated meal type label shown in the UI (e.g. "Breakfast", "Snack").
   */
  async clickAddMeal(weekdayAbbr: string, mealTypeLabel: string) {
    const dayCard = this.page
      .locator('[class*="grid"] > *')
      .filter({ has: this.page.getByText(new RegExp(`^${weekdayAbbr}$`, 'i')) })
      .first();
    await expect(dayCard).toBeVisible({ timeout: 10000 });

    // The + button sits next to the meal type heading, inside a flex row
    const addButton = dayCard
      .locator('div')
      .filter({ hasText: new RegExp(mealTypeLabel, 'i') })
      .getByRole('button')
      .first();

    // Force click to bypass layout-based pointer-event interception in narrow columns
    await addButton.click({ force: true });
    await expect(this.mealFormDialog).toBeVisible({ timeout: 5000 });
  }

  // ── Meal form ───────────────────────────────────────────────────────────────

  /**
   * Fill the open meal form dialog.
   * recipeName is typed into the recipe search box; the first autocomplete suggestion is clicked.
   */
  async fillMealForm(recipeName: string, servings?: number, notes?: string) {
    const dialog = this.mealFormDialog;

    const recipeInput = dialog.getByLabel(/recipe/i);
    await recipeInput.fill(recipeName);
    const suggestion = dialog.getByRole('button').filter({ hasText: recipeName }).first();
    await expect(suggestion).toBeVisible({ timeout: 5000 });
    await suggestion.click();

    if (servings !== undefined) {
      await dialog.getByLabel(/servings/i).fill(String(servings));
    }

    if (notes !== undefined) {
      await dialog.getByLabel(/notes/i).fill(notes);
    }
  }

  async submitMealForm() {
    await this.mealFormDialog.getByRole('button', { name: /save|add|update/i }).click();
    await expect(this.mealFormDialog).not.toBeVisible({ timeout: 10000 });
  }

  // ── Edit / Delete meal ──────────────────────────────────────────────────────

  async openEditMeal(recipeName: string) {
    // Find the meal card by Tailwind's "group" class which enables hover-reveal controls
    const mealCard = this.page
      .locator('.group')
      .filter({ has: this.page.getByRole('link', { name: recipeName }) })
      .first();
    await mealCard.hover();

    // Edit button: the first icon-only button after hover (Pencil)
    const editButton = mealCard.getByRole('button').first();
    await editButton.click();
    await expect(this.mealFormDialog).toBeVisible({ timeout: 5000 });
  }

  async deleteMeal(recipeName: string) {
    const mealCard = this.page
      .locator('.group')
      .filter({ has: this.page.getByRole('link', { name: recipeName }) })
      .first();
    await mealCard.hover();

    // Delete button: the second icon-only button after hover (Trash)
    const deleteButton = mealCard.getByRole('button').last();
    await deleteButton.click();

    const dialog = this.page.getByRole('dialog');
    await expect(dialog).toBeVisible({ timeout: 5000 });
    await dialog.getByRole('button', { name: /delete/i }).last().click();
    await expect(dialog).not.toBeVisible({ timeout: 5000 });
  }
}
