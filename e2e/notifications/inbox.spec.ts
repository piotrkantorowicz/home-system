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

  test('clicking an unread row marks it read', async ({ page }) => {
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

    // NotificationListItem's row button only carries an accessible name
    // ("Mark as read") while unread — once read, aria-label is removed
    // entirely (it becomes a disabled, unlabeled control). A name-scoped
    // role locator would stop matching the instant the click succeeds, so
    // anchor on the notification's own title text instead, which is stable
    // across the read/unread transition.
    const row = page.getByText('Time for lunch').locator('xpath=ancestor::button[1]');
    await expect(row).toHaveAttribute('aria-pressed', 'false');
    await row.click();
    await expect(row).toHaveAttribute('aria-pressed', 'true');
    expect(readCalled).toBe(true);
  });
});
