import { test, expect } from './fixtures';
import { ProductsPage } from './pages/products.page';
import { RecipesPage } from './pages/recipes.page';

// Serial because later tests depend on data created in earlier ones
test.describe.configure({ mode: 'serial' });

test.describe('Recipes CRUD', () => {
  const timestamp = Date.now();
  const ingredientName = `Ingredient ${timestamp}`;
  const recipeName = `Recipe ${timestamp}`;

  test('user can create a recipe with an ingredient', async ({ page }) => {
    const productsPage = new ProductsPage(page);
    const recipesPage = new RecipesPage(page);

    // Given an ingredient product exists
    await productsPage.goto();
    await productsPage.createProduct({ name: ingredientName, calories: 100, protein: 10, carbs: 5, fat: 2 });

    // When the user creates a recipe referencing that ingredient
    await recipesPage.goto();
    await recipesPage.createRecipe({
      name: recipeName,
      servings: 2,
      prepTime: 15,
      ingredients: [{ name: ingredientName, amount: 100, unit: 'g' }],
    });

    // Then the recipe appears in the list
    await recipesPage.expectRecipeVisible(recipeName);
  });

  test('user can view recipe details including the ingredient', async ({ page }) => {
    const recipesPage = new RecipesPage(page);
    await recipesPage.goto();
    await recipesPage.searchFor(recipeName);

    // Find the View link within the recipe card and navigate
    const card = recipesPage.recipeCardFor(recipeName);
    const viewLink = card.getByRole('link', { name: /view/i }).first();
    await expect(viewLink).toBeVisible({ timeout: 10000 });
    await viewLink.click();

    await expect(page.getByText(/nutrition per serving/i)).toBeVisible();
    await expect(page.getByText(ingredientName)).toBeVisible();
  });

  test('user can edit a recipe to update the number of servings', async ({ page }) => {
    const recipesPage = new RecipesPage(page);
    await recipesPage.goto();
    await recipesPage.searchFor(recipeName);

    await recipesPage.editRecipe(recipeName);

    await page.getByLabel(/servings/i).fill('4');
    await page.getByRole('button', { name: /save|update/i }).click();

    // Redirects to detail page
    await page.waitForURL(/\/diet-planner\/recipes\/.+/);
    await expect(page.getByText(/4 serving/i)).toBeVisible();
  });

  test('user can delete a recipe and it disappears from the list', async ({ page }) => {
    const recipesPage = new RecipesPage(page);
    await recipesPage.goto();
    await recipesPage.searchFor(recipeName);

    await recipesPage.deleteRecipe(recipeName);

    await recipesPage.expectRecipeNotVisible(recipeName);
  });
});
