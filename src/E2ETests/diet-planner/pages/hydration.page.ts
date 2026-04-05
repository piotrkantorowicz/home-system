import { expect } from '@playwright/test';

import type { Page, Locator } from '@playwright/test';

export class HydrationPage {
  readonly page: Page;
  readonly progressBar: Locator;
  readonly dailyTargetInput: Locator;
  readonly glassSizeInput: Locator;
  readonly saveSettingsButton: Locator;
  readonly customAmountInput: Locator;
  readonly addCustomButton: Locator;

  constructor(page: Page) {
    this.page = page;
    this.progressBar = page.getByRole('progressbar');
    this.dailyTargetInput = page.getByLabel(/daily.*target/i);
    this.glassSizeInput = page.getByLabel(/glass size/i);
    this.saveSettingsButton = page.getByRole('button', { name: /save settings/i });
    this.customAmountInput = page.getByPlaceholder(/amount/i);
    this.addCustomButton = page.getByRole('button', { name: /add/i }).last();
  }

  async goto() {
    await this.page.goto('/diet-planner/hydration');
    await this.page.waitForLoadState('networkidle');
  }

  async updateSettings(data: { dailyTargetMl?: number; glassSizeMl?: number }) {
    if (data.dailyTargetMl !== undefined) {
      await this.dailyTargetInput.fill(String(data.dailyTargetMl));
    }
    if (data.glassSizeMl !== undefined) {
      await this.glassSizeInput.fill(String(data.glassSizeMl));
    }
    await this.saveSettingsButton.click();
  }

  async expectProgressBarVisible() {
    await expect(this.progressBar).toBeVisible();
  }

  async expectFieldValue(locator: Locator, value: number) {
    await expect(locator).toHaveValue(String(value));
  }
}
