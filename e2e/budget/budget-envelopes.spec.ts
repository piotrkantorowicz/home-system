import { test, expect } from '../diet-planner/fixtures';

import { archiveTrackedEnvelopes, trackEnvelope } from './utils/seed';

// Each worker owns its own household, so setup/envelope state never crosses workers.
test.describe('Budget envelopes', () => {
  test.afterEach(async ({ page }) => archiveTrackedEnvelopes(page));

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
    const createdResponse = page.waitForResponse(
      (r) => r.request().method() === 'POST' && r.url().endsWith('/api/budget/accounts'),
    );
    await page.keyboard.press('Enter');
    trackEnvelope(page, ((await (await createdResponse).json()) as { id: string }).id);
    await expect(page.getByText(name, { exact: true })).toBeVisible();

    // This month's count and limit come from the server; a new envelope has none yet.
    const row = (n: string) => page.getByRole('listitem').filter({ hasText: n });
    await expect(row(name)).toContainText('0 expenses in');
    await expect(row(name)).toContainText('No limit');

    // Set a limit: zero is a real limit, different from "No limit".
    await page.getByRole('button', { name: `Actions for ${name}` }).click();
    await page.getByRole('menuitem', { name: 'Set limit' }).click();
    const limit = page.getByRole('dialog');
    await limit.getByLabel(/^Limit/).fill('0');
    await limit.getByRole('button', { name: 'Save' }).click();
    await expect(limit).toBeHidden();
    await expect(row(name)).toContainText('0.00 / month');

    // Rename, archive, restore, all from the row menu.
    await page.getByRole('button', { name: `Actions for ${name}` }).click();
    await page.getByRole('menuitem', { name: 'Rename' }).click();
    await page.getByLabel('Name', { exact: true }).fill(renamed);
    await page.getByRole('button', { name: 'Save' }).click();
    await expect(page.getByText(renamed, { exact: true })).toBeVisible();

    await page.getByRole('button', { name: `Actions for ${renamed}` }).click();
    await page.getByRole('menuitem', { name: 'Archive' }).click();
    const archived = page.locator('details', { hasText: /^Archived/ });
    await expect(archived).toBeVisible();
    await expect(archived).not.toHaveAttribute('open', '');
    await archived.getByText(/^Archived/).click();
    await page.getByRole('button', { name: `Restore ${renamed}` }).click();
    await expect(page.getByRole('button', { name: `Actions for ${renamed}` })).toBeVisible();

    // A phone-width screen keeps every control reachable without sideways scrolling.
    await page.setViewportSize({ width: 390, height: 844 });
    await expect(page.getByRole('button', { name: 'New envelope' })).toBeVisible();
    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
    );
    expect(overflow).toBeLessThanOrEqual(0);
  });
});
