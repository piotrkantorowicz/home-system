import { expect } from '@playwright/test';

import { BasePage } from './BasePage';

import type { Page, Locator } from '@playwright/test';

export type NutritionRange = '7' | '30' | '90';

/**
 * Nutrition Summary (redesign v3): 7/30/90-day segmented range, neutral summary tiles,
 * labelled daily bars, kcal-based macro bars and a native daily-totals table (newest
 * first, unpaginated up to 31 rows). See docs/e2e/nutrition.md.
 */
export class NutritionPage extends BasePage {
  readonly rangeGroup: Locator;
  readonly previousButton: Locator;
  readonly nextButton: Locator;
  readonly tableRows: Locator;

  constructor(page: Page) {
    super(page);
    this.rangeGroup = page.getByRole('radiogroup', { name: /range/i });
    this.previousButton = page.getByRole('button', { name: /previous/i });
    this.nextButton = page.getByRole('button', { name: /next/i });
    this.tableRows = page.getByRole('table').getByRole('row');
  }

  async goto() {
    // Require a successful summary — a failed request renders the error
    // banner, which would otherwise be mistaken for "no data" downstream.
    const responsePromise = this.page.waitForResponse(
      (resp) =>
        resp.url().includes('/nutrition-summary') && resp.request().method() === 'GET' && resp.ok(),
      { timeout: 10000 },
    );
    await this.page.goto('/diet-planner/nutrition');
    await responsePromise;
  }

  rangeOption(range: NutritionRange): Locator {
    const labels: Record<NutritionRange, string> = {
      '7': '7 days',
      '30': '30 days',
      '90': '90 days',
    };
    return this.rangeGroup.getByRole('radio', { name: labels[range] });
  }

  async selectRange(range: NutritionRange) {
    // Require a successful summary — a failed request renders the error
    // banner, which would otherwise be mistaken for "no data" downstream.
    const responsePromise = this.page.waitForResponse(
      (resp) =>
        resp.url().includes('/nutrition-summary') && resp.request().method() === 'GET' && resp.ok(),
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

  async expectEmptyState() {
    await expect(this.page.getByText(/no meal data for the selected range/i)).toBeVisible({
      timeout: 8000,
    });
  }

  async expectChartVisible() {
    await expect(this.page.getByText(/intake vs\.? target/i)).toBeVisible({
      timeout: 8000,
    });
  }

  async expectMacroSplitVisible() {
    await expect(this.page.getByText(/where calories come from/i)).toBeVisible({
      timeout: 8000,
    });
  }

  /** One cell of a day's row (`tr[data-date]`) — column order matches `NutritionSummary.tsx`. */
  dayCell(date: string, column: 'calories' | 'protein' | 'carbs' | 'fat' | 'fiber'): Locator {
    const index = { calories: 1, protein: 3, carbs: 4, fat: 5, fiber: 6 }[column];
    return this.page.locator(`tr[data-date="${date}"]`).getByRole('cell').nth(index);
  }

  /** The chart's screen-reader list gives every day an item ending in `"<kcal> kcal"`. */
  chartBar(kcal: number): Locator {
    return this.page
      .getByRole('figure')
      .getByRole('listitem')
      .filter({ hasText: `: ${kcal} kcal` })
      .first();
  }

  tileValue(label: string): Locator {
    return this.page.getByText(label, { exact: true }).locator('xpath=..').locator('> div').nth(1);
  }
}
