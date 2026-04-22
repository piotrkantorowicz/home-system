import { expect } from '@playwright/test';

import { BasePage } from './BasePage';

import type { Page, Locator } from '@playwright/test';

export class HydrationPage extends BasePage {
  readonly progressBar: Locator;

  constructor(page: Page) {
    super(page);
    this.progressBar = page.locator('div:has(> [role="progressbar"])');
  }

  async goto() {
    await this.page.goto('/diet-planner/hydration');
    await this.waitForPageReady();
  }

  async expectProgressBarVisible() {
    await expect(this.progressBar).toBeVisible();
  }
}
