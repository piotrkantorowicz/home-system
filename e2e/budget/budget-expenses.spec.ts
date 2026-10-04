import { test, expect } from '../diet-planner/fixtures';

import { ensureBudget } from './utils/seed';

// Each worker owns its own household, so budget state never crosses workers.
test.describe('Budget expenses', () => {
  test('owner adds, corrects and voids an expense; a similar one is flagged first', async ({
    page,
  }) => {
    const amount = `${String(10 + (Date.now() % 80))}.${String(10 + (Date.now() % 89))}`;

    await page.goto('/budget');
    const setup = page.getByRole('button', { name: 'Set up Budget' });
    await expect(setup.or(page.getByRole('link', { name: 'Manage envelopes' }))).toBeVisible();
    if (await setup.isVisible()) await setup.click();

    await page.goto('/budget/expenses');
    await page.getByRole('button', { name: 'Add expense' }).click();
    const dialog = page.getByRole('dialog');

    // Household funds keep the journey independent of other members; amount uses a decimal comma.
    await dialog.getByText('Household account', { exact: true }).click();
    await dialog.getByLabel(/^Amount/).fill(amount.replace('.', ','));
    await dialog.getByText('Groceries', { exact: true }).click();
    await expect(dialog.getByRole('button', { name: 'Save' })).toBeEnabled();
    await dialog.getByRole('button', { name: 'Save' }).click();
    await expect(dialog).toBeHidden();
    const row = page.getByRole('link', { name: new RegExp(`${amount.replace('.', '\\.')} PLN`) });
    await expect(row.first()).toBeVisible();

    // The same amount and category within two days is flagged; Keep both is a deliberate choice.
    await page.getByRole('button', { name: 'Add expense' }).click();
    const second = page.getByRole('dialog');
    await second.getByText('Household account', { exact: true }).click();
    await second.getByLabel(/^Amount/).fill(amount);
    await expect(second.getByText(/similar expense.* already exist/)).toBeVisible();
    await expect(second.getByRole('button', { name: 'Save' })).toBeDisabled();
    await second.getByRole('button', { name: 'Cancel' }).first().click();
    await expect(second).toBeHidden();

    // Correct it, with a reason; the history keeps both revisions.
    await row.first().click();
    await page.getByRole('button', { name: 'Correct' }).click();
    const correct = page.getByRole('dialog');
    await correct.getByLabel(/^Amount/).fill('1,00');
    await correct.getByLabel('Reason').fill('typo');
    await correct.getByRole('button', { name: 'Save' }).click();
    await expect(correct).toBeHidden();
    await expect(page.getByRole('heading', { name: /^1\.00 PLN/ })).toBeVisible();
    const history = page.getByRole('region', { name: 'History' });
    await expect(history.getByText(/Revision 1 · Created/)).toBeVisible();
    await expect(history.getByText(/Revision 2 · Corrected/)).toBeVisible();

    // Void it: it stops offering actions and leaves the default list.
    await page.getByRole('button', { name: 'Void' }).click();
    const voidDialog = page.getByRole('dialog');
    await voidDialog.getByLabel('Reason').fill('test cleanup');
    await voidDialog.getByRole('button', { name: 'Void expense' }).click();
    await expect(page.getByText('Voided').first()).toBeVisible();
    await expect(page.getByRole('button', { name: 'Correct' })).toHaveCount(0);

    // Phone width: the list screen needs no sideways scrolling.
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/budget/expenses');
    await expect(page.getByRole('button', { name: 'Add expense' })).toBeVisible();
    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
    );
    expect(overflow).toBeLessThanOrEqual(0);
  });

  test('on a short phone viewport the amount takes focus and Save stays reachable', async ({
    page,
  }) => {
    await page.goto('/budget');
    await ensureBudget(page);
    await page.setViewportSize({ width: 390, height: 420 });
    await page.goto('/budget/expenses?add=1');

    const dialog = page.getByRole('dialog');
    const amount = dialog.getByLabel(/^Amount/);
    await expect(amount).toBeFocused();
    await amount.fill('12,50');

    // The footer sits outside the scrolling body, so Save is in view without scrolling.
    await expect(dialog.getByRole('button', { name: /^Save 12\.50$/ })).toBeInViewport({ ratio: 1 });
  });
});
