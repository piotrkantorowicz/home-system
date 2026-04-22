import { gotoProfileSection } from './profile-hub.helper';

import type { Page, Locator } from '@playwright/test';

export class HydrationSettingsPage {
  readonly page: Page;
  readonly dailyTargetInput: Locator;
  readonly glassSizeInput: Locator;
  readonly saveSettingsButton: Locator;

  constructor(page: Page) {
    this.page = page;
    this.dailyTargetInput = page.getByLabel(/daily.*target/i);
    this.glassSizeInput = page.getByLabel(/glass size/i);
    this.saveSettingsButton = page.getByRole('button', { name: /save settings/i });
  }

  async goto() {
    await gotoProfileSection(this.page, 'hydration');
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
}
