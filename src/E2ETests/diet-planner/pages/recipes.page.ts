import { expect } from '@playwright/test';

import type { Page, Locator } from '@playwright/test';

export class RecipesPage {
  readonly page: Page;
  readonly createButton: Locator;
  readonly searchInput: Locator;

  constructor(page: Page) {
    this.page = page;
    this.createButton = page.getByRole('link', { name: /create recipe/i });
    this.searchInput = page.getByPlaceholder(/search/i);
  }

  async goto() {
    await this.page.goto('/diet-planner/recipes');
    await this.page.waitForLoadState('networkidle');
  }

  // ── Create ──────────────────────────────────────────────────────────────────

  async createRecipe(data: {
    name: string;
    servings: number;
    ingredients: { name: string; amount: number; unit: string }[];
    prepTime?: number;
    instructions?: string;
  }) {
    await this.createButton.click();
    await this.page.waitForURL('**/recipes/new');

    await this.page.getByLabel(/recipe name/i).fill(data.name);
    await this.page.getByLabel(/servings/i).fill(String(data.servings));

    if (data.prepTime !== undefined) {
      await this.page.getByLabel(/prep time/i).fill(String(data.prepTime));
    }

    if (data.instructions) {
      await this.page.getByLabel(/instructions/i).fill(data.instructions);
    }

    for (const [index, ingredient] of data.ingredients.entries()) {
      if (index > 0) {
        await this.page.getByRole('button', { name: /add ingredient/i }).click();
      }

      // Product name field uses a datalist-backed input — fill by placeholder
      const productInputs = this.page.getByPlaceholder(/search product/i);
      await productInputs.nth(index).fill(ingredient.name);

      // Amount field — locate by placeholder
      const amountInputs = this.page.getByPlaceholder('100');
      await amountInputs.nth(index).fill(String(ingredient.amount));

      // Unit select — the product search inputs also have combobox role (datalist), so target <select> elements directly
      const unitSelects = this.page.locator('select[name^="ingredients"]');
      await unitSelects.nth(index).selectOption(ingredient.unit);
    }

    await this.page.getByRole('button', { name: /save|create/i }).click();
    await this.page.waitForURL(/\/diet-planner\/recipes$/);
    await this.page.waitForLoadState('networkidle');
  }

  // ── Search ──────────────────────────────────────────────────────────────────

  async searchFor(query: string) {
    // Register the response listener BEFORE filling to avoid missing
    // a fast response that arrives between fill() and waitForResponse().
    const responsePromise = this.page.waitForResponse(
      (resp) => resp.url().includes('/api/v1/recipes') && resp.request().method() === 'GET',
      { timeout: 10_000 },
    );
    await this.searchInput.fill(query);
    // If the search term is the same as the current value, no API call may
    // be made. Fall back to networkidle to ensure the UI is settled.
    await responsePromise.catch(() => this.page.waitForLoadState('networkidle'));
  }

  // ── Read ─────────────────────────────────────────────────────────────────────

  recipeCardFor(name: string): Locator {
    return this.page
      .locator('div')
      .filter({ has: this.page.getByRole('heading', { name }) })
      .filter({ has: this.page.getByRole('link', { name: /view/i }) })
      .first();
  }

  async expectRecipeVisible(name: string) {
    await this.searchFor(name);
    await expect(this.page.getByText(name).first()).toBeVisible({ timeout: 10000 });
  }

  async expectRecipeNotVisible(name: string) {
    await expect(this.page.getByRole('heading', { name })).not.toBeVisible({ timeout: 5000 });
  }

  // ── Edit ─────────────────────────────────────────────────────────────────────

  async editRecipe(name: string) {
    await this.searchFor(name);
    const card = this.recipeCardFor(name);
    const editLink = card.getByRole('link', { name: /edit/i }).first();

    await expect(editLink).toBeVisible({ timeout: 10000 });
    await editLink.click();
    await this.page.waitForURL(/\/edit$/);
  }

  // ── Delete ───────────────────────────────────────────────────────────────────

  async deleteRecipe(name: string) {
    await this.searchFor(name);
    const card = this.recipeCardFor(name);
    await expect(card).toBeVisible({ timeout: 10000 });

    const deleteButton = card.getByRole('button', { name: /delete/i }).first();
    await deleteButton.click();

    const dialog = this.page.getByRole('dialog');
    await expect(dialog.getByText(/delete recipe/i)).toBeVisible({ timeout: 5000 });
    await dialog.getByRole('button', { name: /^delete$/i }).click();
    await expect(dialog).not.toBeVisible({ timeout: 5000 });
  }
}
