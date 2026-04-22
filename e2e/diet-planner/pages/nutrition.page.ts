import { expect } from '@playwright/test';

import { BasePage } from './BasePage';

import type { Page, Locator } from '@playwright/test';

export class NutritionPage extends BasePage {
  readonly fromInput: Locator;
  readonly toInput: Locator;
  readonly applyButton: Locator;
  readonly tableRows: Locator;
  readonly pageSizeSelect: Locator;
  readonly previousButton: Locator;
  readonly nextButton: Locator;

  constructor(page: Page) {
    super(page);
    this.fromInput = page.getByTestId('from-date-picker');
    this.toInput = page.getByTestId('to-date-picker');
    this.applyButton = page.getByRole('button', { name: /apply/i });
    this.tableRows = page.getByRole('table').getByRole('row').filter({ hasNot: page.locator('th') });
    this.pageSizeSelect = page.getByRole('combobox').or(page.locator('select')).first();
    this.previousButton = page.getByRole('button', { name: /previous/i });
    this.nextButton = page.getByRole('button', { name: /next/i });
  }

  async goto() {
    const responsePromise = this.page.waitForResponse(
      (resp) => resp.url().includes('/nutrition-summary') && resp.request().method() === 'GET',
      { timeout: 10000 },
    );
    await this.page.goto('/diet-planner/nutrition');
    await responsePromise;
  }

  /** Returns the ISO date string (YYYY-MM-DD) currently shown in the from picker. */
  async getFromValue(): Promise<string> {
    return (await this.fromInput.getAttribute('data-value')) ?? '';
  }

  /** Returns the ISO date string (YYYY-MM-DD) currently shown in the to picker. */
  async getToValue(): Promise<string> {
    return (await this.toInput.getAttribute('data-value')) ?? '';
  }

  /**
   * Open the DatePicker identified by testId and navigate to the given ISO date
   * using the month/year dropdowns, then click the day button.
   */
  private async selectDate(testId: string, dateStr: string) {
    const trigger = this.page.getByTestId(testId);

    // Skip if already set — re-clicking in mode="single" would deselect.
    const currentValue = await trigger.getAttribute('data-value');
    if (currentValue === dateStr) {
      return;
    }

    const [yearStr, monthStr, dayStr] = dateStr.split('-');
    const year = parseInt(yearStr, 10);
    const month = parseInt(monthStr, 10); // 1-based
    const day = parseInt(dayStr, 10);

    await trigger.click();

    // The calendar popover is rendered in a Radix portal
    const popover = this.page.locator('[data-radix-popper-content-wrapper]').last();
    await popover.waitFor({ state: 'visible', timeout: 5000 });

    const selects = popover.locator('select');

    // Identify month vs year select: year options are 4-digit numbers
    const selectCount = await selects.count();
    for (let i = 0; i < selectCount; i++) {
      const sel = selects.nth(i);
      const firstValue = await sel.locator('option').first().getAttribute('value');
      if (firstValue && firstValue.length === 4) {
        await sel.selectOption(String(year));
      } else {
        // Month select — react-day-picker uses 0-based month index values
        await sel.selectOption(String(month - 1));
      }
    }

    // Verify dropdown selections took effect (React re-render completed)
    for (let i = 0; i < selectCount; i++) {
      const sel = selects.nth(i);
      const firstValue = await sel.locator('option').first().getAttribute('value');
      if (firstValue && firstValue.length === 4) {
        await expect(sel).toHaveValue(String(year));
      } else {
        await expect(sel).toHaveValue(String(month - 1));
      }
    }

    // Click the day button. Use a JS click (evaluate) instead of Playwright's
    // native click because Radix Portal event handling can prevent Playwright's
    // CDP-dispatched pointer events from reaching React's synthetic event system.
    const dayButton = popover
      .locator('button')
      .filter({ hasText: new RegExp(`^\\s*${String(day)}\\s*$`) })
      .first();
    await dayButton.evaluate((node) => (node as HTMLButtonElement).click());

    // Close the popover explicitly so it doesn't interfere with subsequent interactions
    await this.page.keyboard.press('Escape');
    await popover.waitFor({ state: 'hidden', timeout: 3000 }).catch(() => undefined);

    // Verify the trigger reflects the selected date
    await expect(trigger).toHaveAttribute('data-value', dateStr, { timeout: 5000 });
  }

  async setDateRange(from: string, to: string) {
    await this.selectDate('from-date-picker', from);
    await this.selectDate('to-date-picker', to);
  }

  async applyRange(expectedFrom?: string, expectedTo?: string) {
    const urlFilter = (resp: { url: () => string; request: () => { method: () => string } }) => {
      if (!resp.url().includes('/nutrition-summary') || resp.request().method() !== 'GET') return false;
      if (expectedFrom && !resp.url().includes(`from=${expectedFrom}`)) return false;
      if (expectedTo && !resp.url().includes(`to=${expectedTo}`)) return false;
      return true;
    };
    const responsePromise = this.page.waitForResponse(urlFilter, { timeout: 10000 });
    await this.applyButton.click();
    await responsePromise;
  }

  async getTableRowCount(): Promise<number> {
    try {
      await this.tableRows.first().waitFor({ state: 'attached', timeout: 8000 });
    } catch {
      return 0;
    }
    return this.tableRows.count();
  }

  async getPageSize(): Promise<number> {
    return parseInt(await this.pageSizeSelect.inputValue(), 10);
  }

  async setPageSize(size: number) {
    await this.pageSizeSelect.selectOption(String(size));
  }

  async expectTotalsVisible() {
    await expect(this.page.getByText(/totals/i).first()).toBeVisible({ timeout: 8000 });
  }

  async expectDailyAvgVisible() {
    await expect(this.page.getByText(/daily average/i)).toBeVisible({ timeout: 8000 });
  }

  async expectEmptyState() {
    await expect(this.page.getByText(/no meal data/i)).toBeVisible({ timeout: 8000 });
  }

  async expectGoalProgressVisible() {
    await expect(this.page.getByText(/goal progress/i)).toBeVisible({ timeout: 8000 });
  }
}
