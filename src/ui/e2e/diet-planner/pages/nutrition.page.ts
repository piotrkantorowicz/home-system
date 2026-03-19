import { Page, Locator, expect } from '@playwright/test';

export class NutritionPage {
  readonly page: Page;
  readonly fromInput: Locator;
  readonly toInput: Locator;
  readonly applyButton: Locator;
  readonly tableRows: Locator;
  readonly pageSizeSelect: Locator;
  readonly previousButton: Locator;
  readonly nextButton: Locator;

  constructor(page: Page) {
    this.page = page;
    this.fromInput = page.locator('input#from-date');
    this.toInput = page.locator('input#to-date');
    this.applyButton = page.getByRole('button', { name: /apply/i });
    this.tableRows = page.locator('table tbody tr');
    this.pageSizeSelect = page.locator('select');
    this.previousButton = page.getByRole('button', { name: /previous/i });
    this.nextButton = page.getByRole('button', { name: /next/i });
  }

  async goto() {
    const responsePromise = this.page.waitForResponse(
      (resp) =>
        resp.url().includes('/nutrition-summary') && resp.request().method() === 'GET',
      { timeout: 10000 }
    );
    await this.page.goto('/diet-planner/nutrition');
    await responsePromise;
  }

  async setDateRange(from: string, to: string) {
    await this.fromInput.fill(from);
    await this.toInput.fill(to);
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
    await this.tableRows.first().waitFor({ state: 'attached', timeout: 8000 }).catch(() => {});
    return await this.tableRows.count();
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
