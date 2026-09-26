import { expect } from '@playwright/test';

import { BasePage } from './BasePage';

import type { Page, Locator } from '@playwright/test';

export class HydrationPage extends BasePage {
  /** The bottle-fill visual — role="meter" + aria-valuenow (see Hydration.tsx). */
  readonly levelMeter: Locator;
  readonly addGlassButton: Locator;
  readonly customButton: Locator;
  readonly customNoteInput: Locator;
  readonly customAddButton: Locator;
  readonly deleteEntryButtons: Locator;
  readonly confirmRemoveButton: Locator;

  constructor(page: Page) {
    super(page);
    this.levelMeter = page.getByRole('meter');
    this.addGlassButton = page.getByRole('button', { name: /\+1 glass \(250 ml\)/i });
    this.customButton = page.getByRole('button', { name: 'Custom' });
    this.customNoteInput = page.getByPlaceholder('e.g. morning glass');
    this.customAddButton = page.getByRole('button', { name: 'Add', exact: true });
    this.deleteEntryButtons = page.getByRole('button', { name: 'Delete entry' });
    this.confirmRemoveButton = page.getByRole('button', { name: 'Remove', exact: true });
  }

  async goto() {
    await this.page.goto('/diet-planner/hydration');
    await this.levelMeter.waitFor();
  }

  async expectLevelMeterVisible() {
    await expect(this.levelMeter).toBeVisible();
  }

  entryNote(note: string) {
    return this.page.getByText(note, { exact: true });
  }

  total(amountMl: number) {
    return this.page.getByText(`${String(amountMl)} ml`, { exact: true });
  }
}
