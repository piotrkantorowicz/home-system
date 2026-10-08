import { expect, test } from './fixtures';

// Every page body sits in the shared PageContainer: one title origin, no page-level overflow (#554).
const ROUTES = [
  '/diet-planner',
  '/diet-planner/profile',
  '/diet-planner/products',
  '/diet-planner/products/new',
  '/diet-planner/recipes',
  '/diet-planner/recipes/new',
  '/diet-planner/calendar',
  '/diet-planner/shopping-list',
  '/diet-planner/hydration',
  '/diet-planner/nutrition',
  '/budget',
  '/household',
  '/notifications',
  '/settings/app',
];

for (const width of [390, 768, 1440, 1920, 2560]) {
  test(`page titles share one origin at ${String(width)}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 900 });
    const origins: string[] = [];
    for (const route of ROUTES) {
      await page.goto(route);
      // A fresh worker may still see Budget's setup gate (h2); it renders inside the same container.
      const heading = page.getByRole('main').getByRole('heading').first();
      await expect(heading).toBeVisible();
      const geometry = await heading.evaluate((el) => {
        for (const a of document.getAnimations()) a.finish();
        const box = el.getBoundingClientRect();
        return {
          origin: `${String(Math.round(box.left))},${String(Math.round(box.top))}`,
          overflow: document.documentElement.scrollWidth > window.innerWidth,
        };
      });
      expect(geometry.overflow, `${route} overflows horizontally`).toBe(false);
      origins.push(`${route} @ ${geometry.origin}`);
    }
    const origin = origins[0]?.split(' @ ')[1] ?? '';
    expect(origins).toEqual(ROUTES.map((route) => `${route} @ ${origin}`));
  });
}
