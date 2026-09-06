import { expect } from '@playwright/test';

import { BasePage } from './BasePage';

import type { Page, Locator } from '@playwright/test';

export class HydrationPage extends BasePage {
  /** The bottle-fill visual — role="meter" + aria-valuenow (see Hydration.tsx). */
  readonly levelMeter: Locator;

  constructor(page: Page) {
    super(page);
    this.levelMeter = page.getByRole('meter');
  }

  async goto() {
    await this.page.goto('/diet-planner/hydration');
    await this.waitForPageReady();
  }

  async expectLevelMeterVisible() {
    await expect(this.levelMeter).toBeVisible();
  }
}
