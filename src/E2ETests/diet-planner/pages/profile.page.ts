import { expect } from '@playwright/test';

import type { Page, Locator } from '@playwright/test';

export class ProfilePage {
  readonly page: Page;
  readonly dateOfBirthInput: Locator;
  readonly genderSelect: Locator;
  readonly heightInput: Locator;
  readonly currentWeightInput: Locator;
  readonly targetWeightInput: Locator;
  readonly activityLevelSelect: Locator;
  readonly saveButton: Locator;
  readonly successMessage: Locator;

  constructor(page: Page) {
    this.page = page;
    this.dateOfBirthInput = page.getByTestId('date-of-birth-picker');
    this.genderSelect = page.getByLabel(/^gender$/i);
    this.heightInput = page.getByLabel(/height/i);
    this.currentWeightInput = page.getByLabel(/current weight/i);
    this.targetWeightInput = page.getByLabel(/target weight/i);
    this.activityLevelSelect = page.getByLabel(/activity level/i);
    this.saveButton = page.getByRole('button', { name: /save profile/i });
    this.successMessage = page.getByText(/profile saved successfully/i);
  }

  async goto() {
    await this.page.goto('/diet-planner/profile');
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * Open the DatePicker identified by testId and navigate to the given ISO date
   * using the month/year dropdowns, then click the day button.
   */
  private async selectDate(testId: string, dateStr: string) {
    const [yearStr, monthStr, dayStr] = dateStr.split('-');
    const year = parseInt(yearStr, 10);
    const month = parseInt(monthStr, 10); // 1-based
    const day = parseInt(dayStr, 10);

    await this.page.getByTestId(testId).click();

    // The calendar popover is rendered in a Radix portal
    const popover = this.page.locator('[data-radix-popper-content-wrapper]').last();
    await popover.waitFor({ state: 'visible', timeout: 5000 });

    const selects = popover.locator('select');

    // Identify month vs year select: year options are 4-digit numbers
    const selectCount = await selects.count();
    for (let i = 0; i < selectCount; i++) {
      const sel = selects.nth(i);
      const firstValue = await sel.locator('option').first().getAttribute('value');
      if (firstValue && firstValue.length === 4) {
        // Year select
        await sel.selectOption(String(year));
      } else {
        // Month select — react-day-picker uses 0-based month index values
        await sel.selectOption(String(month - 1));
      }
    }

    // Click the day by text content — aria-label is the full date string in react-day-picker v9
    await popover
      .locator('button')
      .filter({ hasText: new RegExp(`^\\s*${String(day)}\\s*$`) })
      .first()
      .click();

    // Wait for the popover to close
    await popover.waitFor({ state: 'hidden', timeout: 3000 }).catch(() => undefined);
  }

  async fillForm(data: {
    dateOfBirth?: string;
    gender?: 'Male' | 'Female' | 'Other';
    heightCm?: number;
    currentWeightKg?: number;
    targetWeightKg?: number;
    activityLevel?: 'Sedentary' | 'LightlyActive' | 'ModeratelyActive' | 'VeryActive' | 'ExtraActive';
  }) {
    if (data.dateOfBirth !== undefined) {
      await this.selectDate('date-of-birth-picker', data.dateOfBirth);
    }
    if (data.gender !== undefined) {
      await this.genderSelect.selectOption(data.gender);
    }
    if (data.heightCm !== undefined) {
      await this.heightInput.fill(String(data.heightCm));
    }
    if (data.currentWeightKg !== undefined) {
      await this.currentWeightInput.fill(String(data.currentWeightKg));
    }
    if (data.targetWeightKg !== undefined) {
      await this.targetWeightInput.fill(String(data.targetWeightKg));
    }
    if (data.activityLevel !== undefined) {
      await this.activityLevelSelect.selectOption(data.activityLevel);
    }
  }

  async save() {
    await this.saveButton.click();
    await expect(this.successMessage).toBeVisible({ timeout: 10000 });
  }

  async expectFieldValue(field: 'heightCm' | 'currentWeightKg' | 'targetWeightKg', value: number) {
    const locatorMap = {
      heightCm: this.heightInput,
      currentWeightKg: this.currentWeightInput,
      targetWeightKg: this.targetWeightInput,
    };
    await expect(locatorMap[field]).toHaveValue(String(value));
  }
}
