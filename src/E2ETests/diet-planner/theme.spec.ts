import { test, expect } from './fixtures';

test.describe('Theme Toggle', () => {
  test('clicking the theme toggle cycles through light and dark modes', async ({ page }) => {
    await page.goto('/');

    const themeToggle = page.getByTestId('theme-toggle');
    await expect(themeToggle).toBeVisible();

    const html = page.locator('html');

    await themeToggle.click();
    await expect(html).toHaveClass(/light|dark/);

    await themeToggle.click();
    await expect(html).toHaveClass(/light|dark/);
  });

  test('selected theme persists after a page reload', async ({ page }) => {
    await page.goto('/');

    const themeToggle = page.getByTestId('theme-toggle');

    // Cycle until dark mode is active
    while (!(await page.locator('html').getAttribute('class'))?.includes('dark')) {
      await themeToggle.click();
    }

    await page.reload();

    await expect(page.locator('html')).toHaveClass(/dark/);
  });
});
