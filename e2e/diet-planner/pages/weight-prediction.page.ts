import { expect } from '@playwright/test';

import { BasePage } from './BasePage';
import { gotoProfileSection } from './profile-hub.helper';

import type { Page, Locator } from '@playwright/test';

/**
 * The interactive "type a hypothetical calorie target and watch BMR/TDEE
 * recompute" calculator (the old WeightPredictionCard, with its own
 * `dailyCalorieTarget` input) was dropped in the #208 redesign — the
 * component still exists in the tree but is no longer imported anywhere. It
 * was replaced by a READ-ONLY "Energy model" card on the Profile overview
 * page, driven entirely by the user's saved `dailyCalorieTarget` goal
 * (Profile → Goals) rather than a free-typed value. See docs/e2e/weight-prediction.md.
 */
export class WeightPredictionPage extends BasePage {
  readonly energyModelCard: Locator;
  readonly energyModelEmptyMessage: Locator;
  readonly bmrValue: Locator;
  readonly tdeeValue: Locator;
  readonly weeklyChangeValue: Locator;
  readonly currentBmiValue: Locator;
  readonly targetBmiValue: Locator;

  constructor(page: Page) {
    super(page);
    this.energyModelCard = page.getByText('Energy model', { exact: true });
    this.energyModelEmptyMessage = page.getByText(/set a daily calorie target/i);
    this.bmrValue = this.tileValue(page, 'BMR');
    this.tdeeValue = this.tileValue(page, 'TDEE');
    this.weeklyChangeValue = this.tileValue(page, 'Weekly change');
    this.currentBmiValue = this.tileValue(page, 'Current BMI');
    this.targetBmiValue = this.tileValue(page, 'Target BMI');
  }

  // MetricTile renders <label div><value div class="numeral">...</value></label div's parent>
  // as two sibling divs — walk up to the shared parent, then into the value div.
  private tileValue(page: Page, label: string): Locator {
    return page.getByText(label, { exact: true }).locator('xpath=..').locator('.numeral');
  }

  /** Navigate to the Profile overview, where the Energy model card now lives. */
  async goto() {
    await this.page.goto('/diet-planner/profile');
    await this.energyModelCard.waitFor();
  }

  /**
   * Set the daily calorie target via Profile → Goals — the only way left to
   * drive the Energy model card (there is no in-place override any more).
   */
  async setDailyCalorieTarget(calories: number) {
    await gotoProfileSection(this.page, 'goals');

    const input = this.page.locator('#dailyCalorieTarget');
    const current = await input.inputValue();
    if (current === String(calories)) return;

    const responsePromise = this.page.waitForResponse(
      (r) =>
        r.url().includes('/api/v1/goals') &&
        (r.request().method() === 'PUT' || r.request().method() === 'POST'),
    );
    await input.fill(String(calories));
    await this.page.getByRole('button', { name: /save goals/i }).click();
    await responsePromise;
  }

  /** Assert the read-only Energy model card is populated. */
  async expectEnergyModelVisible() {
    await expect(this.bmrValue).toBeVisible({ timeout: 15000 });
    await expect(this.tdeeValue).toBeVisible({ timeout: 15000 });
    await expect(this.weeklyChangeValue).toBeVisible({ timeout: 15000 });
    await expect(this.currentBmiValue).toBeVisible({ timeout: 15000 });
  }
}
