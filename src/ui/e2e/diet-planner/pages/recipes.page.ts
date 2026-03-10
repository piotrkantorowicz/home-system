import { Page, Locator, expect } from '@playwright/test';

export class RecipesPage {
  readonly page: Page;
  readonly createButton: Locator;
  readonly searchInput: Locator;
  readonly recipeRows: Locator;
  readonly viewModeGrid: Locator;
  readonly viewModeList: Locator;

  constructor(page: Page) {
    this.page = page;
    this.createButton = page.getByRole('link', { name: /create recipe/i });
    this.searchInput = page.getByPlaceholder(/search/i);
    // Adjust selector based on list/grid view, aiming for common card/container
    this.recipeRows = page.locator('.grid > div, .space-y-3 > div');
    this.viewModeGrid = page.getByRole('button').filter({ has: page.locator('svg.lucide-grid') });
    this.viewModeList = page.getByRole('button').filter({ has: page.locator('svg.lucide-list') });
  }

  async goto() {
    await this.page.goto('/diet-planner/recipes');
  }

  async createRecipe(data: {
    name: string;
    servings: number;
    ingredients: Array<{ name: string; amount: number; unit: string }>;
    prepTime?: number;
    instructions?: string;
  }) {
    await this.createButton.click();
    await this.page.waitForURL('/diet-planner/recipes/new');

    await this.page.fill('input[name="name"]', data.name);
    await this.page.fill('input[name="servings"]', data.servings.toString());

    if (data.prepTime) {
      await this.page.fill('input[name="prepTimeMinutes"]', data.prepTime.toString());
    }

    if (data.instructions) {
      await this.page.fill('textarea[name="instructions"]', data.instructions);
    }

    // Handle Ingredients
    for (const [index, ingredient] of data.ingredients.entries()) {
      if (index > 0) {
        await this.page.getByRole('button', { name: /add ingredient/i }).click();
      }

      const _row = this.page.locator(`div.grid`).nth(index + 1); // +1 to skip basic info grid if any, relying on structure
      // Better approach: use name attributes which are stable
      await this.page.fill(`input[name="ingredients.${index}.productName"]`, ingredient.name);
      // Wait for autocomplete if needed, or just fill
      // Assuming test data uses exact names that match seeded/created products

      await this.page.fill(
        `input[name="ingredients.${index}.amount"]`,
        ingredient.amount.toString()
      );
      await this.page.selectOption(`select[name="ingredients.${index}.unit"]`, ingredient.unit);
    }

    await this.page.click('button[type="submit"]');
    await this.page.waitForURL(/\/diet-planner\/recipes$/);
  }

  async searchRecipes(query: string) {
    await this.searchInput.fill(query);
    await this.page.waitForTimeout(300); // Debounce
  }

  private getRecipeCard(name: string) {
    // Use a card-level locator that's specific enough to avoid matching parent containers
    return this.page
      .locator('[class*="rounded"]')
      .filter({ hasText: name })
      .filter({ has: this.page.getByRole('link', { name: /view/i }) })
      .first();
  }

  async editRecipe(name: string) {
    const card = this.getRecipeCard(name);
    await expect(card).toBeVisible({ timeout: 10000 });
    await card.getByRole('link', { name: /edit/i }).click();
  }

  async deleteRecipe(name: string) {
    const card = this.getRecipeCard(name);
    await expect(card).toBeVisible({ timeout: 10000 });
    await card
      .getByRole('button')
      .filter({ has: this.page.locator('svg.lucide-trash-2') })
      .click();

    // Confirm dialog
    await expect(this.page.getByText(/delete recipe/i)).toBeVisible();
    await this.page.getByRole('button', { name: /^delete$/i }).click();
  }
}
