import { format, addDays, startOfWeek } from 'date-fns';

import { test, expect } from './fixtures';
import { ImportPage } from './pages';

test.describe.configure({ mode: 'serial', timeout: 120000 });

test.describe('Diet Plan Import', () => {
  test('user can load sample JSON and it populates the input', async ({ page }) => {
    const importPage = new ImportPage(page);
    await importPage.goto();

    await importPage.loadSample();

    const jsonContent = await importPage.jsonInput.inputValue();
    expect(jsonContent).toContain('products');
    expect(jsonContent).toContain('recipes');
  });

  test('entering invalid JSON blocks progression to step 2', async ({ page }) => {
    const importPage = new ImportPage(page);
    await importPage.goto();

    await importPage.setJson('{ this is not valid json }');
    await importPage.continueButton.click();

    await expect(page.getByText(/invalid json/i)).toBeVisible();
  });

  test('continuing past step 1 auto-validates and shows a ready-to-import summary', async ({
    page,
  }) => {
    const importPage = new ImportPage(page);
    const ts = Date.now();

    await importPage.goto();

    const data = {
      products: [
        {
          name: `Validate Product ${String(ts)}`,
          caloriesPer100g: 165,
          proteinPer100g: 31,
          carbsPer100g: 0,
          fatPer100g: 3.6,
          unit: 'g',
        },
      ],
      recipes: [
        {
          name: `Validate Recipe ${String(ts)}`,
          servings: 2,
          prepTimeMinutes: 20,
          ingredients: [{ product: `Validate Product ${String(ts)}`, amount: 200, unit: 'g' }],
          instructions: 'Mix and serve.',
        },
      ],
      schedule: [
        {
          date: new Date().toISOString().slice(0, 10),
          meals: [{ type: 'lunch', recipe: `Validate Recipe ${String(ts)}`, servings: 1 }],
        },
      ],
    };

    await importPage.setJson(data);

    const validatePromise = page.waitForResponse(
      (resp) => resp.url().includes('/meals/validate') && resp.request().method() === 'POST',
    );
    await importPage.continueButton.click();
    await validatePromise;

    await expect(importPage.reviewDetected).toBeVisible({ timeout: 10000 });
    await expect(page.getByText(/ready to import/i)).toBeVisible();
    await expect(importPage.importButton).toBeEnabled();
  });

  test('full import wizard creates products, recipes and calendar entries', async ({ page }) => {
    const importPage = new ImportPage(page);
    const ts = Date.now();

    const data = {
      products: [
        {
          name: `Import Product ${String(ts)}`,
          caloriesPer100g: 200,
          proteinPer100g: 20,
          carbsPer100g: 10,
          fatPer100g: 5,
          unit: 'g',
        },
      ],
      recipes: [
        {
          name: `Import Recipe ${String(ts)}`,
          servings: 2,
          prepTimeMinutes: 30,
          ingredients: [{ product: `Import Product ${String(ts)}`, amount: 300, unit: 'g' }],
          instructions: 'Cook and serve.',
        },
      ],
      schedule: [
        {
          date: new Date().toISOString().slice(0, 10),
          meals: [{ type: 'dinner', recipe: `Import Recipe ${String(ts)}`, servings: 1 }],
        },
      ],
    };

    await importPage.goto();
    await importPage.runImportWizard(data);

    await expect(page).toHaveURL(/\/diet-planner\/calendar/);
  });

  test('imported weekly plan meals are visible on the calendar', async ({ page }) => {
    const importPage = new ImportPage(page);
    const ts = Date.now();

    const monday = startOfWeek(new Date(), { weekStartsOn: 1 });
    const mealTypes = ['breakfast', 'lunch', 'dinner', 'snack'];

    // One product and one recipe per meal type
    const products = mealTypes.map((type) => ({
      name: `${type} Prod ${String(ts)}`,
      caloriesPer100g: 100,
      proteinPer100g: 10,
      carbsPer100g: 20,
      fatPer100g: 5,
      unit: 'g',
    }));

    const recipes = mealTypes.map((type, i) => ({
      name: `${type} Recipe ${String(ts)}`,
      servings: 1,
      ingredients: [{ product: products[i].name, amount: 100, unit: 'g' }],
    }));

    // Schedule every day of the week — each day gets all 4 meal types
    const schedule = Array.from({ length: 7 }, (_, i) => ({
      date: format(addDays(monday, i), 'yyyy-MM-dd'),
      meals: mealTypes.map((type, j) => ({
        type,
        recipe: recipes[j].name,
        servings: 1,
      })),
    }));

    // Given the plan is imported
    await importPage.goto();
    await importPage.runImportWizard({ products, recipes, schedule });
    await expect(page).toHaveURL(/\/diet-planner\/calendar/);

    // Wait for the calendar to load meal data
    await page.waitForLoadState('networkidle');

    // Each of the 4 recipes should appear exactly 7 times (once per day of the
    // week). Meal chips are <button>s whose accessible name is
    // "<recipe name> <kcal> kcal", so a substring match on the name works.
    for (const recipe of recipes) {
      await expect(page.getByRole('button', { name: recipe.name }).first()).toBeVisible({
        timeout: 10000,
      });
      await expect(page.getByRole('button', { name: recipe.name })).toHaveCount(7);
    }

    // All 7 weekday column headers should be visible
    const weekdays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
    for (const day of weekdays) {
      await expect(page.getByRole('columnheader', { name: day })).toBeVisible();
    }
  });
});
