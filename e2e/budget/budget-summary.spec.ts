import { test, expect } from '../diet-planner/fixtures';

// Each worker owns its own household, so budget state never crosses workers.
test.describe('Budget monthly summary', () => {
  test('a zero limit reports overspending in words but never blocks an expense', async ({
    page,
  }) => {
    const amount = `${String(10 + (Date.now() % 80))}.${String(10 + (Date.now() % 89))}`;

    await page.goto('/budget');
    const setup = page.getByRole('button', { name: 'Set up Budget' });
    const overview = page.getByRole('heading', { name: 'Envelopes' });
    await expect(setup.or(overview)).toBeVisible();
    if (await setup.isVisible()) await setup.click();
    await expect(overview).toBeVisible();
    await expect(page.getByText(/^Shared spending ·/)).toBeVisible();

    // A limit of zero is a real limit (unlike "No limit").
    await page
      .getByRole('button', { name: /^(Set|Change) limit for Everyday$/ })
      .click();
    const limit = page.getByRole('dialog');
    await limit.getByLabel(/^Limit/).fill('0');
    await limit.getByRole('button', { name: 'Save' }).click();
    await expect(limit).toBeHidden();
    await expect(page.getByText(/Limit 0\.00 PLN/)).toBeVisible();

    // Overspending never blocks recording.
    await page.getByRole('button', { name: 'Add expense' }).click();
    const dialog = page.getByRole('dialog');
    await dialog.getByLabel('Envelope').selectOption({ label: 'Everyday' });
    await dialog.getByLabel('Funded by').selectOption('HouseholdFunds');
    await dialog.getByLabel(/^Amount/).fill(amount);
    await expect(dialog.getByRole('button', { name: 'Save' })).toBeEnabled();
    await dialog.getByRole('button', { name: 'Save' }).click();
    await expect(dialog).toBeHidden();
    await expect(page.getByText(/Over the limit by/)).toBeVisible();

    // Another month is its own page of numbers; going back restores this one.
    const label = (await page.getByText(/^Shared spending ·/).textContent()) ?? '';
    await page.getByRole('button', { name: 'Previous month' }).click();
    await expect(page.getByText(/^Shared spending ·/)).not.toHaveText(label);
    await page.getByRole('button', { name: 'Next month' }).click();
    await expect(page.getByText(/^Shared spending ·/)).toHaveText(label);

    // Clean up: clear the limit and void the expense.
    await page.getByRole('button', { name: 'Change limit for Everyday' }).click();
    await page.getByRole('dialog').getByRole('button', { name: 'Clear limit' }).click();
    await expect(page.getByText(/No limit/).first()).toBeVisible();
    await page
      .getByRole('link', { name: new RegExp(`${amount.replace('.', '\\.')} PLN`) })
      .first()
      .click();
    await page.getByRole('button', { name: 'Void' }).click();
    const voidDialog = page.getByRole('dialog');
    await voidDialog.getByLabel('Reason').fill('test cleanup');
    await voidDialog.getByRole('button', { name: 'Void expense' }).click();
    await expect(page.getByText('Voided').first()).toBeVisible();
  });
});
