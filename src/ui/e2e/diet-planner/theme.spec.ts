import { test, expect } from './fixtures/auth.fixture';

test.describe('Theme Toggle', () => {
  test('can switch between themes', async ({ page }) => {
    await page.goto('/');

    const themeToggle = page.getByTestId('theme-toggle');
    await expect(themeToggle).toBeVisible();

    // Check initial state (could be light, dark, or system)
    const html = page.locator('html');

    // Click to cycle through themes
    await themeToggle.click();
    await expect(html).toHaveClass(/light|dark/);

    await themeToggle.click();
    await expect(html).toHaveClass(/light|dark/);

    await themeToggle.click();
    await expect(html).toHaveClass(/light|dark/);
  });

  test('persists theme across page reload', async ({ page }) => {
    await page.goto('/');

    const themeToggle = page.getByTestId('theme-toggle');

    // Set to dark
    while (!(await page.locator('html').getAttribute('class'))?.includes('dark')) {
      await themeToggle.click();
    }

    // Reload
    await page.reload();

    // Should still be dark
    await expect(page.locator('html')).toHaveClass(/dark/);
  });
});
