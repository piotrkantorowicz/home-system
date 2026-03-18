import { test, expect } from './fixtures/auth.fixture';
import { ImportPage } from './pages/import.page';
import { DashboardPage } from './pages/dashboard.page';
import { DietPlansPage } from './pages/diet-plans.page';
import { ProductsPage } from './pages/products.page';
import { generateWeeklyPlan } from './utils/data-generator';

test.describe.configure({ mode: 'serial', timeout: 120000 });

test.describe('Full Week Workflow: "The Athlete\'s Week"', () => {
  const planData = generateWeeklyPlan(new Date());

  test('executes complete lifecycle: import, verify, interconnectivity, cleanup', async ({
    page,
  }) => {
    const importPage = new ImportPage(page);
    const dashboardPage = new DashboardPage(page);
    const calendarPage = new DietPlansPage(page);
    const productsPage = new ProductsPage(page);

    // --- A. Bulk Ingestion ---
    console.log('Step A: Importing 7-day meals...');
    await importPage.goto();
    await importPage.runImportWizard(planData);
    await page.waitForURL(/\/diet-planner\/calendar/);

    // Go to dashboard to verify product count
    await dashboardPage.goto();
    await page.waitForTimeout(1000);
    expect(await dashboardPage.getProductCount()).toBeGreaterThan(0);

    // --- B. Calendar Precision: Verify ALL 7 days ---
    console.log('Step B: Verifying Calendar (all 7 days)...');
    await calendarPage.goto();

    await expect(page.getByText(/loading/i)).not.toBeVisible({ timeout: 10000 });
    await calendarPage.expectAllDaysRendered();

    const breakfastRecipe = planData.targetRecipe;
    const lunchRecipe = planData.recipeNames[1];

    for (const day of ['Mon', 'Wed', 'Fri', 'Sun']) {
      console.log(`  Checking ${day} for breakfast: ${breakfastRecipe}`);
      await calendarPage.expectMealInDay(day, breakfastRecipe);
    }

    for (const day of ['Tue', 'Thu', 'Sat']) {
      console.log(`  Checking ${day} for lunch: ${lunchRecipe}`);
      await calendarPage.expectMealInDay(day, lunchRecipe);
    }

    const monCol = calendarPage.getDayColumn('Mon').first();
    await expect(monCol.getByText(/1 serving/i).first()).toBeVisible();

    // --- C. Interconnectivity (Product -> Calendar) ---
    console.log('Step C: Updating Product (Ripple Effect)...');
    await productsPage.goto();
    await productsPage.searchProducts(planData.targetProduct);
    await productsPage.editProduct(planData.targetProduct, { calories: 200 });

    await calendarPage.goto();
    await calendarPage.expectMealInDay('Mon', breakfastRecipe);

    // --- D. Constraint Logic ---
    console.log('Step D: Verifying delete product flow...');
    await productsPage.goto();
    await productsPage.searchProducts(planData.targetProduct);

    const productRow = page.getByRole('row', { name: new RegExp(planData.targetProduct) });
    await expect(productRow).toBeVisible({ timeout: 10000 });

    await productRow
      .getByRole('button')
      .filter({ has: page.locator('svg.lucide-trash-2') })
      .click();
    await expect(page.getByText(/delete product/i)).toBeVisible();
    await page.getByRole('button', { name: /^delete$/i }).click();

    await page.waitForTimeout(2000);
    await page.reload();
    await productsPage.searchProducts(planData.targetProduct);
    await expect(page.getByText(/no products found/i)).toBeVisible({ timeout: 10000 });
  });
});
