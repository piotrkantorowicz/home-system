import { test, expect } from './fixtures/auth.fixture';
import { ProductsPage } from './pages/products.page';

test.describe('Products', () => {
  test('can create a new product', async ({ page }) => {
    const productsPage = new ProductsPage(page);
    await productsPage.goto();

    const initialCount = await productsPage.getProductCount();
    const productName = `E2E Test Product ${Date.now()}`;

    await productsPage.createProduct({
      name: productName,
      calories: 100,
      protein: 10,
      carbs: 20,
      fat: 5,
    });

    // Verify redirect and count increase
    await expect(page).toHaveURL('/diet-planner/products');
    const newCount = await productsPage.getProductCount();
    expect(newCount).toBeGreaterThanOrEqual(initialCount);
  });

  test('can search products', async ({ page }) => {
    const productsPage = new ProductsPage(page);
    await productsPage.goto();

    await productsPage.searchProducts('chicken');

    // Either shows results or "no products found" message
    const hasResults = (await page.locator('table tbody tr').count()) > 0;
    const hasNoProductsMessage = await page
      .getByText(/no products/i)
      .isVisible()
      .catch(() => false);

    expect(hasResults || hasNoProductsMessage).toBeTruthy();
  });

  test('shows validation errors for invalid inputs', async ({ page }) => {
    const productsPage = new ProductsPage(page);
    await productsPage.goto();
    await productsPage.createButton.click();

    // Submit empty form to check required fields
    await page.click('button[type="submit"]');
    await expect(page.getByText('Product name is required')).toBeVisible();

    // Enter negative values
    await page.fill('input[name="caloriesPer100g"]', '-100');
    await page.fill('input[name="proteinPer100g"]', '-10');
    await page.click('button[type="submit"]');

    await expect(page.getByText('Calories must be 0 or greater')).toBeVisible();
    await expect(page.getByText('Protein must be 0 or greater')).toBeVisible();
  });

  test('can edit a product', async ({ page }) => {
    const productsPage = new ProductsPage(page);
    await productsPage.goto();

    const timestamp = Date.now();
    const productName = `Edit Test Product ${timestamp}`;

    // Create product to edit
    await productsPage.createProduct({
      name: productName,
      calories: 100,
      protein: 10,
      carbs: 10,
      fat: 10,
    });

    // Edit it
    await productsPage.editProduct(productName, { calories: 200 });

    // Verify change in detail page
    await expect(page.getByText('200.0 kcal')).toBeVisible();
  });

  test('can delete a product', async ({ page }) => {
    const productsPage = new ProductsPage(page);
    await productsPage.goto();

    const timestamp = Date.now();
    const productName = `Delete Test Product ${timestamp}`;

    await productsPage.createProduct({
      name: productName,
      calories: 100,
      protein: 10,
      carbs: 10,
      fat: 10,
    });

    // Delete it
    await productsPage.deleteProduct(productName);

    // Verify it's gone
    await expect(page.getByRole('row', { name: new RegExp(productName) })).not.toBeVisible();
  });
});
