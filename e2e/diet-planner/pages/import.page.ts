import { expect } from '@playwright/test';

import { BasePage } from './BasePage';

import type { Page, Locator } from '@playwright/test';

/**
 * The wizard was collapsed from 3 steps to 2 in the #208 redesign: "Continue"
 * now validates automatically (no separate "Validate" click / step-3 screen)
 * — the review step shows the validation result (ready/warning/error banner)
 * directly, with "Import N days" doing the actual import.
 */
export class ImportPage extends BasePage {
  readonly jsonInput: Locator;
  readonly continueButton: Locator;
  readonly importButton: Locator;
  readonly loadSampleButton: Locator;
  readonly reviewDetected: Locator;

  constructor(page: Page) {
    super(page);
    this.jsonInput = page.getByLabel(/diet plan json/i);
    this.continueButton = page.getByRole('button', { name: /^continue$/i });
    this.importButton = page.getByRole('button', { name: /^import \d+ days?$/i });
    this.loadSampleButton = page.getByRole('button', { name: /load sample/i });
    this.reviewDetected = page.getByText('Detected', { exact: true });
  }

  async goto() {
    await this.page.goto('/diet-planner/import');
    await this.waitForPageReady();
  }

  async loadSample() {
    await this.loadSampleButton.click();
  }

  async setJson(json: object | string) {
    const jsonString = typeof json === 'string' ? json : JSON.stringify(json, null, 2);
    await this.jsonInput.fill(jsonString);
  }

  /**
   * Run the full import wizard:
   *  Upload → paste/load JSON, click Continue (auto-validates)
   *  Review → wait for the validation summary, click "Import N days"
   */
  async runImportWizard(json?: object) {
    if (json) {
      await this.setJson(json);
    } else {
      await this.loadSample();
    }

    const validatePromise = this.page.waitForResponse(
      (resp) => resp.url().includes('/meals/validate') && resp.request().method() === 'POST',
      { timeout: 15000 },
    );
    await this.continueButton.click();
    await validatePromise;
    await expect(this.reviewDetected).toBeVisible({ timeout: 10000 });

    const importPromise = this.page.waitForResponse(
      (resp) => resp.url().includes('/meals/import') && resp.request().method() === 'POST',
      { timeout: 30000 },
    );
    await this.importButton.click();
    const response = await importPromise;
    if (!response.ok()) {
      throw new Error(`Import step failed with status ${String(response.status())}`);
    }

    await this.page.waitForURL(/\/diet-planner\/calendar/, { timeout: 15000 });
  }
}
