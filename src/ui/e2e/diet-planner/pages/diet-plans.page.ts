import { Page, Locator, expect } from '@playwright/test';

export class DietPlansPage {
  readonly page: Page;
  readonly importButton: Locator;

  constructor(page: Page) {
    this.page = page;
    this.importButton = page.getByRole('link', { name: /import diet plan/i });
  }

  async goto() {
    await this.page.goto('/diet-plans');
  }

  async openPlan(name: string) {
    // Find the card containing the plan name, then click its "View Calendar" link
    const card = this.page
      .locator('div')
      .filter({ hasText: name })
      .locator('a', { hasText: /view calendar/i })
      .first();
    await card.click();
    await expect(this.page.getByText(/week of/i)).toBeVisible({ timeout: 10000 });
  }

  async deletePlan(name: string) {
    const card = this.page
      .locator('div')
      .filter({ hasText: name })
      .locator('button')
      .filter({ has: this.page.locator('svg.lucide-trash-2') })
      .first();
    await card.click();

    // Confirm dialog
    await expect(this.page.getByText(/delete diet plan/i)).toBeVisible();
    await this.page.getByRole('button', { name: /delete/i }).click();
  }

  /**
   * Get all weekday column cards on the detail page.
   * Returns an array of 7 locators (Mon-Sun).
   */
  getWeekDayColumns() {
    // The detail page renders a 7-column grid of Cards
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
}
