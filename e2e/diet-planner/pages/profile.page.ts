import { expect } from '@playwright/test';

import { BasePage } from './BasePage';
import { gotoProfileSection } from './profile-hub.helper';

import type { Page, Locator } from '@playwright/test';

export class ProfilePage extends BasePage {
  readonly dateOfBirthInput: Locator;
  readonly genderSelect: Locator;
  readonly heightInput: Locator;
  readonly currentWeightInput: Locator;
  readonly targetWeightInput: Locator;
  readonly activityLevelSelect: Locator;
  readonly saveButton: Locator;
  readonly successMessage: Locator;
  readonly fieldError: Locator;

  constructor(page: Page) {
    super(page);
    this.dateOfBirthInput = page.getByTestId('date-of-birth-picker');
    this.genderSelect = page.getByLabel(/^gender$/i);
    this.heightInput = page.getByLabel(/height/i);
    this.currentWeightInput = page.getByLabel(/current weight/i);
    this.targetWeightInput = page.getByLabel(/target weight/i);
    this.activityLevelSelect = page.getByLabel(/activity level/i);
    this.saveButton = page.getByRole('button', { name: /save profile/i });
    this.successMessage = page.getByText(/profile saved successfully/i);
    this.fieldError = page.getByRole('alert');
  }

  async goto() {
    await gotoProfileSection(this.page, 'body-stats');
  }

  /**
   * Open the DatePicker identified by testId and navigate to the given ISO date
   * using the month/year dropdowns, then click the day button.
   */
  private async selectDate(testId: string, dateStr: string) {
    const trigger = this.page.getByTestId(testId);

    // If the date is already set to the desired value, skip the calendar
    // interaction entirely. In react-day-picker v9 with mode="single",
    // clicking an already-selected day DESELECTS it — which would clear
    // the value instead of keeping it.
    const currentValue = await trigger.getAttribute('data-value');
    if (currentValue === dateStr) {
      return;
    }

    const [yearStr, monthStr, dayStr] = dateStr.split('-');
    const year = parseInt(yearStr, 10);
    const month = parseInt(monthStr, 10); // 1-based
    const day = parseInt(dayStr, 10);

    await trigger.click();

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
        await sel.selectOption(String(year));
      } else {
        // Month select — react-day-picker uses 0-based month index values
        await sel.selectOption(String(month - 1));
      }
    }

    // Verify dropdown selections took effect (React re-render completed)
    // before clicking the day button, to avoid clicking a stale DOM node.
    for (let i = 0; i < selectCount; i++) {
      const sel = selects.nth(i);
      const firstValue = await sel.locator('option').first().getAttribute('value');
      if (firstValue && firstValue.length === 4) {
        await expect(sel).toHaveValue(String(year));
      } else {
        await expect(sel).toHaveValue(String(month - 1));
      }
    }

    // Click the day button. Use a JS click (evaluate) instead of Playwright's
    // native click because Radix Portal event handling can prevent Playwright's
    // CDP-dispatched pointer events from reaching React's synthetic event system.
    const dayButton = popover
      .locator('button')
      .filter({ hasText: new RegExp(`^\\s*${String(day)}\\s*$`) })
      .first();
    await dayButton.evaluate((node) => (node as HTMLButtonElement).click());

    // DatePicker has no auto-close logic — close the popover explicitly
    // so it doesn't interfere with subsequent form interactions.
    await this.page.keyboard.press('Escape');
    await popover.waitFor({ state: 'hidden', timeout: 3000 }).catch(() => undefined);

    // Verify the trigger's data-value attribute reflects the selected date.
    // This confirms the React state update propagated successfully.
    await expect(trigger).toHaveAttribute('data-value', dateStr, {
      timeout: 5000,
    });
  }

  async fillForm(data: {
    dateOfBirth?: string;
    gender?: 'Male' | 'Female' | 'Other';
    heightCm?: number;
    currentWeightKg?: number;
    targetWeightKg?: number;
    activityLevel?:
      'Sedentary' | 'LightlyActive' | 'ModeratelyActive' | 'VeryActive' | 'ExtraActive';
  }) {
    // Fill non-date fields first. Interacting with form controls can trigger
    // TanStack Query's refetchOnWindowFocus, which fires useEffect → reset()
    // and clears all form values. By filling dateOfBirth LAST, we minimise
    // the window between setting it and clicking Save.
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

    // Fill dateOfBirth last — the calendar popover interaction is complex and
    // the value is vulnerable to being cleared by a profile query refetch.
    if (data.dateOfBirth !== undefined) {
      await this.selectDate('date-of-birth-picker', data.dateOfBirth);
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
