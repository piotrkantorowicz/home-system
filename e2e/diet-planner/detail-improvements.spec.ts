import { test } from './fixtures';
import { ProductsPage, RecipesPage, MealSchedulePage } from './pages';
import { DetailReviewPage } from './pages/detail-review.page';
import { seedMealSchedule } from './utils/seed';

test('product nutrition, recipe scaling, planning, and Today navigation', async ({
  page,
}, testInfo) => {
  test.setTimeout(120000);
  const name = `Review oats ${Date.now()}`;
  const recipeName = `Review bowl ${Date.now()}`;
  const products = new ProductsPage(page);
  const recipes = new RecipesPage(page);
  const detail = new DetailReviewPage(page);
  const schedule = new MealSchedulePage(page);
  await schedule.goto();
  // API upsert, not the form: a worker another spec already seeded leaves the form pristine.
  await seedMealSchedule(page);
  await products.goto();
  await products.createProduct({
    name,
    calories: 100,
    protein: 10,
    carbs: 5,
    fat: 2,
  });
  await products.expectProductVisible(name);
  await detail.openProduct(name);
  await detail.captureResponsive((file) => testInfo.outputPath(file), 'product');
  await recipes.goto();
  await recipes.createRecipe({
    name: recipeName,
    servings: 2,
    ingredients: [{ name, amount: 100, unit: 'g' }],
    instructions: 'Mix ingredients.\nServe in a bowl.',
  });
  await recipes.viewRecipe(recipeName);
  await detail.scaleRecipe();
  await detail.captureResponsive((file) => testInfo.outputPath(file), 'recipe');
  await detail.planRecipe(recipeName);
  await detail.jumpToToday();
});
