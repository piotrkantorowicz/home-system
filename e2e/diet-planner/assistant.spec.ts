import { addDays, format } from 'date-fns';

import { test, expect } from './fixtures';
import { AssistantPanel, CalendarPage } from './pages';
import { createApiContext } from './utils/seed';

interface PlannedMeal {
  recipeName: string;
  status: string;
}

/**
 * Epic #634 acceptance: ask the assistant for a week, confirm its preview card, see the
 * meals in the planner. The backend runs `Assistant:Provider=Fake`, which plans a week of
 * lunches of a recipe named after the message (docs/e2e/assistant.md).
 */
test('a week planned through the assistant shows up in the planner, not eaten', async ({
  page,
}) => {
  const recipe = `Assistant week ${Date.now()}`;
  const assistant = new AssistantPanel(page);
  const calendar = new CalendarPage(page);
  await calendar.goto();

  await assistant.open();
  await assistant.ask(recipe);

  await expect(assistant.pendingCard).toContainText('execute_plan_import');
  await expect(assistant.message).toBeDisabled();
  await assistant.confirmButton.click();
  await expect(assistant.conversation).toContainText('Done. The week is in your planner.');
  await assistant.close();

  await calendar.goto();
  await expect(calendar.mealChips(recipe).first()).toBeVisible();

  const api = await createApiContext(page);
  try {
    const today = new Date();
    const res = await api.get('/api/v1/meals', {
      params: {
        from: format(addDays(today, -1), 'yyyy-MM-dd'),
        to: format(addDays(today, 8), 'yyyy-MM-dd'),
      },
    });
    expect(res.ok()).toBe(true);
    const meals = ((await res.json()) as PlannedMeal[]).filter((m) => m.recipeName === recipe);
    expect(meals).toHaveLength(7);
    expect(meals.filter((m) => m.status === 'Done' || m.status === 'Modified')).toHaveLength(0);
  } finally {
    await api.dispose();
  }
});
