import { test, expect } from '../diet-planner/fixtures';

import {
  archiveTrackedEnvelopes,
  createEnvelope,
  createExpense,
  ensureBudget,
  get,
  minor,
  myPersonId,
  send,
  today,
} from './utils/seed';

// Each worker owns its own household, so budget state never crosses workers.
test.describe('Budget spending totals', () => {
  test.afterEach(async ({ page }) => archiveTrackedEnvelopes(page));

  test('correcting and voiding an expense moves the shared total, and a reload keeps it', async ({
    page,
  }) => {
    await page.goto('/household');
    await ensureBudget(page);
    const envelope = await createEnvelope(page, `Totals ${Date.now().toString(36)}`, 'Household');
    const month = today().slice(0, 7);
    const total = async () =>
      minor(
        (await get<{ totalSpent: string }>(page, `/api/budget/summary?month=${month}&scope=shared`))
          .body.totalSpent,
      );
    const before = await total();

    const { expenseId } = await createExpense(page, {
      accountId: envelope,
      amount: '12.34',
      category: 'Groceries',
      fundingSource: 'HouseholdFunds',
    });
    expect(await total()).toBe(before + 1234);

    const fields = {
      amount: '2.00',
      occurredOn: today(),
      category: 'Groceries',
      fundingSource: 'HouseholdFunds',
      paidByPersonId: null,
      participantIds: null,
    };
    expect(
      await send(page, 'put', `/api/budget/expenses/${expenseId}`, {
        clientRequestId: crypto.randomUUID(),
        expectedRevision: 1,
        reason: 'typo',
        ...fields,
      }),
    ).toBe(200);
    expect(await total()).toBe(before + 200);

    expect(
      await send(page, 'post', `/api/budget/expenses/${expenseId}/void`, {
        clientRequestId: crypto.randomUUID(),
        expectedRevision: 2,
        reason: 'test cleanup',
      }),
    ).toBe(200);
    expect(await total()).toBe(before);

    // The UI agrees after a reload: the voided expense is marked and no longer counts.
    await page.goto(`/budget/expenses/${expenseId}`);
    await page.reload();
    await expect(page.getByText('Voided').first()).toBeVisible();
  });
});
