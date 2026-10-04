import { test, expect } from './fixtures';

// The #208 redesign dropped the "console" channel row entirely — App &
// account now shows only Email (disabled, "coming soon") and Websocket (the one
// interactive toggle). If a console channel comes back, these tests will
// need a third row again. See docs/e2e/notification-preferences.md.
test.describe('Notifications channel preferences', () => {
  test('renders the email (disabled) and websocket (enabled) channel rows', async ({ page }) => {
    await page.route('**/api/notification-preferences', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          consoleEnabled: true,
          emailEnabled: false,
          webSocketEnabled: false,
        }),
      }),
    );

    await page.goto('/notifications/preferences');

    // The legacy route redirects to App & account, which now hosts the channels.
    await expect(page).toHaveURL(/\/diet-planner\/settings\/app/);
    await expect(page.getByRole('heading', { name: /app & account/i, level: 1 })).toBeVisible();
    await expect(page.getByRole('switch', { name: /^email$/i })).toBeDisabled(); // coming soon
    await expect(page.getByRole('switch', { name: /real-time/i })).toBeEnabled();
  });

  test('toggling websocket fires a PUT', async ({ page }) => {
    let putCalled = false;

    await page.route('**/api/notification-preferences', (route) => {
      if (route.request().method() === 'PUT') {
        putCalled = true;
        return route.fulfill({ status: 204 });
      }
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          consoleEnabled: true,
          emailEnabled: false,
          webSocketEnabled: false,
        }),
      });
    });

    await page.goto('/settings/app');

    await page.getByRole('switch', { name: /real-time/i }).click();

    await expect.poll(() => putCalled, { timeout: 3000 }).toBe(true);
  });
});
