import { expect } from '@playwright/test';

import { BasePage } from './BasePage';

import type { Page, Locator } from '@playwright/test';

export type NutritionRange = '7' | '30' | '90';

/**
 * Nutrition Summary dropped the custom from/to DatePicker range in the #208
 * redesign — it now offers three fixed presets (7/30/90 days ending today)
 * via a SegmentedControl, plus a bar chart, a macro split, and a paginated
 * daily-breakdown table. See docs/e2e/nutrition.md.
 */
export class NutritionPage extends BasePage {
  readonly rangeGroup: Locator;
  readonly pageSizeSelect: Locator;
  readonly previousButton: Locator;
  readonly nextButton: Locator;
  readonly tableRows: Locator;

  constructor(page: Page) {
    super(page);
    this.rangeGroup = page.getByRole('radiogroup', { name: /range/i });
    this.pageSizeSelect = page.getByRole('combobox').or(page.locator('select')).first();
    this.previousButton = page.getByRole('button', { name: /previous/i });
    this.nextButton = page.getByRole('button', { name: /next/i });
    // The daily-breakdown table is a CSS-grid list, not a native <table> —
    // each row div carries role="row" (see NutritionSummary.tsx).
    this.tableRows = page.getByRole('table').getByRole('row');
  }

  async goto() {
    const responsePromise = this.page.waitForResponse(
      (resp) => resp.url().includes('/nutrition-summary') && resp.request().method() === 'GET',
      { timeout: 10000 },
    );
    await this.page.goto('/diet-planner/nutrition');
    await responsePromise;
  }

  rangeOption(range: NutritionRange): Locator {
    const labels: Record<NutritionRange, string> = { '7': '7 days', '30': '30 days', '90': '90 days' };
    return this.rangeGroup.getByRole('radio', { name: labels[range] });
  }

  async selectRange(range: NutritionRange) {
    const responsePromise = this.page.waitForResponse(
      (resp) => resp.url().includes('/nutrition-summary') && resp.request().method() === 'GET',
      { timeout: 10000 },
    );
    await this.rangeOption(range).click();
    await responsePromise;
  }

  async getTableRowCount(): Promise<number> {
    try {
      await this.tableRows.first().waitFor({ state: 'attached', timeout: 8000 });
    } catch {
      return 0;
    }
    // Minus the header row (also role="row").
    return (await this.tableRows.count()) - 1;
  }

  async getPageSize(): Promise<number> {
    return parseInt(await this.pageSizeSelect.inputValue(), 10);
  }

  async setPageSize(size: number) {
    await this.pageSizeSelect.selectOption(String(size));
  }

  async expectEmptyState() {
    await expect(this.page.getByText(/no meal data for the selected range/i)).toBeVisible({
      timeout: 8000,
    });
  }

  async expectChartVisible() {
    await expect(this.page.getByText(/intake vs\.? target/i)).toBeVisible({ timeout: 8000 });
  }

  async expectMacroSplitVisible() {
    await expect(this.page.getByText(/macro split/i)).toBeVisible({ timeout: 8000 });
  }
}
