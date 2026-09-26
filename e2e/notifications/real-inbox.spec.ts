import {
  createApiContext,
  seedDietReminderSettings,
  seedMealSchedule,
} from '../diet-planner/utils/seed';
import { expect, test } from './fixtures';
import { NotificationsPage } from './pages/notifications.page';

import type { DietReminderSettingsSeed } from '../diet-planner/utils/seed';

interface ChannelPreferences {
  consoleEnabled: boolean;
  emailEnabled: boolean;
  webSocketEnabled: boolean;
}

interface Slot {
  id: string;
  name: string;
  defaultTime: string;
}
interface Notification {
  id: string;
  title: string;
  readAt: string | null;
}

test('real preferences and meal-missed inbox support single and bulk read', async ({ page }) => {
  // Production scheduler ticks every 60 seconds; delivery then drains the outbox.
  test.setTimeout(150_000);
  const notifications = new NotificationsPage(page);
  await notifications.preferences();
  const api = await createApiContext(page);
  const preferenceResponse = await api.get('/api/notification-preferences');
  expect(preferenceResponse.ok()).toBe(true);
  const preferences = (await preferenceResponse.json()) as ChannelPreferences;
  const settingsResponse = await api.get('/api/v1/diet-reminder-settings');
  const settings = settingsResponse.ok()
    ? ((await settingsResponse.json()) as DietReminderSettingsSeed)
    : null;
  let scheduleResponse = await api.get('/api/v1/meal-schedule');
  const scheduleText = (await scheduleResponse.text()).trim();
  let schedule = (scheduleText ? JSON.parse(scheduleText) : null) as { slots: Slot[] } | null;
  if (!schedule?.slots.length) {
    await seedMealSchedule(page);
    scheduleResponse = await api.get('/api/v1/meal-schedule');
    schedule = (await scheduleResponse.json()) as { slots: Slot[] };
  }
  const originalSlots = schedule!.slots;
  const slotName = `Inbox ${Date.now()}`;
  const mealIds: string[] = [];
  let notificationIds: string[] = [];
  let productId: string | undefined;
  let recipeId: string | undefined;
  try {
    const save = page.waitForResponse(
      (response) =>
        response.url().endsWith('/api/notification-preferences') &&
        response.request().method() === 'PUT',
    );
    await notifications.realTimeSwitch().click();
    expect((await save).ok()).toBe(true);
    await page.reload();
    await expect(notifications.realTimeSwitch()).toBeChecked({
      checked: !preferences.webSocketEnabled,
    });
    // Enable delivery while testing the real inbox.
    expect(
      (
        await api.put('/api/notification-preferences', {
          data: { ...preferences, webSocketEnabled: true },
        })
      ).ok(),
    ).toBe(true);

    await seedDietReminderSettings(page, {
      mealRemindersEnabled: false,
      waterRemindersEnabled: false,
      weeklySummaryEnabled: false,
    });
    expect(originalSlots.length).toBeLessThan(8);
    expect(
      (
        await api.put('/api/v1/meal-schedule', {
          data: { slots: [...originalSlots, { name: slotName, defaultTime: '12:00' }] },
        })
      ).ok(),
    ).toBe(true);
    const slots = (await (await api.get('/api/v1/meal-schedule')).json()) as { slots: Slot[] };
    const slot = slots.slots.find((item) => item.name === slotName)!;
    const product = await api.post('/api/v1/products', {
      data: { name: slotName, defaultUnit: 'g', calories: 100 },
    });
    expect(product.ok()).toBe(true);
    productId = (await product.json()) as string;
    const recipe = await api.post('/api/v1/recipes', {
      data: { name: slotName, servings: 1, ingredients: [{ productId, amount: 100, unit: 'g' }] },
    });
    expect(recipe.ok()).toBe(true);
    recipeId = (await recipe.json()) as string;
    const past = new Date(Date.now() - 2 * 60 * 60 * 1000);
    for (let index = 0; index < 3; index++) {
      const meal = await api.post('/api/v1/meals', {
        data: {
          date: past.toISOString().slice(0, 10),
          mealTime: past.toISOString().slice(11, 19),
          mealSlotId: slot.id,
          recipeId,
          servings: 1,
          sequenceOrder: index,
        },
      });
      expect(meal.ok()).toBe(true);
      mealIds.push((await meal.json()) as string);
    }
    await seedDietReminderSettings(page, {
      mealRemindersEnabled: true,
      mealMissedGraceMinutes: 1,
      waterRemindersEnabled: false,
      weeklySummaryEnabled: false,
    });
    await expect
      .poll(
        async () => {
          const response = await api.get('/api/notifications', {
            params: { page: 1, pageSize: 100 },
          });
          expect(response.ok()).toBe(true);
          const result = (await response.json()) as { items: Notification[] };
          notificationIds = result.items
            .filter((item) => item.title === `Missed ${slotName}`)
            .map((item) => item.id);
          return notificationIds.length;
        },
        { timeout: 90_000, intervals: [1000] },
      )
      .toBe(3);
    await seedDietReminderSettings(page, {
      mealRemindersEnabled: false,
      waterRemindersEnabled: false,
      weeklySummaryEnabled: false,
    });
    const { total } = (await (await api.get('/api/notifications/unread-count')).json()) as {
      total: number;
    };
    await notifications.inbox();
    await expect(notifications.rows(slotName)).toHaveCount(3);
    await expect(notifications.rows(slotName).getByRole('button', { pressed: false })).toHaveCount(
      3,
    );
    await notifications.expectUnreadCount(total);
    await notifications.readFirst(slotName);
    await notifications.expectUnreadCount(total - 1);
    await page.reload();
    await expect(notifications.rows(slotName).getByRole('button', { pressed: true })).toHaveCount(
      1,
    );
    await notifications.expectUnreadCount(total - 1);
    await notifications.readRemaining(slotName);
    await notifications.expectUnreadCount(total - 3);
    await page.reload();
    await expect(notifications.rows(slotName).getByRole('button', { pressed: true })).toHaveCount(
      3,
    );
    await notifications.expectUnreadCount(total - 3);
  } finally {
    if (notificationIds.length)
      expect(
        (await api.post('/api/notifications/read', { data: { ids: notificationIds } })).ok(),
      ).toBe(true);
    for (const id of mealIds) expect((await api.delete(`/api/v1/meals/${id}`)).ok()).toBe(true);
    if (recipeId) expect((await api.delete(`/api/v1/recipes/${recipeId}`)).ok()).toBe(true);
    if (productId) expect((await api.delete(`/api/v1/products/${productId}`)).ok()).toBe(true);
    expect((await api.put('/api/v1/meal-schedule', { data: { slots: originalSlots } })).ok()).toBe(
      true,
    );
    if (settings)
      expect((await api.put('/api/v1/diet-reminder-settings', { data: settings })).ok()).toBe(true);
    else await seedDietReminderSettings(page);
    expect((await api.put('/api/notification-preferences', { data: preferences })).ok()).toBe(true);
    await api.dispose();
  }
});
