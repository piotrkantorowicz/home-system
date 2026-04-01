import { test, expect } from './fixtures';

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
