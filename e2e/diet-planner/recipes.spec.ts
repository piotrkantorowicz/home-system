import { test, expect } from './fixtures';
import { ProductsPage, RecipesPage } from './pages';

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
    await productsPage.createProduct({
      name: ingredientName,
      calories: 100,
      protein: 10,
      carbs: 5,
      fat: 2,
    });

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
    await recipesPage.viewRecipe(recipeName);

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
    await expect(page.getByText(/4 servings$/i)).toBeVisible();
  });

  test('user can delete a recipe and it disappears from the list', async ({ page }) => {
    const recipesPage = new RecipesPage(page);
    await recipesPage.goto();
    await recipesPage.searchFor(recipeName);

    await recipesPage.deleteRecipe(recipeName);

    await recipesPage.expectRecipeNotVisible(recipeName);
  });

  test('mixed-unit ingredients, instructions, and per-serving nutrition survive edits', async ({
    page,
  }) => {
    const productsPage = new ProductsPage(page);
    const recipesPage = new RecipesPage(page);
    const liquidName = `Liquid Ingredient ${timestamp}`;
    const detailName = `Detail Recipe ${timestamp}`;

    await productsPage.goto();
    await productsPage.createProduct({
      name: liquidName,
      unit: 'ml',
      density: 1,
      calories: 50,
      protein: 2,
      carbs: 10,
      fat: 0,
      fiber: 7.5,
    });
    await productsPage.viewProduct(liquidName);
    await page.reload();
    await expect(page.getByRole('heading', { name: liquidName, level: 1 })).toBeVisible();
    await expect(page.getByText('ml', { exact: true }).first()).toBeVisible();
    await expect(page.getByText('7.5 g', { exact: true })).toBeVisible();

    await recipesPage.goto();
    await recipesPage.createRecipe({
      name: detailName,
      servings: 2,
      ingredients: [
        { name: ingredientName, amount: 100, unit: 'g' },
        { name: liquidName, amount: 200, unit: 'ml' },
      ],
      instructions: 'Mix grains. Add liquid.',
    });
    await recipesPage.viewRecipe(detailName);
    await page.reload();
    await expect(page.getByText(ingredientName, { exact: true })).toBeVisible();
    await expect(page.getByText(liquidName, { exact: true })).toBeVisible();
    await expect(page.getByText('100.0 g', { exact: true })).toBeVisible();
    await expect(page.getByText('200.0 ml', { exact: true })).toBeVisible();
    await expect(page.getByText('Mix grains.')).toBeVisible();
    await expect(page.getByText('Add liquid.')).toBeVisible();
    await expect(page.getByText('100kcal', { exact: true })).toBeVisible();

    await page.getByRole('link', { name: /^edit$/i }).click();
    await page.waitForURL(/\/edit$/);
    await page.getByRole('spinbutton', { name: /servings/i }).fill('4');
    await recipesPage.updateIngredientAmount(ingredientName, 200);
    await page.getByRole('button', { name: /save|update/i }).click();
    await page.waitForURL(/\/diet-planner\/recipes\/[a-z0-9-]+$/);
    await page.reload();
    // The visibility badge shares the line ("Household 4 servings").
    await expect(page.getByText(/4 servings$/i)).toBeVisible();
    await expect(page.getByText('200.0 g', { exact: true })).toBeVisible();
    await expect(page.getByText('200.0 ml', { exact: true })).toBeVisible();
    await expect(page.getByText('Mix grains.')).toBeVisible();
    await expect(page.getByText('Add liquid.')).toBeVisible();
    await expect(page.getByText('75kcal', { exact: true })).toBeVisible();
  });
});
