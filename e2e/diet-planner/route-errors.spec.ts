import { expect, test } from './fixtures';

test('an unknown page shows a friendly message inside the shell, not the router developer page', async ({
  page,
}) => {
  await page.goto('/diet-planner/does-not-exist');

  const alert = page.getByRole('alert');
  await expect(alert).toBeVisible();
  await expect(alert.getByRole('heading')).toHaveText(/page not found/i);
  await expect(page.getByText('Hey developer')).toHaveCount(0);
  // No retry for a page that does not exist; the way back is always offered.
  await expect(alert.getByRole('button', { name: /try again/i })).toHaveCount(0);

  await alert.getByRole('link', { name: /go to start/i }).click();
  await expect(page.getByRole('alert')).toHaveCount(0);
});
