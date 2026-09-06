import { test, expect } from './fixtures';
import { DietReminderSettingsPage } from './pages';
import { seedDietReminderSettings } from './utils/seed';

// Serial: every test reads/writes the same per-user diet-reminder-settings
// record (there's no per-test scoping for this resource), so fullyParallel
// execution across tests in this file races and flakes — a checkbox toggled
// by one test can be reset mid-flight by another, e.g. right as "user can
// save diet reminder settings" clicks Save.
test.describe.configure({ mode: 'serial' });

test.describe('Diet Reminder Settings', () => {
  test('user can navigate to diet reminders page', async ({ page }) => {
    const prefsPage = new DietReminderSettingsPage(page);
    await prefsPage.goto();

    await expect(page).toHaveURL(/\/diet-planner\/profile\?section=notifications/);
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
  });

  test('page shows all reminder sections', async ({ page }) => {
    const prefsPage = new DietReminderSettingsPage(page);
    await prefsPage.goto();

    await expect(page.getByText(/meal reminder/i).first()).toBeVisible();
    await expect(page.getByText(/water reminder/i).first()).toBeVisible();
    await expect(page.getByText(/weekly nutrition summary/i).first()).toBeVisible();
    await expect(page.getByText(/goal milestone/i).first()).toBeVisible();
  });

  test('save button is disabled when form has not been changed', async ({ page }) => {
    const prefsPage = new DietReminderSettingsPage(page);
    await prefsPage.goto();

    await page
      .waitForResponse(
        (r) =>
          r.url().includes('/api/v1/diet-reminder-settings') &&
          r.request().method() === 'GET' &&
          r.status() < 500,
        { timeout: 5000 },
      )
      .catch(() => undefined);

    await prefsPage.expectSaveButtonDisabled();
  });

  test('save button becomes enabled after changing a setting', async ({ page }) => {
    const prefsPage = new DietReminderSettingsPage(page);
    await prefsPage.goto();

    await prefsPage.weeklySummaryCheckbox.click();

    await prefsPage.expectSaveButtonEnabled();
  });

  test('user can save diet reminder settings', async ({ page }) => {
    const prefsPage = new DietReminderSettingsPage(page);
    await prefsPage.goto();

    await prefsPage.weeklySummaryCheckbox.click();
    await prefsPage.goalAlertsCheckbox.click();

    await prefsPage.save();

    await expect(prefsPage.successMessage).toBeVisible({ timeout: 5000 });
  });

  test('meal lead time input is disabled when meal reminders are off', async ({ page }) => {
    const prefsPage = new DietReminderSettingsPage(page);
    await prefsPage.goto();

    const isChecked = await prefsPage.mealRemindersCheckbox.isChecked();
    if (!isChecked) {
      await prefsPage.toggleMealReminders(true);
    }
    await prefsPage.expectMealLeadTimeEnabled();

    await prefsPage.toggleMealReminders(false);

    await prefsPage.expectMealLeadTimeDisabled();
  });

  test('water interval input is disabled when water reminders are off', async ({ page }) => {
    const prefsPage = new DietReminderSettingsPage(page);
    await prefsPage.goto();

    const isChecked = await prefsPage.waterRemindersCheckbox.isChecked();
    if (!isChecked) {
      await prefsPage.toggleWaterReminders(true);
    }
    await prefsPage.expectWaterIntervalEnabled();

    await prefsPage.toggleWaterReminders(false);

    await prefsPage.expectWaterIntervalDisabled();
  });

  test('settings persist after saving and reloading page', async ({ page }) => {
    const prefsPage = new DietReminderSettingsPage(page);
    await prefsPage.goto();

    await seedDietReminderSettings(page, {
      mealRemindersEnabled: true,
      mealReminderLeadTimeMinutes: 15,
    });
    await prefsPage.goto();

    const isChecked = await prefsPage.mealRemindersCheckbox.isChecked();
    if (!isChecked) {
      await prefsPage.mealRemindersCheckbox.click();
    }

    await prefsPage.setMealLeadTime(30);

    await prefsPage.weeklySummaryCheckbox.click();
    const expectedWeeklySummary = await prefsPage.weeklySummaryCheckbox.isChecked();

    await prefsPage.save();
    await expect(prefsPage.successMessage).toBeVisible({ timeout: 5000 });

    await prefsPage.goto();

    await expect(prefsPage.mealLeadTimeInput).toHaveValue('30');
    const weeklySummaryChecked = await prefsPage.weeklySummaryCheckbox.isChecked();
    expect(weeklySummaryChecked).toBe(expectedWeeklySummary);
  });
});
