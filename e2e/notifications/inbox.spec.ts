import { test, expect } from './fixtures';

test.describe('Notifications inbox', () => {
  test('shows the empty state when no notifications exist', async ({ page }) => {
    await page.route('**/api/notifications**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [],
          page: 1,
          pageSize: 20,
          totalCount: 0,
          totalPages: 0,
        }),
      }),
    );

    await page.goto('/notifications');

    await expect(page.getByRole('heading', { name: /notifications/i })).toBeVisible();
    await expect(page.getByText(/no notifications yet/i)).toBeVisible();
  });

  test('clicking an unread row marks it read', async ({ page }) => {
    const id = '11111111-1111-1111-1111-111111111111';
    let readCalled = false;

    await page.route('**/api/notifications?**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              id,
              type: 'MealReminder',
              title: 'Time for lunch',
              body: 'Lunch is starting',
              createdAt: new Date().toISOString(),
              readAt: null,
            },
          ],
          page: 1,
          pageSize: 20,
          totalCount: 1,
          totalPages: 1,
        }),
      }),
    );

    await page.route(`**/api/notifications/${id}/read`, (route) => {
      readCalled = true;
      return route.fulfill({ status: 204 });
    });

    await page.goto('/notifications');

    const row = page.getByRole('button', { name: /mark as read/i });
    await expect(row).toHaveAttribute('aria-pressed', 'false');
    await row.click();
    await expect(row).toHaveAttribute('aria-pressed', 'true');
    expect(readCalled).toBe(true);
  });
});
