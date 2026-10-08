import { expect, test } from './fixtures';
import { createApiContext } from './utils/seed';

import type { Page } from '@playwright/test';

/** A route plus, where it loads data, text that only appears once the data has rendered. */
interface AlignedRoute {
  path: string;
  ready?: string | RegExp;
}

// Every page body sits in the shared PageContainer: one title origin, no page-level overflow (#554).
const STATIC_ROUTES: AlignedRoute[] = [
  '/diet-planner',
  '/diet-planner/profile',
  '/diet-planner/products/new',
  '/diet-planner/recipes/new',
  '/diet-planner/calendar',
  '/diet-planner/shopping-list',
  '/diet-planner/hydration',
  '/diet-planner/nutrition',
  '/budget',
  '/household',
  '/notifications',
  '/settings/app',
].map((path) => ({ path }));

/** Seeds a product and a recipe, then returns the populated list, detail and edit routes for both. */
async function seedLibraryRoutes(page: Page): Promise<AlignedRoute[]> {
  const product = `Align oats ${String(Date.now())}`;
  const recipe = `Align bowl ${String(Date.now())}`;
  const api = await createApiContext(page);
  try {
    const productRes = await api.post('/api/v1/products', {
      data: { name: product, calories: 100, protein: 5, carbs: 10, fat: 2, defaultUnit: 'g' },
    });
    expect(productRes.ok(), `seed product: ${String(productRes.status())}`).toBe(true);
    const productId = (await productRes.json()) as string;
    const recipeRes = await api.post('/api/v1/recipes', {
      data: { name: recipe, servings: 1, ingredients: [{ productId, amount: 100, unit: 'g' }] },
    });
    expect(recipeRes.ok(), `seed recipe: ${String(recipeRes.status())}`).toBe(true);
    const recipeId = (await recipeRes.json()) as string;
    return [
      { path: `/diet-planner/products?search=${encodeURIComponent(product)}`, ready: product },
      { path: `/diet-planner/products/${productId}`, ready: product },
      { path: `/diet-planner/products/${productId}/edit`, ready: product },
      { path: `/diet-planner/recipes?search=${encodeURIComponent(recipe)}`, ready: recipe },
      { path: `/diet-planner/recipes/${recipeId}`, ready: recipe },
      { path: `/diet-planner/recipes/${recipeId}/edit`, ready: recipe },
    ];
  } finally {
    await api.dispose();
  }
}

/** Opens the route, waits for its content (not loading skeletons), and returns the title origin. */
async function titleOrigin(page: Page, { path, ready }: AlignedRoute): Promise<string> {
  await page.goto(path);
  const main = page.getByRole('main');
  if (ready) await expect(main.getByText(ready).first()).toBeVisible();
  // A fresh worker may still see Budget's setup gate (h2); it renders inside the same container.
  const heading = main.getByRole('heading').first();
  await expect(heading).toBeVisible();
  const geometry = await heading.evaluate((el) => {
    for (const a of document.getAnimations()) a.finish();
    const box = el.getBoundingClientRect();
    return {
      origin: `${String(Math.round(box.left))},${String(Math.round(box.top))}`,
      overflow: document.documentElement.scrollWidth > window.innerWidth,
    };
  });
  expect(geometry.overflow, `${path} overflows horizontally`).toBe(false);
  return geometry.origin;
}

for (const width of [390, 768, 1440, 1920, 2560]) {
  test(`page titles share one origin at ${String(width)}px`, async ({ page }) => {
    test.setTimeout(120_000);
    await page.goto('/diet-planner');
    const routes = [...STATIC_ROUTES, ...(await seedLibraryRoutes(page))];
    await page.setViewportSize({ width, height: 900 });
    const origins: string[] = [];
    for (const route of routes) origins.push(`${route.path} @ ${await titleOrigin(page, route)}`);
    const origin = origins[0]?.split(' @ ')[1] ?? '';
    expect(origins).toEqual(routes.map((route) => `${route.path} @ ${origin}`));
  });
}

test('empty and error list states keep the page origin', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  const expected = await titleOrigin(page, { path: '/diet-planner' });
  for (const [path, api, empty, error] of [
    [
      '/diet-planner/products',
      '**/api/v1/products?*',
      /no products found/i,
      /failed to fetch products/i,
    ],
    [
      '/diet-planner/recipes',
      '**/api/v1/recipes?*',
      /no recipes found/i,
      /failed to fetch recipes/i,
    ],
  ] as const) {
    // The real backend cannot produce an empty library or a failure on demand.
    await page.route(api, (r) =>
      r.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 } }),
    );
    expect(await titleOrigin(page, { path, ready: empty }), `${path} empty`).toBe(expected);
    await page.unroute(api);

    // A 4xx skips the query client's retries, so the error banner renders straight away.
    await page.route(api, (r) => r.fulfill({ status: 404, json: {} }));
    expect(await titleOrigin(page, { path, ready: error }), `${path} error`).toBe(expected);
    await page.unroute(api);
  }
});
