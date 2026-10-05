import { expect } from '@playwright/test';

import { BasePage } from './BasePage';

import type { Page, Locator } from '@playwright/test';

export class HydrationPage extends BasePage {
  /** Today's progress bar — only rendered once a daily goal is set (see Hydration.tsx). */
  readonly levelMeter: Locator;
  readonly addGlassButton: Locator;
  readonly customButton: Locator;
  readonly customNoteInput: Locator;
  readonly customAddButton: Locator;
  readonly deleteEntryButtons: Locator;
  readonly confirmRemoveButton: Locator;

  constructor(page: Page) {
    super(page);
    this.levelMeter = page.getByRole('progressbar', { name: "Today's water progress" });
    this.addGlassButton = page.getByRole('button', { name: /^\+250 ml/ });
    this.customButton = page.getByRole('button', { name: 'Other…' });
    this.customNoteInput = page.getByPlaceholder('e.g. morning glass');
    this.customAddButton = page.getByRole('button', { name: 'Add', exact: true });
    this.deleteEntryButtons = page.getByRole('button', { name: 'Delete entry' });
    this.confirmRemoveButton = page.getByRole('button', { name: 'Remove', exact: true });
  }

  async goto() {
    await this.page.goto('/diet-planner/hydration');
    await this.page.getByRole('heading', { level: 1 }).waitFor();
  }

  async expectLevelMeterVisible() {
    await expect(this.levelMeter).toBeVisible();
  }

  entryNote(note: string) {
    return this.page.getByText(note, { exact: true });
  }

  /** Today's total, read from the progress bar (capped at the daily goal). */
  expectProgress(amountMl: number, goalMl: number) {
    return expect(this.levelMeter).toHaveAttribute(
      'aria-valuenow',
      String(Math.min(amountMl, goalMl)),
    );
  }
}
