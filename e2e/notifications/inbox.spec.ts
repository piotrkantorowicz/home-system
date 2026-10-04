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

    // Exact + level:1 — the empty-state's own "No notifications yet" is
    // itself a heading and matches a loose /notifications/i substring.
    await expect(page.getByRole('heading', { name: 'Notifications', level: 1 })).toBeVisible();
    await expect(page.getByText(/no notifications yet/i)).toBeVisible();
  });

  test('opening an unread item marks it read', async ({ page }) => {
    const id = '11111111-1111-1111-1111-111111111111';
    let readCalled = false;

    // Stateful mock: the list must reflect the read once the POST lands, or
    // the mutation's onSettled refetch reverts the optimistic update.
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
              readAt: readCalled ? new Date().toISOString() : null,
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

    // Each item is one button; opening it marks it read and goes to its destination (Today).
    const row = page.getByRole('button', { name: /Time for lunch/ });
    await expect(row.getByRole('img', { name: 'Unread' })).toBeVisible();
    await row.click();
    await expect.poll(() => readCalled, { timeout: 3000 }).toBe(true);
  });
});
