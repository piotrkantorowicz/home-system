import { Page, Locator, expect } from '@playwright/test';

export class ProductsPage {
  readonly page: Page;
  readonly createButton: Locator;
  readonly searchInput: Locator;
  readonly productTable: Locator;
  readonly productRows: Locator;

  constructor(page: Page) {
    this.page = page;
    this.createButton = page.getByRole('link', { name: /add product/i });
    this.searchInput = page.getByPlaceholder(/search/i);
    this.productTable = page.locator('table');
    this.productRows = page.locator('table tbody tr');
  }

  async goto() {
    await this.page.goto('/diet-planner/products');
  }

  async createProduct(data: {
    name: string;
    calories: number;
    protein: number;
    carbs: number;
    fat: number;
  }) {
    await this.createButton.click();
    await this.page.waitForURL('/diet-planner/products/new');

    await this.page.fill('input[name="name"]', data.name);
    await this.page.fill('input[name="caloriesPer100g"]', data.calories.toString());
    await this.page.fill('input[name="proteinPer100g"]', data.protein.toString());
    await this.page.fill('input[name="carbsPer100g"]', data.carbs.toString());
    await this.page.fill('input[name="fatPer100g"]', data.fat.toString());

    await this.page.click('button[type="submit"]');
    await this.page.waitForURL(/\/products$/);
  }

  async searchProducts(query: string) {
    await this.searchInput.fill(query);
    // Wait for debounce
    await this.page.waitForTimeout(300);
  }

  async getProductCount(): Promise<number> {
    return await this.productRows.count();
  }

  async editProduct(name: string, newData: { calories?: number }) {
    const row = this.page.getByRole('row', { name: new RegExp(name) });
    await row.waitFor({ state: 'visible' });
    await row.getByRole('link', { name: /edit/i }).click();
    await this.page.waitForURL(/\/edit$/);

    if (newData.calories !== undefined) {
      await this.page.fill('input[name="caloriesPer100g"]', newData.calories.toString());
    }

    await this.page.click('button[type="submit"]');
    // Wait for redirect to detail page (e.g. /products/uuid)
    await this.page.waitForURL(/\/products\/[a-z0-9-]+$/);
  }

  async deleteProduct(name: string) {
    const row = this.page.getByRole('row', { name: new RegExp(name) });
    await row.waitFor({ state: 'visible' });
    // Assuming delete button is available in the row or detail view
    // In ProductList.tsx, delete is an icon button in the row
    await row
      .getByRole('button')
      .filter({ has: this.page.locator('svg.lucide-trash-2') })
      .click();

    // Confirm dialog
    await expect(this.page.getByText(/delete product/i)).toBeVisible();
    await this.page.getByRole('button', { name: /delete/i }).click();
  }
}
