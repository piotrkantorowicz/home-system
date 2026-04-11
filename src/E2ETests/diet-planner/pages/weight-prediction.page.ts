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
  readonly loadingSpinner: Locator;

  constructor(page: Page) {
    this.page = page;
    this.calorieTargetInput = page.getByLabel(/daily calorie target/i);
    // Stat cards — located by their uppercase label text
    this.bmrValue = page
      .locator('.rounded-lg.border')
      .filter({ hasText: /^BMR$/i })
      .locator('p.text-2xl');
    this.tdeeValue = page
      .locator('.rounded-lg.border')
      .filter({ hasText: /^TDEE$/i })
      .locator('p.text-2xl');
    this.weeklyChangeValue = page
      .locator('.rounded-lg.border')
      .filter({ hasText: /weekly change/i })
      .locator('p.text-2xl');
    this.currentBmiValue = page
      .locator('.rounded-lg.border')
      .filter({ hasText: /current bmi/i })
      .locator('p.text-2xl');
    this.targetBmiValue = page
      .locator('.rounded-lg.border')
      .filter({ hasText: /target bmi/i })
      .locator('p.text-2xl');
    this.goalDateValue = page
      .locator('.rounded-lg.border')
      .filter({ hasText: /estimated goal date/i })
      .locator('p.text-xl');
    // State messages
    this.noProfileMessage = page.getByText(/complete your profile/i);
    this.enterCaloriesMessage = page.getByText(/enter your daily calorie target/i);
    this.incompleteProfileMessage = page.getByText(/profile is incomplete/i);
    this.loadingSpinner = page.locator('[class*="animate-spin"]');
  }

  /** Navigate to the Profile page where the prediction card lives. */
  async goto() {
    await this.page.goto('/diet-planner/profile');
    await this.page.waitForLoadState('networkidle');
  }

  /** Type a calorie value into the input and wait for the query result. */
  async enterCalories(calories: number) {
    await this.calorieTargetInput.fill(String(calories));
    // Wait for the loading state to resolve
    await this.page.waitForResponse(
      (r) => r.url().includes('/api/v1/profile/prediction') && r.status() < 500,
      { timeout: 10_000 },
    );
  }

  /** Assert that all main prediction stat cards are visible. */
  async expectPredictionVisible() {
    await expect(this.bmrValue).toBeVisible();
    await expect(this.tdeeValue).toBeVisible();
    await expect(this.weeklyChangeValue).toBeVisible();
    await expect(this.currentBmiValue).toBeVisible();
  }
}
