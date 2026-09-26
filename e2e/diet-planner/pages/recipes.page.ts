import { expect } from '@playwright/test';

import { BasePage } from './BasePage';

import type { Page, Locator } from '@playwright/test';

export class RecipesPage extends BasePage {
  readonly createButton: Locator;
  readonly searchInput: Locator;
  readonly visibilitySelect: Locator;

  constructor(page: Page) {
    super(page);
    this.createButton = page.getByRole('link', { name: /create recipe/i });
    this.searchInput = page.getByPlaceholder(/search/i);
    this.visibilitySelect = page.getByLabel(/who can see it/i);
  }

  async goto() {
    await this.page.goto('/diet-planner/recipes');
    // Not the create button — a Guest never gets one.
    await this.searchInput.waitFor();
  }

  // ── Create ──────────────────────────────────────────────────────────────────

  async createRecipe(data: {
    name: string;
    servings: number;
    ingredients: { name: string; amount: number; unit: string }[];
    prepTime?: number;
    instructions?: string;
    visibility?: 'Private' | 'Household' | 'Public';
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

    if (data.visibility) {
      await this.visibilitySelect.selectOption(data.visibility);
    }

    for (const [index, ingredient] of data.ingredients.entries()) {
      if (index > 0) {
        await this.page.getByRole('button', { name: /add ingredient/i }).click();
      }

      // The product field is a type-ahead combobox (ProductPicker) — type the
      // name and pick the matching option from its listbox. Relying on the
      // blur-triggered "exact match" auto-commit is a race against the
      // in-flight product search, so click the option explicitly instead.
      const productInput = this.page.getByPlaceholder(/search product/i).nth(index);
      // Centre the field first so the listbox (a position:fixed popover
      // anchored just below the input) opens inside the viewport.
      await productInput.evaluate((el) => {
        el.scrollIntoView({ block: 'center' });
      });
      await productInput.fill(ingredient.name);
      const option = this.page.getByRole('option', { name: ingredient.name }).first();
      await option.waitFor({ state: 'visible', timeout: 10000 });
      await option.click({ force: true });

      const amountInputs = this.page.getByPlaceholder('100');
      await amountInputs.nth(index).fill(String(ingredient.amount));

      await this.page
        .getByRole('combobox', { name: /^unit/i })
        .nth(index)
        .selectOption(ingredient.unit);
    }

    await this.page.getByRole('button', { name: /save|create/i }).click();
    await this.page.waitForURL(/\/diet-planner\/recipes$/);
    await this.createButton.waitFor();
  }

  // ── Search ──────────────────────────────────────────────────────────────────

  async searchFor(query: string) {
    // Typing the term that is already in the box fires no request — the list
    // is already filtered by it, so there is nothing to wait for.
    if ((await this.searchInput.inputValue()) === query) return;

    // Register the response listener BEFORE filling to avoid missing
    // a fast response that arrives between fill() and waitForResponse().
    const responsePromise = this.page.waitForResponse(
      (resp) => resp.url().includes('/api/v1/recipes') && resp.request().method() === 'GET',
      { timeout: 10_000 },
    );
    await this.searchInput.fill(query);
    await responsePromise;
  }

  // ── Read ─────────────────────────────────────────────────────────────────────

  // RecipeCard's root carries role="listitem" + aria-label={recipe.name}
  // (there is no heading in the card — the name is a plain link).
  recipeCardFor(name: string): Locator {
    return this.page.getByRole('listitem', { name });
  }

  async expectRecipeVisible(name: string) {
    await this.searchFor(name);
    await expect(this.recipeCardFor(name)).toBeVisible({ timeout: 10000 });
  }

  /** The visibility badge on the recipe card, e.g. "Private". */
  visibilityBadgeFor(name: string, visibility: string): Locator {
    return this.recipeCardFor(name).getByText(visibility, { exact: true });
  }

  async expectRecipeNotVisible(name: string) {
    await expect(this.recipeCardFor(name)).not.toBeVisible({ timeout: 5000 });
  }

  // ── Row actions ────────────────────────────────────────────────────────────────
  // Edit/View/Delete live behind a "…" dropdown menu, portaled to
  // document.body by Radix — so once open, its items are queried at the page
  // level rather than scoped to the card.

  private async openCardMenu(card: Locator) {
    await card.getByRole('button', { name: /^actions$/i }).click();
  }

  /** Opens the card's "…" menu and clicks "View". */
  async viewRecipe(name: string) {
    await this.searchFor(name);
    const card = this.recipeCardFor(name);
    await expect(card).toBeVisible({ timeout: 10000 });
    await this.openCardMenu(card);
    await this.page.getByRole('menuitem', { name: /^view$/i }).click();
  }

  // ── Edit ─────────────────────────────────────────────────────────────────────

  async editRecipe(name: string) {
    await this.searchFor(name);
    const card = this.recipeCardFor(name);
    await expect(card).toBeVisible({ timeout: 10000 });
    await this.openCardMenu(card);
    await this.page.getByRole('menuitem', { name: /^edit$/i }).click();
    await this.page.waitForURL(/\/edit$/);
  }

  async updateIngredientAmount(name: string, amount: number) {
    const products = this.page.getByPlaceholder(/search product/i);
    for (let index = 0; index < (await products.count()); index++) {
      if ((await products.nth(index).inputValue()) === name) {
        await this.page.getByPlaceholder('100').nth(index).fill(String(amount));
        return;
      }
    }
    throw new Error(`Ingredient not found: ${name}`);
  }

  // ── Delete ───────────────────────────────────────────────────────────────────

  async deleteRecipe(name: string) {
    await this.searchFor(name);
    const card = this.recipeCardFor(name);
    await expect(card).toBeVisible({ timeout: 10000 });
    await this.openCardMenu(card);
    await this.page.getByRole('menuitem', { name: /^delete$/i }).click();

    const dialog = this.page.getByRole('dialog');
    await expect(dialog.getByText(/delete recipe/i)).toBeVisible({ timeout: 5000 });
    await dialog.getByRole('button', { name: /^delete$/i }).click();
    await expect(dialog).not.toBeVisible({ timeout: 5000 });
  }
}
