import { expect } from '@playwright/test';

import { BasePage } from './BasePage';
import { gotoProfileSection } from './profile-hub.helper';

import type { Page, Locator } from '@playwright/test';

export class MealSchedulePage extends BasePage {
  readonly addSlotButton: Locator;
  readonly saveButton: Locator;
  readonly successMessage: Locator;

  constructor(page: Page) {
    super(page);
    this.addSlotButton = page.getByRole('button', { name: /add slot/i });
    this.saveButton = page.getByRole('button', { name: /save schedule/i });
    this.successMessage = page.getByText(/saved successfully/i);
  }

  async goto() {
    await gotoProfileSection(this.page, 'meal-schedule');
  }

  slotNameInput(index: number): Locator {
    return this.page.locator(`#slot-${String(index)}-name`);
  }

  slotTimeInput(index: number): Locator {
    return this.page.locator(`#slot-${String(index)}-time`);
  }

  slotRemoveButton(index: number): Locator {
    return this.page.getByRole('button', { name: /remove slot/i }).nth(index);
  }

  async slotCount(): Promise<number> {
    return this.page.locator('input[id^="slot-"][id$="-name"]').count();
  }

  async addSlot(name: string, time: string) {
    await this.addSlotButton.click();
    const idx = (await this.slotCount()) - 1;
    await this.slotNameInput(idx).fill(name);
    await this.slotTimeInput(idx).fill(time);
  }

  async save() {
    await this.saveButton.click();
  }

  async expectSaveButtonDisabled() {
    await expect(this.saveButton).toBeDisabled();
  }

  async expectSaveButtonEnabled() {
    await expect(this.saveButton).toBeEnabled();
  }
}
