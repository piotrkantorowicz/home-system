import { expect } from '@playwright/test';

import type { Page, Locator } from '@playwright/test';

export class HydrationPage {
  readonly page: Page;
  readonly progressBar: Locator;

  constructor(page: Page) {
    this.page = page;
    this.progressBar = page.locator('div:has(> [role="progressbar"])');
  }

  async goto() {
    await this.page.goto('/diet-planner/hydration');
    await this.page.waitForLoadState('networkidle');
  }

  async expectProgressBarVisible() {
    await expect(this.progressBar).toBeVisible();
  }
}
