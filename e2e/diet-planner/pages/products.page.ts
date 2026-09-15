import { expect } from '@playwright/test';

import { BasePage } from './BasePage';

import type { Page, Locator } from '@playwright/test';

export class ProductsPage extends BasePage {
  readonly createButton: Locator;
  readonly searchInput: Locator;

  constructor(page: Page) {
    super(page);
    this.createButton = page.getByRole('link', { name: /add product/i });
    this.searchInput = page.getByPlaceholder(/search/i);
  }

  async goto() {
    await this.page.goto('/diet-planner/products');
    await this.createButton.waitFor();
  }

  // ── Create ──────────────────────────────────────────────────────────────────

  async createProduct(data: {
    name: string;
    calories?: number;
    protein?: number;
    carbs?: number;
    fat?: number;
    fiber?: number;
  }) {
    await this.createButton.click();
    await this.page.waitForURL('**/products/new');

    await this.page.getByRole('textbox', { name: /product name/i }).fill(data.name);

    if (data.calories !== undefined)
      await this.page.getByRole('spinbutton', { name: /calories/i }).fill(String(data.calories));
    if (data.protein !== undefined)
      await this.page.getByRole('spinbutton', { name: /protein/i }).fill(String(data.protein));
    if (data.carbs !== undefined)
      await this.page.getByRole('spinbutton', { name: /carbs/i }).fill(String(data.carbs));
    if (data.fat !== undefined)
      await this.page.getByRole('spinbutton', { name: /^fat/i }).fill(String(data.fat));
    if (data.fiber !== undefined)
      await this.page.getByRole('spinbutton', { name: /fiber/i }).fill(String(data.fiber));

    await this.page.getByRole('button', { name: /save|create/i }).click();
    await this.page.waitForURL(/\/diet-planner\/products$/);
  }

  // ── Search ──────────────────────────────────────────────────────────────────

  async searchFor(query: string) {
    await this.searchInput.fill(query);
    await this.page.waitForResponse(
      (resp) => resp.url().includes('/api/v1/products') && resp.request().method() === 'GET',
    );
  }

  // ── Read ────────────────────────────────────────────────────────────────────

  // The table view is a CSS-grid list, not a native <table> — each row div
  // carries an explicit role="row" + aria-label={name} (see ProductList.tsx)
  // so it's still reachable by role + name.
  rowFor(name: string): Locator {
    return this.page.getByRole('row', { name });
  }

  async expectProductVisible(name: string) {
    await this.searchFor(name);
    await expect(this.rowFor(name)).toBeVisible({ timeout: 10000 });
  }

  async expectProductNotVisible(name: string) {
    await expect(this.rowFor(name)).not.toBeVisible({ timeout: 5000 });
  }

  // ── Row actions ────────────────────────────────────────────────────────────────
  // Row actions (Edit/Delete) live behind a "…" dropdown menu, and Radix
  // portals its content to document.body — so once the menu is open, its
  // items are queried at the page level, not scoped to the row.

  private async openRowMenu(row: Locator) {
    await row.getByRole('button', { name: /^actions$/i }).click();
  }

  // ── Edit ────────────────────────────────────────────────────────────────────

  async editProduct(name: string, newData: { calories?: number }) {
    await this.searchFor(name);
    const row = this.rowFor(name);
    await expect(row).toBeVisible({ timeout: 10000 });
    await this.openRowMenu(row);
    await this.page.getByRole('menuitem', { name: /^edit$/i }).click();
    await this.page.waitForURL(/\/edit$/);

    if (newData.calories !== undefined) {
      const caloriesInput = this.page.getByRole('spinbutton', { name: /calories/i });
      await caloriesInput.fill(String(newData.calories));
    }

    await this.page.getByRole('button', { name: /save|update/i }).click();
    await this.page.waitForURL(/\/products\/[a-z0-9-]+$/);
  }

  // ── Delete ──────────────────────────────────────────────────────────────────

  async deleteProduct(name: string) {
    await this.searchFor(name);
    const row = this.rowFor(name);
    await expect(row).toBeVisible({ timeout: 10000 });
    await this.openRowMenu(row);
    await this.page.getByRole('menuitem', { name: /^delete$/i }).click();

    const dialog = this.page.getByRole('dialog');
    await expect(dialog.getByText(/delete product/i)).toBeVisible();
    await dialog.getByRole('button', { name: /^delete$/i }).click();
    await expect(dialog).not.toBeVisible({ timeout: 5000 });
  }
}
