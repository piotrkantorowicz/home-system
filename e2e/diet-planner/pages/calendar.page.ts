import { expect } from '@playwright/test';

import { BasePage } from './BasePage';

import type { Page, Locator } from '@playwright/test';

/**
 * The calendar week view was rebuilt as WeekGrid for the #208 redesign: a
 * single ARIA grid (role="grid" / "columnheader" / "rowheader" / "gridcell"),
 * empty day/slot cells hold a dashed "Add Meal" button, and a scheduled meal
 * is a "chip" <button> that opens a dropdown menu (Edit / Delete / Mark done /
 * …) — it is no longer a link. See docs/e2e/meals.md.
 */
export class CalendarPage extends BasePage {
  /** The week grid — WeekGrid renders a skeleton until the meals query resolves. */
  readonly weekGrid: Locator;
  readonly mealFormDialog: Locator;
  readonly dayViewRadio: Locator;
  readonly weekViewRadio: Locator;
  readonly dayEaten: Locator;
  /** Household person filter — only rendered when the household has more than one member. */
  readonly personFilter: Locator;
  /** "Plan for" picker in the meal form — only when the caller may plan for someone else. */
  readonly assignToPicker: Locator;

  constructor(page: Page) {
    super(page);
    this.personFilter = page.getByRole('combobox', { name: /^person$/i });
    this.assignToPicker = page.getByRole('dialog').getByLabel(/plan for/i);
    this.weekGrid = page.getByRole('grid');
    this.mealFormDialog = page.getByRole('dialog');
    this.dayViewRadio = page.getByRole('radio', { name: /^day$/i });
    this.weekViewRadio = page.getByRole('radio', { name: /^week$/i });
    this.dayEaten = page.getByTestId('day-summary-calories');
  }

  async goto() {
    await this.page.goto('/diet-planner/calendar');
    await this.weekGrid.waitFor();
  }

  // ── Cells / chips ───────────────────────────────────────────────────────────

  /** A day/slot cell, e.g. ("Mon", "Snack") — matched on the gridcell aria-label. */
  cell(weekdayAbbr: string, slotName: string): Locator {
    return this.page.getByRole('gridcell', {
      name: `${weekdayAbbr} ${slotName}`,
    });
  }

  /** All meal chips (across the whole grid) whose recipe name contains `recipeName`. */
  mealChips(recipeName: string): Locator {
    return this.page.getByRole('button', { name: recipeName });
  }

  dayAction(name: string): Locator {
    return this.page.getByRole('button', { name, exact: true });
  }

  async openMealAction(weekday: string, slot: string, recipeName: string, action: string) {
    await this.cell(weekday, slot).getByRole('button', { name: recipeName }).click();
    await this.page.getByRole('menuitem', { name: action, exact: true }).click();
  }

  async expectMealInDay(weekdayAbbr: string, recipeName: string) {
    const mondayCells = this.page.getByRole('gridcell', {
      name: new RegExp(`^${weekdayAbbr} `),
    });
    await expect(
      mondayCells.filter({ has: this.page.getByRole('button', { name: recipeName }) }).first(),
    ).toBeVisible({ timeout: 10000 });
  }

  async expectNoMealInDay(weekdayAbbr: string, slotName: string) {
    await expect(
      this.cell(weekdayAbbr, slotName).getByRole('button', {
        name: /add meal/i,
      }),
    ).toBeVisible({ timeout: 10000 });
  }

  // ── Add meal ────────────────────────────────────────────────────────────────

  async clickAddMeal(weekdayAbbr: string, slotName: string) {
    await this.cell(weekdayAbbr, slotName)
      .getByRole('button', { name: /add meal/i })
      .click();
    await expect(this.mealFormDialog).toBeVisible({ timeout: 5000 });
  }

  // ── Meal form ───────────────────────────────────────────────────────────────

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
    await this.mealFormDialog.getByRole('button', { name: /add meal|save changes/i }).click();
    await expect(this.mealFormDialog).not.toBeVisible({ timeout: 10000 });
  }

  // ── Edit / Delete meal ──────────────────────────────────────────────────────
  // The chip's dropdown menu is portaled to document.body — its items are
  // queried at the page level once open.

  async openEditMeal(recipeName: string) {
    await this.mealChips(recipeName).first().click();
    await this.page.getByRole('menuitem', { name: /^edit$/i }).click();
    await expect(this.mealFormDialog).toBeVisible({ timeout: 5000 });
  }

  async deleteMeal(recipeName: string) {
    await this.mealChips(recipeName).first().click();
    await this.page.getByRole('menuitem', { name: /^delete$/i }).click();

    const dialog = this.page.getByRole('dialog');
    await expect(dialog.getByText(/delete meal/i)).toBeVisible({
      timeout: 5000,
    });
    await dialog.getByRole('button', { name: /^delete$/i }).click();
    await expect(dialog).not.toBeVisible({ timeout: 5000 });
  }
}
