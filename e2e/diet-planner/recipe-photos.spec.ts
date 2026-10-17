import { test, expect } from './fixtures';
import { RecipesPage } from './pages';
import {
  MALFORMED_PHOTO,
  OVERSIZED_PHOTO,
  PHOTO_A,
  PHOTO_B,
  deleteRecipe,
  photoStatus,
  putPhoto,
  seedPhotoRecipe,
} from './utils/recipe-photos';
import { seedProducts } from './utils/seed';

// Requires the stack to run with recipe image storage — see docs/e2e/README.md.
test.describe('Recipe photos', () => {
  const stamp = Date.now();

  test('owner creates a recipe, then uploads, replaces, removes the photo and deletes the recipe', async ({
    page,
  }) => {
    const recipes = new RecipesPage(page);
    const ingredient = `Photo base ${stamp}`;
    const name = `Photo lifecycle ${stamp}`;
    await recipes.goto();
    await seedProducts(page, [ingredient]);
    await recipes.createRecipe({
      name,
      servings: 1,
      ingredients: [{ name: ingredient, amount: 100, unit: 'g' }],
    });
    await recipes.viewRecipe(name);
    await expect(recipes.detailPhoto(name)).toHaveCount(0);
    const id = new URL(page.url()).pathname.split('/').pop() ?? '';

    // Upload: the photo survives a reload and shows as a library thumbnail.
    const first = await recipes.uploadPhoto(PHOTO_A);
    expect(first.status()).toBe(200);
    const { version: firstVersion } = (await first.json()) as { version: string };
    await expect(page.getByText('Photo saved.')).toBeVisible();
    await page.reload();
    await expect(recipes.detailPhoto(name)).toBeVisible();
    await recipes.goto();
    await recipes.searchFor(name);
    await expect(recipes.thumbnailFor(name)).toBeVisible();

    // Replace: a new immutable revision, still shown after a reload.
    await recipes.viewRecipe(name);
    await expect(recipes.photoInput()).toHaveAccessibleName(/replacement/i);
    const second = await recipes.uploadPhoto(PHOTO_B);
    expect(second.status()).toBe(200);
    const { version: secondVersion } = (await second.json()) as { version: string };
    expect(secondVersion).not.toBe(firstVersion);
    await page.reload();
    await expect(recipes.detailPhoto(name)).toBeVisible();

    // Remove: gone from detail, library and the API.
    await recipes.removePhoto();
    await expect(recipes.detailPhoto(name)).toHaveCount(0);
    expect(await photoStatus(page, id)).toBe(404);
    await page.reload();
    await expect(recipes.photoInput()).toHaveAccessibleName(/^choose a photo$/i);
    await recipes.goto();
    await recipes.searchFor(name);
    await expect(recipes.recipeCardFor(name)).toBeVisible();
    await expect(recipes.thumbnailFor(name)).toHaveCount(0);

    // Delete: a recipe with a photo takes its photo with it.
    await recipes.viewRecipe(name);
    expect((await recipes.uploadPhoto(PHOTO_A)).status()).toBe(200);
    await recipes.goto();
    await recipes.deleteRecipe(name);
    await recipes.expectRecipeNotVisible(name);
    expect(await photoStatus(page, id)).toBe(404);
    expect(await photoStatus(page, id, 'thumbnail')).toBe(404);
  });

  test('rejected uploads keep the previous photo and the recipe usable', async ({ page }) => {
    const recipes = new RecipesPage(page);
    await recipes.goto();
    const recipe = await seedPhotoRecipe(page, `Photo rejects ${stamp}`, 'Private');
    try {
      expect(await putPhoto(page, recipe.id)).toBe(200);
      await recipes.gotoRecipe(recipe.id, recipe.name);
      await expect(recipes.detailPhoto(recipe.name)).toBeVisible();

      // Malformed bytes pass the browser's type check; the server refuses them.
      const malformed = await recipes.uploadPhoto(MALFORMED_PHOTO);
      expect(malformed.status()).toBeGreaterThanOrEqual(400);
      expect(malformed.status()).toBeLessThan(500);
      await expect(recipes.photoError()).toHaveText(/could not be accepted/i);
      await expect(recipes.detailPhoto(recipe.name)).toBeVisible();

      // Oversized input never leaves the browser, and the server refuses it on its own too.
      await recipes.photoInput().setInputFiles(OVERSIZED_PHOTO);
      await expect(recipes.photoError()).toHaveText(/could not be accepted/i);
      await expect(page.getByRole('button', { name: /^upload photo$/i })).toHaveCount(0);
      const oversized = await putPhoto(page, recipe.id, OVERSIZED_PHOTO);
      expect([400, 413]).toContain(oversized);

      await page.reload();
      await expect(recipes.detailPhoto(recipe.name)).toBeVisible();
      await expect(page.getByRole('button', { name: 'Add to plan', exact: true })).toBeEnabled();
      expect(await photoStatus(page, recipe.id)).toBe(200);
    } finally {
      await deleteRecipe(page, recipe.id);
    }
  });

  test('a recipe without a photo keeps its compact card and Add to plan', async ({ page }) => {
    const recipes = new RecipesPage(page);
    await recipes.goto();
    const recipe = await seedPhotoRecipe(page, `No photo ${stamp}`, 'Private');
    try {
      await recipes.goto();
      await recipes.searchFor(recipe.name);
      await expect(recipes.recipeCardFor(recipe.name)).toBeVisible();
      await expect(recipes.thumbnailFor(recipe.name)).toHaveCount(0);

      await recipes.viewRecipe(recipe.name);
      await expect(recipes.detailPhoto(recipe.name)).toHaveCount(0);
      await expect(page.getByText(/photo unavailable/i)).toHaveCount(0);
      await page.getByRole('button', { name: 'Add to plan', exact: true }).click();
      await expect(page.getByRole('dialog').getByLabel('Recipe', { exact: true })).toHaveValue(
        recipe.name,
      );
    } finally {
      await deleteRecipe(page, recipe.id);
    }
  });
});
