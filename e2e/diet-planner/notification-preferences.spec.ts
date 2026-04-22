import { test, expect } from './fixtures';
import { NotificationPreferencesPage } from './pages/notification-preferences.page';

test.describe('Notification Preferences', () => {
  test('user can navigate to notification preferences page', async ({ page }) => {
    const prefsPage = new NotificationPreferencesPage(page);
    await prefsPage.goto();

    await expect(page).toHaveURL(/\/diet-planner\/profile\?section=notifications/);
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
  });

  test('page shows all notification sections', async ({ page }) => {
    const prefsPage = new NotificationPreferencesPage(page);
    await prefsPage.goto();

    // All four setting groups should be visible
    await expect(page.getByText(/meal reminder/i).first()).toBeVisible();
    await expect(page.getByText(/water reminder/i).first()).toBeVisible();
    await expect(page.getByText(/weekly nutrition summary/i).first()).toBeVisible();
    await expect(page.getByText(/goal milestone/i).first()).toBeVisible();
  });

  test('save button is disabled when form has not been changed', async ({ page }) => {
    const prefsPage = new NotificationPreferencesPage(page);
    await prefsPage.goto();

    // Wait for the form to load (either from 404 → defaults or from existing prefs)
    await page.waitForTimeout(500);

    await prefsPage.expectSaveButtonDisabled();
  });

  test('save button becomes enabled after changing a setting', async ({ page }) => {
    const prefsPage = new NotificationPreferencesPage(page);
    await prefsPage.goto();

    // Click directly to guarantee a change regardless of current backend state
    await prefsPage.weeklySummaryCheckbox.click();

    await prefsPage.expectSaveButtonEnabled();
  });

  test('user can save notification preferences', async ({ page }) => {
    const prefsPage = new NotificationPreferencesPage(page);
    await prefsPage.goto();

    // Click directly to guarantee dirty state regardless of current backend state
    await prefsPage.weeklySummaryCheckbox.click();
    await prefsPage.goalMilestoneCheckbox.click();

    await prefsPage.save();

    await expect(prefsPage.successMessage).toBeVisible({ timeout: 5000 });
  });

  test('meal lead time input is disabled when meal reminders are off', async ({ page }) => {
    const prefsPage = new NotificationPreferencesPage(page);
    await prefsPage.goto();

    // Ensure meal reminder is checked first so we can uncheck it
    const isChecked = await prefsPage.mealReminderCheckbox.isChecked();
    if (!isChecked) {
      await prefsPage.toggleMealReminder(true);
    }
    await prefsPage.expectMealLeadTimeEnabled();

    await prefsPage.toggleMealReminder(false);

    await prefsPage.expectMealLeadTimeDisabled();
  });

  test('water interval input is disabled when water reminders are off', async ({ page }) => {
    const prefsPage = new NotificationPreferencesPage(page);
    await prefsPage.goto();

    const isChecked = await prefsPage.waterReminderCheckbox.isChecked();
    if (!isChecked) {
      await prefsPage.toggleWaterReminder(true);
    }
    await prefsPage.expectWaterIntervalEnabled();

    await prefsPage.toggleWaterReminder(false);

    await prefsPage.expectWaterIntervalDisabled();
  });

  test('settings persist after saving and reloading page', async ({ page }) => {
    const prefsPage = new NotificationPreferencesPage(page);
    await prefsPage.goto();

    // Ensure meal reminder is on so lead time is editable
    const isChecked = await prefsPage.mealReminderCheckbox.isChecked();
    if (!isChecked) {
      await prefsPage.mealReminderCheckbox.click();
    }

    // Use a distinctive lead time value (must be one of the preset options)
    await prefsPage.setMealLeadTime(30);

    // Always click weekly summary to guarantee dirty state regardless of current backend value
    await prefsPage.weeklySummaryCheckbox.click();
    const expectedWeeklySummary = await prefsPage.weeklySummaryCheckbox.isChecked();

    await prefsPage.save();
    await expect(prefsPage.successMessage).toBeVisible({ timeout: 5000 });

    // Reload and verify values are restored
    await prefsPage.goto();

    await expect(prefsPage.mealLeadTimeInput).toHaveValue('30');
    const weeklySummaryChecked = await prefsPage.weeklySummaryCheckbox.isChecked();
    expect(weeklySummaryChecked).toBe(expectedWeeklySummary);
  });
});
