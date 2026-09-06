import { test, expect } from './fixtures';

// The #208 redesign dropped the "console" channel row entirely — the page
// now shows only Email (disabled, "coming soon") and Websocket (the one
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

    await expect(page.getByRole('heading', { name: /channel preferences/i })).toBeVisible();
    const switches = page.getByRole('switch');
    await expect(switches).toHaveCount(2);
    await expect(switches.nth(0)).toBeDisabled(); // Email — "coming soon"
    await expect(switches.nth(1)).toBeEnabled(); // Websocket
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

    await page.goto('/notifications/preferences');

    const websocketSwitch = page.getByRole('switch').nth(1);
    await websocketSwitch.click();

    await expect.poll(() => putCalled, { timeout: 3000 }).toBe(true);
  });
});
