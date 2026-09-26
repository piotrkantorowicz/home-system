import { test, expect } from './fixtures';
import { ProductsPage } from './pages';
import { seedProducts } from './utils/seed';

const PAGE_SIZE_OPTIONS = ['10', '25', '50', '100'];

test.describe('Pagination — Products list', () => {
  test('page size selector is visible with the correct options', async ({ page }) => {
    await page.goto('/diet-planner/products');

    const select = page.getByRole('combobox');
    await expect(select).toBeVisible({ timeout: 8000 });

    const options = await select.locator('option').allTextContents();
    expect(options).toEqual(PAGE_SIZE_OPTIONS);
  });

  test('page size selector defaults to 25', async ({ page }) => {
    await page.goto('/diet-planner/products');

    const select = page.getByRole('combobox');
    await expect(select).toBeVisible({ timeout: 8000 });
    await expect(select).toHaveValue('25');
  });

  test('changing page size triggers a new products API request', async ({ page }) => {
    await page.goto('/diet-planner/products');

    const select = page.getByRole('combobox');
    await expect(select).toBeVisible({ timeout: 8000 });

    const responsePromise = page.waitForResponse(
      (resp) => resp.url().includes('/api/v1/products') && resp.request().method() === 'GET',
      { timeout: 8000 },
    );

    await select.selectOption('10');
    await responsePromise;

    await expect(select).toHaveValue('10');
  });

  test('Previous button is disabled when on the first page', async ({ page }) => {
    await page.goto('/diet-planner/products');

    const prevButton = page.getByRole('button', { name: /previous/i });
    await expect(prevButton).toBeVisible({ timeout: 8000 });
    await expect(prevButton).toBeDisabled();
  });

  test('Next/Previous navigate a search-scoped result set with correct bounds', async ({
    page,
  }) => {
    // A shared prefix isolates this batch from whatever else the household's
    // products list already holds — pagination bounds below are exact, not
    // "at least N", because the search scopes the list to exactly these 12.
    const productsPage = new ProductsPage(page);
    // A fresh context starts on `about:blank`, an opaque origin that throws on
    // any storage access — load a same-origin page before the API seed below
    // reads the access token out of localStorage.
    await productsPage.goto();

    const prefix = `E2EPager${Date.now()}`;
    const names = Array.from(
      { length: 12 },
      (_, i) => `${prefix} ${String(i + 1).padStart(2, '0')}`,
    );
    await seedProducts(page, names);

    await productsPage.goto();
    await productsPage.searchFor(prefix);
    await productsPage.setPageSize(10);

    await expect(productsPage.rowFor(names[0]!)).toBeVisible();
    await expect(productsPage.rowFor(names[9]!)).toBeVisible();
    await expect(productsPage.rowFor(names[10]!)).not.toBeVisible();
    await expect(productsPage.previousButton).toBeDisabled();
    await expect(productsPage.nextButton).toBeEnabled();

    await productsPage.goToNextPage();

    await expect(productsPage.rowFor(names[10]!)).toBeVisible();
    await expect(productsPage.rowFor(names[11]!)).toBeVisible();
    await expect(productsPage.rowFor(names[0]!)).not.toBeVisible();
    await expect(productsPage.nextButton).toBeDisabled();
    await expect(productsPage.previousButton).toBeEnabled();

    await productsPage.goToPreviousPage();

    await expect(productsPage.rowFor(names[0]!)).toBeVisible();
    await expect(productsPage.rowFor(names[10]!)).not.toBeVisible();
    await expect(productsPage.previousButton).toBeDisabled();
  });

  test('search and page size survive a reload via the URL, and clearing search widens the results', async ({
    page,
  }) => {
    const productsPage = new ProductsPage(page);
    await productsPage.goto();

    const prefix = `E2ESearch${Date.now()}`;
    const names = [`${prefix} A`, `${prefix} B`, `${prefix} C`];
    await seedProducts(page, names);

    await productsPage.goto();
    await productsPage.searchFor(prefix);
    await productsPage.setPageSize(10);

    await expect(page).toHaveURL(new RegExp(`search=${encodeURIComponent(prefix)}`));
    await expect(page).toHaveURL(/pageSize=10/);
    await expect(page.getByText('3 items', { exact: true })).toBeVisible();

    await page.reload();

    await expect(productsPage.searchInput).toHaveValue(prefix);
    await expect(productsPage.pageSizeSelect).toHaveValue('10');
    await expect(page.getByText('3 items', { exact: true })).toBeVisible();
    for (const name of names) {
      await expect(productsPage.rowFor(name)).toBeVisible();
    }

    await productsPage.clearSearch();

    await expect(page).not.toHaveURL(/search=/);
    const countText = await page.getByText(/^\d+ items$/).textContent();
    const count = Number(countText?.match(/\d+/)?.[0] ?? 0);
    expect(count).toBeGreaterThan(names.length);
  });
});

test.describe('Pagination — Recipes list', () => {
  test('page size selector is visible with the correct options', async ({ page }) => {
    await page.goto('/diet-planner/recipes');

    const select = page.getByRole('combobox');
    await expect(select).toBeVisible({ timeout: 8000 });

    const options = await select.locator('option').allTextContents();
    expect(options).toEqual(PAGE_SIZE_OPTIONS);
  });

  test('page size selector defaults to 25', async ({ page }) => {
    await page.goto('/diet-planner/recipes');

    const select = page.getByRole('combobox');
    await expect(select).toBeVisible({ timeout: 8000 });
    await expect(select).toHaveValue('25');
  });

  test('changing page size triggers a new recipes API request', async ({ page }) => {
    await page.goto('/diet-planner/recipes');

    const select = page.getByRole('combobox');
    await expect(select).toBeVisible({ timeout: 8000 });

    const responsePromise = page.waitForResponse(
      (resp) => resp.url().includes('/api/v1/recipes') && resp.request().method() === 'GET',
      { timeout: 8000 },
    );

    await select.selectOption('10');
    await responsePromise;

    await expect(select).toHaveValue('10');
  });

  test('Previous button is disabled when on the first page', async ({ page }) => {
    await page.goto('/diet-planner/recipes');

    const prevButton = page.getByRole('button', { name: /previous/i });
    await expect(prevButton).toBeVisible({ timeout: 8000 });
    await expect(prevButton).toBeDisabled();
  });
});
