import { Page, Locator } from '@playwright/test';

export class DashboardPage {
  readonly page: Page;
  readonly productCount: Locator;
  readonly recipeCount: Locator;
  readonly dietPlanCount: Locator;
  readonly productCard: Locator;
  readonly recipeCard: Locator;
  readonly dietPlanCard: Locator;

  constructor(page: Page) {
    this.page = page;
    this.productCount = page.getByTestId('product-count');
    this.recipeCount = page.getByTestId('recipe-count');
    this.dietPlanCount = page.getByTestId('diet-plan-count');
    this.productCard = page.getByTestId('product-card');
    this.recipeCard = page.getByTestId('recipe-card');
    this.dietPlanCard = page.getByTestId('diet-plan-card');
  }

  async goto() {
    await this.page.goto('/');
  }

  async getProductCount(): Promise<number> {
    const text = await this.productCount.textContent();
    return parseInt(text || '0', 10);
  }

  async getRecipeCount(): Promise<number> {
    const text = await this.recipeCount.textContent();
    return parseInt(text || '0', 10);
  }

  async getDietPlanCount(): Promise<number> {
    const text = await this.dietPlanCount.textContent();
    return parseInt(text || '0', 10);
  }
}
