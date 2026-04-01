import { expect } from '@playwright/test';

import type { Page, Locator } from '@playwright/test';

export class NotificationPreferencesPage {
  readonly page: Page;
  readonly mealReminderCheckbox: Locator;
  readonly mealLeadTimeInput: Locator;
  readonly waterReminderCheckbox: Locator;
  readonly waterIntervalInput: Locator;
  readonly weeklySummaryCheckbox: Locator;
  readonly goalMilestoneCheckbox: Locator;
  readonly saveButton: Locator;
  readonly successMessage: Locator;

  constructor(page: Page) {
    this.page = page;
    this.mealReminderCheckbox = page.locator('#mealReminderEnabled');
    this.mealLeadTimeInput = page.locator('#mealReminderLeadTimeMinutes');
    this.waterReminderCheckbox = page.locator('#waterReminderEnabled');
    this.waterIntervalInput = page.locator('#waterReminderIntervalMinutes');
    this.weeklySummaryCheckbox = page.locator('#weeklySummaryEnabled');
    this.goalMilestoneCheckbox = page.locator('#goalMilestoneAlertsEnabled');
    this.saveButton = page.getByRole('button', { name: /save preferences/i });
    this.successMessage = page.getByText(/saved successfully/i);
  }

  async goto() {
    await this.page.goto('/diet-planner/notification-preferences');
    await this.page.waitForLoadState('networkidle');
  }

  async setMealLeadTime(minutes: number) {
    await this.mealLeadTimeInput.fill(String(minutes));
  }

  async setWaterInterval(minutes: number) {
    await this.waterIntervalInput.fill(String(minutes));
  }

  async toggleMealReminder(enable: boolean) {
    const isChecked = await this.mealReminderCheckbox.isChecked();
    if (isChecked !== enable) {
      await this.mealReminderCheckbox.click();
    }
  }

  async toggleWaterReminder(enable: boolean) {
    const isChecked = await this.waterReminderCheckbox.isChecked();
    if (isChecked !== enable) {
      await this.waterReminderCheckbox.click();
    }
  }

  async toggleWeeklySummary(enable: boolean) {
    const isChecked = await this.weeklySummaryCheckbox.isChecked();
    if (isChecked !== enable) {
      await this.weeklySummaryCheckbox.click();
    }
  }

  async toggleGoalMilestone(enable: boolean) {
    const isChecked = await this.goalMilestoneCheckbox.isChecked();
    if (isChecked !== enable) {
      await this.goalMilestoneCheckbox.click();
    }
  }

  async save() {
    await this.saveButton.click();
    await this.page.waitForResponse(
      (resp) =>
        resp.url().includes('/api/v1/notification-preferences') &&
        resp.request().method() === 'PUT',
    );
  }

  async expectSaveButtonEnabled() {
    await expect(this.saveButton).toBeEnabled();
  }

  async expectSaveButtonDisabled() {
    await expect(this.saveButton).toBeDisabled();
  }

  async expectMealLeadTimeEnabled() {
    await expect(this.mealLeadTimeInput).toBeEnabled();
  }

  async expectMealLeadTimeDisabled() {
    await expect(this.mealLeadTimeInput).toBeDisabled();
  }

  async expectWaterIntervalEnabled() {
    await expect(this.waterIntervalInput).toBeEnabled();
  }

  async expectWaterIntervalDisabled() {
    await expect(this.waterIntervalInput).toBeDisabled();
  }
}
