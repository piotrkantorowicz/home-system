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
    this.dateOfBirthInput = page.getByLabel(/date of birth/i);
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

  async fillForm(data: {
    dateOfBirth?: string;
    gender?: 'Male' | 'Female' | 'Other';
    heightCm?: number;
    currentWeightKg?: number;
    targetWeightKg?: number;
    activityLevel?: 'Sedentary' | 'LightlyActive' | 'ModeratelyActive' | 'VeryActive' | 'ExtraActive';
  }) {
    if (data.dateOfBirth !== undefined) {
      await this.dateOfBirthInput.fill(data.dateOfBirth);
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
