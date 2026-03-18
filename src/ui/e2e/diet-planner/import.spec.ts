import { test, expect } from './fixtures/auth.fixture';
import { ImportPage } from './pages/import.page';

test.describe.configure({ mode: 'serial', timeout: 120000 });

test.describe('Diet Plan Import', () => {
  test('can load sample JSON', async ({ page }) => {
    const importPage = new ImportPage(page);
    await importPage.goto();

    await importPage.loadSample();

    const jsonContent = await importPage.jsonInput.inputValue();
    expect(jsonContent).toContain('products');
    expect(jsonContent).toContain('recipes');
  });

  test('shows validation errors for invalid JSON', async ({ page }) => {
    const importPage = new ImportPage(page);
    await importPage.goto();

    await importPage.setJson('{ invalid json }');
    await importPage.continueButton.click();

    // Should show error alert
    await expect(page.getByText(/invalid json/i)).toBeVisible();
  });

  test('full import wizard flow with unique data', async ({ page }) => {
    const importPage = new ImportPage(page);
    const timestamp = Date.now();

    // Use custom data with unique names to avoid conflicts
    const uniqueImportData = {
      products: [
        {
          name: `E2E Product ${timestamp}`,
          caloriesPer100g: 165,
          proteinPer100g: 31,
          carbsPer100g: 0,
          fatPer100g: 3.6,
          unit: 'g',
        },
      ],
      recipes: [
        {
          name: `E2E Recipe ${timestamp}`,
          description: 'Test recipe for E2E',
          servings: 2,
          prepTimeMinutes: 30,
          ingredients: [{ product: `E2E Product ${timestamp}`, amount: 300, unit: 'g' }],
          instructions: '1. Cook it...',
        },
      ],
      schedule: [
        {
          date: new Date().toISOString().slice(0, 10),
          meals: [{ type: 'lunch', recipe: `E2E Recipe ${timestamp}`, servings: 1 }],
        },
      ],
    };

    await importPage.goto();
    await importPage.runImportWizard(uniqueImportData);

    // Should redirect to calendar
    await page.waitForURL(/\/diet-planner\/calendar/);
  });

  test('validates before import', async ({ page }) => {
    const importPage = new ImportPage(page);
    const timestamp = Date.now();

    await importPage.goto();

    // Use custom data with unique names to avoid conflicts
    const uniqueImportData = {
      products: [
        {
          name: `E2E Test Chicken ${timestamp}`,
          caloriesPer100g: 165,
          proteinPer100g: 31,
          carbsPer100g: 0,
          fatPer100g: 3.6,
          unit: 'g',
        },
      ],
      recipes: [
        {
          name: `E2E Grilled Chicken ${timestamp}`,
          description: 'Test recipe',
          servings: 2,
          prepTimeMinutes: 30,
          ingredients: [{ product: `E2E Test Chicken ${timestamp}`, amount: 300, unit: 'g' }],
          instructions: '1. Grill chicken...',
        },
      ],
      schedule: [
        {
          date: new Date().toISOString().slice(0, 10),
          meals: [{ type: 'lunch', recipe: `E2E Grilled Chicken ${timestamp}`, servings: 1 }],
        },
      ],
    };

    await importPage.setJson(uniqueImportData);
    await importPage.continueButton.click();

    // Should be on step 2
    await expect(page.getByText('Step 2: Validate Import Data')).toBeVisible();

    await importPage.validateButton.click();

    // Should advance to step 3 with validation success
    await expect(page.getByText('Step 3: Review & Confirm')).toBeVisible({ timeout: 15000 });
    await expect(page.getByText(/validation successful/i)).toBeVisible();
  });
});
