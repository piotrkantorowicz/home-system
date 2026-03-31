import type { Page, Locator } from '@playwright/test';

export class DashboardPage {
  readonly page: Page;
  readonly productCount: Locator;
  readonly recipeCount: Locator;
  readonly calendarCount: Locator;
  readonly productCard: Locator;
  readonly recipeCard: Locator;
  readonly calendarCard: Locator;

  constructor(page: Page) {
    this.page = page;
    this.productCount = page.getByTestId('product-count');
    this.recipeCount = page.getByTestId('recipe-count');
    this.calendarCount = page.getByTestId('calendar-count');
    this.productCard = page.getByTestId('product-card');
    this.recipeCard = page.getByTestId('recipe-card');
    this.calendarCard = page.getByTestId('calendar-card');
  }

  async goto() {
    await this.page.goto('/diet-planner');
  }

  async getProductCount(): Promise<number> {
    const text = await this.productCount.textContent();
    return parseInt(text || '0', 10);
  }

  async getRecipeCount(): Promise<number> {
    const text = await this.recipeCount.textContent();
    return parseInt(text || '0', 10);
  }

  async getCalendarCount(): Promise<number> {
    const text = await this.calendarCount.textContent();
    return parseInt(text || '0', 10);
  }
}
