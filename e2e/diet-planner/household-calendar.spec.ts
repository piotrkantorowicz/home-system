import { test, expect } from './fixtures';
import { CalendarPage } from './pages';
import { addManagedChild, removeManagedMember, seedRecipe } from './utils/household-seed';

import type { ManagedMember } from './utils/household-seed';

test.describe.configure({ mode: 'serial' });

test.describe('Household calendar', () => {
  const stamp = Date.now();
  const childName = `E2E Kid ${stamp}`;
  const slotName = `Kid breakfast ${stamp}`;
  const recipeName = `E2E Kid Porridge ${stamp}`;
  let child: ManagedMember | undefined;

  // `page` is test-scoped, so clean up after the test rather than in afterAll.
  test.afterEach(async ({ page }) => {
    if (child) await removeManagedMember(page, child);
    child = undefined;
  });

  test('an adult plans a meal for a managed child and sees it on the child calendar', async ({
    page,
  }) => {
    const calendar = new CalendarPage(page);
    await calendar.goto();
    child = await addManagedChild(page, childName, slotName);
    await seedRecipe(page, recipeName);

    // The filter opens on the caller; switch to the child.
    await page.reload();
    await calendar.weekGrid.waitFor();
    await calendar.personFilter.selectOption({ label: childName });
    await expect(page).toHaveURL(new RegExp(`person=${child.personId}`));

    // The grid now shows the child's own schedule; plan from the form.
    await calendar.clickAddMeal('Mon', slotName);
    await expect(calendar.assignToPicker).toHaveValue(child.personId);
    await calendar.fillMealForm(recipeName);
    await calendar.submitMealForm();
    await calendar.expectMealInDay('Mon', recipeName);

    // The selection survives a reload.
    await page.reload();
    await calendar.weekGrid.waitFor();
    await expect(calendar.personFilter).toHaveValue(child.personId);
    await calendar.expectMealInDay('Mon', recipeName);

    // Back on the caller's own plan (always listed first) the child's meal is gone.
    await calendar.personFilter.selectOption({ index: 0 });
    await expect(page).not.toHaveURL(/person=/);
    await expect(calendar.mealChips(recipeName)).toHaveCount(0);
  });
});
