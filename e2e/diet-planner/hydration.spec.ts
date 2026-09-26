import { test, expect } from './fixtures';
import { DashboardPage, HydrationPage, HydrationSettingsPage } from './pages';
import { createApiContext } from './utils/seed';

test.describe('Hydration', () => {
  test('hydration page loads and shows the heading', async ({ page }) => {
    const hydrationPage = new HydrationPage(page);
    await hydrationPage.goto();

    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
  });

  test('the water-level meter is visible on the page', async ({ page }) => {
    const hydrationPage = new HydrationPage(page);
    await hydrationPage.goto();

    await hydrationPage.expectLevelMeterVisible();
  });

  test('settings form shows daily target and glass size fields', async ({ page }) => {
    const settingsPage = new HydrationSettingsPage(page);
    await settingsPage.goto();

    await expect(settingsPage.dailyTargetInput).toBeVisible();
    await expect(settingsPage.glassSizeInput).toBeVisible();
  });

  test('save settings button is visible', async ({ page }) => {
    const settingsPage = new HydrationSettingsPage(page);
    await settingsPage.goto();

    await expect(settingsPage.saveSettingsButton).toBeVisible();
  });

  test('quick-add glass button is visible', async ({ page }) => {
    const hydrationPage = new HydrationPage(page);
    await hydrationPage.goto();

    await expect(page.getByRole('button', { name: /\+.*ml|glass/i }).first()).toBeVisible();
  });

  test('can update hydration settings', async ({ page }) => {
    const settingsPage = new HydrationSettingsPage(page);
    await settingsPage.goto();

    // Alternate values to guarantee the form is always dirty regardless of prior run state
    const currentTarget = await settingsPage.dailyTargetInput.inputValue();
    const newTarget = currentTarget === '3000' ? 2500 : 3000;
    const currentGlass = await settingsPage.glassSizeInput.inputValue();
    const newGlass = currentGlass === '300' ? 250 : 300;
    await settingsPage.updateSettings({ dailyTargetMl: newTarget, glassSizeMl: newGlass });

    // After save the form resets to saved values — field still visible
    await expect(settingsPage.dailyTargetInput).toBeVisible();
  });

  test('water entries persist, can be removed, and update the dashboard', async ({ page }) => {
    const settings = new HydrationSettingsPage(page);
    await settings.goto();
    if (
      (await settings.dailyTargetInput.inputValue()) !== '2500' ||
      (await settings.glassSizeInput.inputValue()) !== '250'
    ) {
      await settings.updateSettings({ dailyTargetMl: 2500, glassSizeMl: 250 });
      await expect(settings.saveSettingsButton).toBeDisabled();
    }

    const hydration = new HydrationPage(page);
    await hydration.goto();
    const date = await page.evaluate(() => {
      const now = new Date();
      return `${String(now.getFullYear())}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
    });
    const api = await createApiContext(page);
    const response = await api.get('/api/v1/hydration/intake', { params: { date } });
    expect(response.ok()).toBe(true);
    const baseline = (await response.json()) as { totalMl: number; entries: unknown[] };
    await api.dispose();

    const note = `E2E custom water ${Date.now()}`;
    await hydration.addGlassButton.click();
    await expect(hydration.deleteEntryButtons).toHaveCount(baseline.entries.length + 1);
    await hydration.customButton.click();
    await hydration.customNoteInput.fill(note);
    await hydration.customAddButton.click();
    await expect(hydration.entryNote(note)).toBeVisible();
    await expect(hydration.deleteEntryButtons).toHaveCount(baseline.entries.length + 2);

    await page.reload();
    await expect(hydration.entryNote(note)).toBeVisible();
    await expect(hydration.total(baseline.totalMl + 580)).toBeVisible();
    await hydration.deleteEntryButtons.first().click();
    const deletion = page.waitForResponse(
      (response) =>
        response.request().method() === 'DELETE' &&
        response.url().includes('/api/v1/hydration/intake/'),
    );
    await hydration.confirmRemoveButton.click();
    expect((await deletion).ok()).toBe(true);
    await expect(hydration.entryNote(note)).toBeHidden();
    await expect(hydration.deleteEntryButtons).toHaveCount(baseline.entries.length + 1);

    await page.reload();
    const persistedApi = await createApiContext(page);
    const persistedResponse = await persistedApi.get('/api/v1/hydration/intake', {
      params: { date },
    });
    expect(persistedResponse.ok()).toBe(true);
    const persisted = (await persistedResponse.json()) as { totalMl: number; entries: unknown[] };
    await persistedApi.dispose();
    expect(persisted.totalMl).toBe(baseline.totalMl + 250);
    expect(persisted.entries).toHaveLength(baseline.entries.length + 1);
    const dashboard = new DashboardPage(page);
    await dashboard.goto();
    await expect(dashboard.waterTotal(baseline.totalMl + 250)).toBeVisible();
    await expect(dashboard.waterProgress).toHaveAttribute(
      'aria-valuenow',
      String(Math.min(baseline.totalMl + 250, 2500)),
    );
  });
});
