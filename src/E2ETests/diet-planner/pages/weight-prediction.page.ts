import { expect } from '@playwright/test';

import type { Page, Locator } from '@playwright/test';

export class WeightPredictionPage {
  readonly page: Page;
  readonly calorieTargetInput: Locator;
  readonly bmrValue: Locator;
  readonly tdeeValue: Locator;
  readonly weeklyChangeValue: Locator;
  readonly currentBmiValue: Locator;
  readonly targetBmiValue: Locator;
  readonly goalDateValue: Locator;
  readonly noProfileMessage: Locator;
  readonly enterCaloriesMessage: Locator;
  readonly incompleteProfileMessage: Locator;

  constructor(page: Page) {
    this.page = page;
    this.calorieTargetInput = page.getByLabel(/daily calorie target/i);

    // Stat card values — locate the label <p>, navigate up to the container div,
    // then find the numeric value <p> within it.
    // Using xpath=.. is required because Playwright has no built-in parent-locator API.
    this.bmrValue = page
      .getByText('BMR', { exact: true })
      .locator('xpath=..')
      .locator('p.text-2xl');

    this.tdeeValue = page
      .getByText('TDEE', { exact: true })
      .locator('xpath=..')
      .locator('p.text-2xl');

    // Weekly change value lives inside a nested flex div (alongside the trend icon)
    this.weeklyChangeValue = page
      .getByText('Weekly Change', { exact: true })
      .locator('xpath=..')
      .locator('p.text-2xl');

    this.currentBmiValue = page
      .getByText('Current BMI', { exact: true })
      .locator('xpath=..')
      .locator('p.text-2xl');

    this.targetBmiValue = page
      .getByText('Target BMI', { exact: true })
      .locator('xpath=..')
      .locator('p.text-2xl');

    // Goal date uses text-xl, not text-2xl
    this.goalDateValue = page
      .getByText('Estimated Goal Date', { exact: true })
      .locator('xpath=..')
      .locator('p.text-xl');

    // State messages — matched against actual i18n strings (en.json)
    this.noProfileMessage = page.getByText(/set up your biometrics profile/i);
    this.enterCaloriesMessage = page.getByText(/enter a daily calorie target/i);
    this.incompleteProfileMessage = page.getByText(/complete your profile/i);
  }

  /** Navigate to the Profile page where the prediction card lives. */
  async goto() {
    await this.page.goto('/diet-planner/profile');
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * Type a calorie value into the input and wait for the prediction API response.
   * waitForResponse must be registered BEFORE fill() to avoid missing the response.
   */
  async enterCalories(calories: number) {
    const responsePromise = this.page.waitForResponse(
      (r) => r.url().includes('/api/v1/profile/prediction') && r.status() < 500,
      { timeout: 10_000 },
    );
    await this.calorieTargetInput.fill(String(calories));
    await responsePromise;
  }

  /** Assert that all main prediction stat cards are visible. */
  async expectPredictionVisible() {
    await expect(this.bmrValue).toBeVisible();
    await expect(this.tdeeValue).toBeVisible();
    await expect(this.weeklyChangeValue).toBeVisible();
    await expect(this.currentBmiValue).toBeVisible();
  }
}
