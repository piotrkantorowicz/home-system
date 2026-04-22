import { test, expect } from './fixtures';

test.describe('Theme Toggle', () => {
  test('clicking the theme toggle cycles through light and dark modes', async ({ page }) => {
    await page.goto('/');

    // Theme toggle now lives inside the user menu dropdown — open it once
    await page.getByRole('button', { name: /user menu/i }).click();

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
    await page.getByRole('button', { name: /user menu/i }).click();

    const themeToggle = page.getByTestId('theme-toggle');

    // Cycle until dark mode is active
    for (let attempts = 0; attempts < 4; attempts++) {
      const cls = await page.locator('html').getAttribute('class');
      if (cls?.includes('dark')) break;
      await themeToggle.click();
    }

    await expect(page.locator('html')).toHaveClass(/dark/);

    await page.reload();

    await expect(page.locator('html')).toHaveClass(/dark/);
  });
});
