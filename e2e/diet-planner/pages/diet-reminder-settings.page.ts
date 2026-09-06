import { expect } from '@playwright/test';

import { BasePage } from './BasePage';
import { gotoProfileSection } from './profile-hub.helper';

import type { Page, Locator } from '@playwright/test';

export class DietReminderSettingsPage extends BasePage {
  readonly mealRemindersCheckbox: Locator;
  readonly mealLeadTimeInput: Locator;
  readonly mealMissedGraceInput: Locator;
  readonly waterRemindersCheckbox: Locator;
  readonly waterIntervalInput: Locator;
  readonly waterWindowStartInput: Locator;
  readonly waterWindowEndInput: Locator;
  readonly weeklySummaryCheckbox: Locator;
  readonly weeklySummaryDayInput: Locator;
  readonly weeklySummaryTimeInput: Locator;
  readonly goalAlertsCheckbox: Locator;
  readonly saveButton: Locator;
  readonly successMessage: Locator;

  constructor(page: Page) {
    super(page);
    this.mealRemindersCheckbox = page.locator('#mealRemindersEnabled');
    this.mealLeadTimeInput = page.locator('#mealReminderLeadTimeMinutes');
    this.mealMissedGraceInput = page.locator('#mealMissedGraceMinutes');
    this.waterRemindersCheckbox = page.locator('#waterRemindersEnabled');
    this.waterIntervalInput = page.locator('#waterReminderIntervalMinutes');
    this.waterWindowStartInput = page.locator('#waterWindowStartLocal');
    this.waterWindowEndInput = page.locator('#waterWindowEndLocal');
    this.weeklySummaryCheckbox = page.locator('#weeklySummaryEnabled');
    this.weeklySummaryDayInput = page.locator('#weeklySummaryDayOfWeekLocal');
    this.weeklySummaryTimeInput = page.locator('#weeklySummaryTimeOfDayLocal');
    this.goalAlertsCheckbox = page.locator('#goalAlertsEnabled');
    this.saveButton = page.getByRole('button', { name: /^save$/i });
    this.successMessage = page.getByText(/diet reminders saved/i);
  }

  async goto() {
    await gotoProfileSection(this.page, 'notifications');
  }

  async setMealLeadTime(minutes: number) {
    await this.mealLeadTimeInput.selectOption(String(minutes));
  }

  async setMealMissedGrace(minutes: number) {
    await this.mealMissedGraceInput.selectOption(String(minutes));
  }

  async setWaterInterval(minutes: number) {
    await this.waterIntervalInput.selectOption(String(minutes));
  }

  async setWaterWindow(startHHmm: string, endHHmm: string) {
    await this.waterWindowStartInput.fill(startHHmm);
    await this.waterWindowEndInput.fill(endHHmm);
  }

  async setWeeklySummary(dayOfWeek: number, timeHHmm: string) {
    await this.weeklySummaryDayInput.selectOption(String(dayOfWeek));
    await this.weeklySummaryTimeInput.fill(timeHHmm);
  }

  async toggleMealReminders(enable: boolean) {
    await this.toggle(this.mealRemindersCheckbox, enable);
  }

  async toggleWaterReminders(enable: boolean) {
    await this.toggle(this.waterRemindersCheckbox, enable);
  }

  async toggleWeeklySummary(enable: boolean) {
    await this.toggle(this.weeklySummaryCheckbox, enable);
  }

  async toggleGoalAlerts(enable: boolean) {
    await this.toggle(this.goalAlertsCheckbox, enable);
  }

  async save() {
    // Register the response listener BEFORE clicking — on a fast local
    // backend the PUT can resolve before a listener attached afterward
    // would ever see it.
    const responsePromise = this.page.waitForResponse(
      (resp) =>
        resp.url().includes('/api/v1/diet-reminder-settings') && resp.request().method() === 'PUT',
    );
    await this.saveButton.click();
    await responsePromise;
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

  private async toggle(checkbox: Locator, enable: boolean) {
    const isChecked = await checkbox.isChecked();
    if (isChecked !== enable) {
      await checkbox.click();
    }
  }
}
