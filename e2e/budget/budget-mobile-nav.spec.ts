import { test, expect } from '../diet-planner/fixtures';

// Each worker owns its own household, so budget state never crosses workers.
test.describe('Budget phone navigation', () => {
  test('tabs, raised add action and More sheet reach every Budget destination', async ({
    page,
  }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/budget');
    const setup = page.getByRole('button', { name: 'Set up Budget' });
    await expect(setup.or(page.getByRole('link', { name: 'Manage envelopes' }))).toBeVisible();
    if (await setup.isVisible()) await setup.click();
    await expect(
      page
        .getByRole('heading', { name: 'Budget', level: 2 })
        .or(page.getByRole('heading', { name: 'Budget', level: 1 })),
    ).toBeVisible();

    const bar = page.getByRole('navigation', { name: 'Budget' });
    for (const name of ['Overview', 'Expenses', 'Add expense', 'Settle up'])
      await expect(bar.getByRole('link', { name })).toBeVisible();
    await expect(bar.getByRole('link', { name: 'Envelopes' })).toHaveCount(0);

    // The raised action opens the existing expense form and drops the flag on close.
    await bar.getByRole('link', { name: 'Add expense' }).click();
    await expect(page).toHaveURL(/\/budget\/expenses\?add=1$/);
    const dialog = page.getByRole('dialog', { name: 'Add expense' });
    await expect(dialog).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(dialog).toBeHidden();
    await expect(page).toHaveURL('/budget/expenses');

    // Envelopes lives in More; the tab for the current page is marked.
    await expect(bar.getByRole('link', { name: 'Expenses' })).toHaveAttribute(
      'aria-current',
      'page',
    );
    await bar.getByRole('button', { name: 'More' }).click();
    const sheet = page.getByRole('dialog', { name: 'More' });
    await sheet.getByRole('link', { name: 'Envelopes' }).click();
    await expect(page).toHaveURL('/budget/envelopes');
    await expect(sheet).toBeHidden();
    await expect(bar.getByRole('button', { name: 'More' })).toHaveAttribute('aria-current', 'page');

    // The module switcher in the sheet moves to the other product module.
    await bar.getByRole('button', { name: 'More' }).click();
    await sheet.getByRole('button', { name: /Diet Planner/ }).click();
    await expect(page).toHaveURL(/\/diet-planner/);
  });
});
