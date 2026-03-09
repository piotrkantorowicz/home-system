import { test, expect } from './fixtures/auth.fixture';
import { ImportPage } from './pages/import.page';
import { DashboardPage } from './pages/dashboard.page';
import { DietPlansPage } from './pages/diet-plans.page';
import { ProductsPage } from './pages/products.page';
import { generateWeeklyPlan } from './utils/data-generator';

test.describe.configure({ mode: 'serial', timeout: 120000 });

test.describe('Full Week Workflow: "The Athlete\'s Week"', () => {
  const planData = generateWeeklyPlan(`Athlete Plan ${Date.now()}`, new Date());

  test('executes complete lifecycle: import, verify, interconnectivity, cleanup', async ({
    page,
  }) => {
    // Page Objects
    const importPage = new ImportPage(page);
    const dashboardPage = new DashboardPage(page);
    const dietPlansPage = new DietPlansPage(page);
    const productsPage = new ProductsPage(page);

    // --- A. Bulk Ingestion ---
    console.log('Step A: Importing 7-day plan...');
    await importPage.goto();
    await importPage.runImportWizard(planData);

    // Redirects to diet plans, go to dashboard to verify counts
    await dashboardPage.goto();
    await page.waitForTimeout(1000);

    expect(await dashboardPage.getProductCount()).toBeGreaterThan(0);
    expect(await dashboardPage.getDietPlanCount()).toBeGreaterThan(0);

    // --- B. Calendar Precision: Verify ALL 7 days ---
    console.log('Step B: Verifying Calendar (all 7 days)...');
    await dietPlansPage.goto();
    await dietPlansPage.openPlan(planData.planName);

    // Verify plan header
    await expect(page.getByText(planData.planName)).toBeVisible({ timeout: 10000 });

    // Wait for meals to load
    await expect(page.getByText(/loading/i)).not.toBeVisible({ timeout: 10000 });

    // All 7 day columns must be rendered
    await dietPlansPage.expectAllDaysRendered();

    // Data generator schedule:
    // i % 2 === 0 → Mon(0), Wed(2), Fri(4), Sun(6) get breakfast (Morning Bowl = targetRecipe)
    // i % 2 === 1 → Tue(1), Thu(3), Sat(5) get lunch (Chicken Rice = recipeNames[1])
    const breakfastRecipe = planData.targetRecipe;
    const lunchRecipe = planData.recipeNames[1];

    // Verify breakfast days (Mon, Wed, Fri, Sun)
    for (const day of ['Mon', 'Wed', 'Fri', 'Sun']) {
      console.log(`  Checking ${day} for breakfast: ${breakfastRecipe}`);
      await dietPlansPage.expectMealInDay(day, breakfastRecipe);
    }

    // Verify lunch days (Tue, Thu, Sat)
    for (const day of ['Tue', 'Thu', 'Sat']) {
      console.log(`  Checking ${day} for lunch: ${lunchRecipe}`);
      await dietPlansPage.expectMealInDay(day, lunchRecipe);
    }

    // Verify servings display correctly (no duplicate number)
    const monCol = dietPlansPage.getDayColumn('Mon').first();
    await expect(monCol.getByText(/1 serving/i)).toBeVisible();

    // --- C. Interconnectivity (Product -> Plan) ---
    console.log('Step C: Updating Product (Ripple Effect)...');
    await productsPage.goto();
    await productsPage.searchProducts(planData.targetProduct);
    await productsPage.editProduct(planData.targetProduct, { calories: 200 });

    // Return to Plan and verify meal is still there
    await dietPlansPage.goto();
    await dietPlansPage.openPlan(planData.planName);

    await dietPlansPage.expectMealInDay('Mon', breakfastRecipe);

    // --- D. Constraint Logic ---
    console.log('Step D: Verifying delete product flow...');
    await productsPage.goto();
    await productsPage.searchProducts(planData.targetProduct);

    const productRow = page.getByRole('row', { name: new RegExp(planData.targetProduct) });
    await expect(productRow).toBeVisible({ timeout: 10000 });

    // Delete the product via UI
    await productRow
      .getByRole('button')
      .filter({ has: page.locator('svg.lucide-trash-2') })
      .click();
    await expect(page.getByText(/delete product/i)).toBeVisible();
    await page.getByRole('button', { name: /^delete$/i }).click();

    await page.waitForTimeout(2000);

    // Verify product is deleted
    await page.reload();
    await productsPage.searchProducts(planData.targetProduct);
    await expect(page.getByText(/no products found/i)).toBeVisible({ timeout: 10000 });

    // --- E. User-Driven Teardown ---
    console.log('Step E: Deleting Plan...');
    await dietPlansPage.goto();
    await dietPlansPage.deletePlan(planData.planName);

    await expect(page.getByRole('heading', { name: planData.planName })).not.toBeVisible();
  });
});
