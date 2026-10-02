import { test, expect } from '../diet-planner/fixtures';

// Each worker owns its own household, so setup/envelope state never crosses workers.
test.describe('Budget envelopes', () => {
  test('owner sets up Budget, manages envelopes with the keyboard, and the page fits a phone', async ({
    page,
  }) => {
    const name = `E2E ${Date.now().toString(36)}`;
    const renamed = `${name} renamed`;

    await page.goto('/budget');

    // First use: choose a currency and set up — or skip when a previous run already did.
    const setup = page.getByRole('button', { name: 'Set up Budget' });
    const heading = page.getByRole('heading', { name: 'Envelopes' });
    await expect(setup.or(page.getByRole('link', { name: 'Manage envelopes' }))).toBeVisible();
    if (await setup.isVisible()) {
      await setup.click();
      await expect(page.getByText(/Currency: PLN/)).toBeVisible();
    }

    await page.goto('/budget/envelopes');
    await expect(heading).toBeVisible();
    await expect(page.getByText('Everyday').first()).toBeVisible();

    // Create a shared envelope using only the keyboard: focus, activate, type, submit.
    const newEnvelope = page.getByRole('button', { name: 'New envelope' });
    await newEnvelope.focus();
    await expect(newEnvelope).toBeFocused();
    await page.keyboard.press('Enter');
    await page.getByLabel('Name', { exact: true }).fill(name);
    await page.keyboard.press('Enter');
    await expect(page.getByText(name, { exact: true })).toBeVisible();

    // Rename, archive, restore.
    await page.getByRole('button', { name: `Rename ${name}` }).click();
    await page.getByLabel('Name', { exact: true }).fill(renamed);
    await page.getByRole('button', { name: 'Save' }).click();
    await expect(page.getByText(renamed, { exact: true })).toBeVisible();

    await page.getByRole('button', { name: `Archive ${renamed}` }).click();
    await expect(page.getByRole('heading', { name: 'Archived' })).toBeVisible();
    await page.getByRole('button', { name: `Restore ${renamed}` }).click();
    await expect(page.getByRole('button', { name: `Archive ${renamed}` })).toBeVisible();

    // A phone-width screen keeps every control reachable without sideways scrolling.
    await page.setViewportSize({ width: 390, height: 844 });
    await expect(page.getByRole('button', { name: 'New envelope' })).toBeVisible();
    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
    );
    expect(overflow).toBeLessThanOrEqual(0);
  });
});
