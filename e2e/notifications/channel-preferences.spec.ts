import { test, expect } from './fixtures';

test.describe('Notifications channel preferences', () => {
  test('renders the three channel rows with email/websocket disabled', async ({ page }) => {
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
    await expect(switches).toHaveCount(3);
    await expect(switches.nth(0)).toBeEnabled();
    await expect(switches.nth(1)).toBeDisabled();
    await expect(switches.nth(2)).toBeDisabled();
  });

  test('toggling console fires a PUT', async ({ page }) => {
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

    const consoleSwitch = page.getByRole('switch').first();
    await consoleSwitch.click();

    await expect.poll(() => putCalled, { timeout: 3000 }).toBe(true);
  });
});
