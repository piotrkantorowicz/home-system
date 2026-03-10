import { test, expect } from './fixtures/auth.fixture';
import { RecipesPage } from './pages/recipes.page';
import { ProductsPage } from './pages/products.page';

// Tests share state (recipe created in first test, used in subsequent ones)
test.describe.configure({ mode: 'serial' });

test.describe('Recipes CRUD', () => {
  const timestamp = Date.now();
  const ingredientName = `Recipe Ingredient ${timestamp}`;
  const recipeName = `CRUD Recipe ${timestamp}`;

  test.beforeAll(async ({ browser: _browser }) => {
    // Setup: Create a product to use as an ingredient
    // We use a separate context or the first test to set this up,
    // but for simplicity in parallel execution, we'll create it inside the test or use a shared one.
    // Ideally, we'd use the API to seed this, but we'll use the UI in the first test
    // or just assume we can create it.
  });

  test('can create a recipe with ingredients', async ({ page }) => {
    const productsPage = new ProductsPage(page);
    const recipesPage = new RecipesPage(page);

    // 1. Create Ingredient
    await productsPage.goto();
    await productsPage.createProduct({
      name: ingredientName,
      calories: 100,
      protein: 10,
      carbs: 5,
      fat: 2,
    });

    // 2. Create Recipe
    await recipesPage.goto();
    await recipesPage.createRecipe({
      name: recipeName,
      servings: 2,
      prepTime: 15,
      instructions: 'Mix everything.',
      ingredients: [{ name: ingredientName, amount: 100, unit: 'g' }],
    });

    // Verify it appears in the list
    await expect(page.getByText(recipeName)).toBeVisible();
  });

  test('can view recipe details', async ({ page }) => {
    const recipesPage = new RecipesPage(page);
    await recipesPage.goto();
    await recipesPage.searchRecipes(recipeName);

    // Wait for search results and find the specific recipe card
    const card = page
      .locator('[class*="rounded"]')
      .filter({ hasText: recipeName })
      .filter({ has: page.getByRole('link', { name: /view/i }) })
      .first();
    await expect(card).toBeVisible({ timeout: 10000 });

    await card.getByRole('link', { name: /view/i }).click();
    await expect(page.getByText('Nutrition Per Serving')).toBeVisible();
    await expect(page.getByText(ingredientName)).toBeVisible();
  });

  test('can edit a recipe', async ({ page }) => {
    const recipesPage = new RecipesPage(page);
    await recipesPage.goto();
    await recipesPage.searchRecipes(recipeName);

    await recipesPage.editRecipe(recipeName);

    // Update servings
    await page.fill('input[name="servings"]', '4');
    await page.click('button[type="submit"]');

    // Verify update in detail or list
    await page.waitForURL(/\/recipes\/.+/); // detail page
    await expect(page.getByText('4 servings')).toBeVisible();
  });

  test('can delete a recipe', async ({ page }) => {
    const recipesPage = new RecipesPage(page);
    await recipesPage.goto();
    await recipesPage.searchRecipes(recipeName);

    // Wait for debounced search to complete and results to appear
    const card = page
      .locator('[class*="rounded"]')
      .filter({ hasText: recipeName })
      .filter({ has: page.getByRole('link', { name: /view/i }) })
      .first();
    await expect(card).toBeVisible({ timeout: 10000 });

    await recipesPage.deleteRecipe(recipeName);
    // Wait for delete dialog to close, then verify recipe is gone from the list
    await expect(page.getByRole('heading', { name: recipeName })).not.toBeVisible();
  });
});
