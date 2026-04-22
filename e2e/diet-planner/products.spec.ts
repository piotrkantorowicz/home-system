import { test, expect } from './fixtures';
import { ProductsPage } from './pages/products.page';

test.describe('Products', () => {
  test('user can create a product with nutrition values', async ({ page }) => {
    const productsPage = new ProductsPage(page);
    await productsPage.goto();

    const productName = `E2E Product ${Date.now()}`;

    await productsPage.createProduct({
      name: productName,
      calories: 150,
      protein: 12,
      carbs: 18,
      fat: 6,
    });

    await expect(page).toHaveURL('/diet-planner/products');
    await productsPage.expectProductVisible(productName);
  });

  test('user can search for products by name', async ({ page }) => {
    const productsPage = new ProductsPage(page);
    await productsPage.goto();

    await productsPage.searchFor('chicken');

    // Either results are visible or an empty state is shown
    const table = page.getByRole('table');
    const emptyMessage = page.getByText(/no products/i);
    await expect(table.or(emptyMessage).first()).toBeVisible({ timeout: 10000 });
  });

  test('product form shows required-field error when name is empty', async ({ page }) => {
    const productsPage = new ProductsPage(page);
    await productsPage.goto();
    await productsPage.createButton.click();

    await page.getByRole('button', { name: /save|create/i }).click();

    await expect(page.getByText(/product name is required/i)).toBeVisible();
  });

  test('user can edit an existing product', async ({ page }) => {
    const productsPage = new ProductsPage(page);
    await productsPage.goto();

    const productName = `Edit Product ${Date.now()}`;

    await productsPage.createProduct({ name: productName, calories: 100, protein: 5, carbs: 10, fat: 3 });
    await productsPage.editProduct(productName, { calories: 250 });

    // After edit, the detail page shows the updated calorie value
    await expect(page.getByText(/250/)).toBeVisible();
  });

  test('user can delete a product and it disappears from the list', async ({ page }) => {
    const productsPage = new ProductsPage(page);
    await productsPage.goto();

    const productName = `Delete Product ${Date.now()}`;
    await productsPage.createProduct({ name: productName, calories: 80, protein: 4, carbs: 8, fat: 2 });

    await productsPage.deleteProduct(productName);

    await productsPage.goto();
    await productsPage.expectProductNotVisible(productName);
  });
});
