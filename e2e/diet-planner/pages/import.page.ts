import { expect } from '@playwright/test';

import { BasePage } from './BasePage';

import type { Page, Locator } from '@playwright/test';

/**
 * The import wizard (#500): Choose file → Review → Done. Choosing a file or the
 * sample plan validates straight away; pasted JSON sits behind "Paste JSON instead"
 * and is checked with Continue. The review step shows counts, warnings and
 * "Import N meals"; the done step offers "Open meal plan".
 */
export class ImportPage extends BasePage {
  readonly jsonInput: Locator;
  readonly continueButton: Locator;
  readonly importButton: Locator;
  readonly loadSampleButton: Locator;
  /** Present on the review step only. */
  readonly reviewDetected: Locator;
  readonly uploadButton: Locator;
  readonly previousButton: Locator;
  readonly openPlanLink: Locator;

  constructor(page: Page) {
    super(page);
    this.jsonInput = page.getByLabel(/diet plan json/i);
    this.continueButton = page.getByRole('button', { name: /^continue$/i });
    this.importButton = page.getByRole('button', { name: /^import \d+ meals?$/i });
    this.loadSampleButton = page.getByRole('button', { name: /try the sample plan/i });
    this.reviewDetected = page.getByRole('button', { name: /^choose another file$/i });
    this.uploadButton = page.getByRole('button', { name: /^choose file$/i });
    this.previousButton = page.getByRole('button', { name: /^back$/i });
    this.openPlanLink = page.getByRole('link', { name: /^open meal plan$/i });
  }

  async goto() {
    await this.page.goto('/diet-planner/import');
    await this.uploadButton.waitFor();
  }

  /** Expand the "Paste JSON instead" section. */
  async openPaste() {
    if (!(await this.jsonInput.isVisible())) {
      await this.page.getByText('Paste JSON instead', { exact: true }).click();
    }
  }

  /** The sample plan is validated straight away and lands on the review step. */
  async loadSample() {
    await this.loadSampleButton.click();
    await expect(this.reviewDetected).toBeVisible({ timeout: 10000 });
  }

  async setJson(json: object | string) {
    const jsonString = typeof json === 'string' ? json : JSON.stringify(json, null, 2);
    await this.openPaste();
    await this.jsonInput.fill(jsonString);
  }

  /** Choose a file; the wizard reads it, validates it and shows the review. */
  async uploadJson(json: object) {
    const chooser = this.page.waitForEvent('filechooser');
    await this.uploadButton.click();
    await (
      await chooser
    ).setFiles({
      name: 'diet-plan.json',
      mimeType: 'application/json',
      buffer: Buffer.from(JSON.stringify(json)),
    });
    await expect(this.reviewDetected).toBeVisible({ timeout: 10000 });
  }

  /**
   * Run the full import wizard:
   *  Choose file → paste JSON and Continue (or try the sample), which validates
   *  Review → wait for the counts, click "Import N meals"
   *  Done → open the meal plan
   */
  async runImportWizard(json?: object) {
    const validatePromise = this.page.waitForResponse(
      (resp) => resp.url().includes('/meals/validate') && resp.request().method() === 'POST',
      { timeout: 15000 },
    );
    if (json) {
      await this.setJson(json);
      await this.continueButton.click();
    } else {
      await this.loadSampleButton.click();
    }
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

    await this.openPlanLink.click();
    await this.page.waitForURL(/\/diet-planner\/calendar/, { timeout: 15000 });
  }
}
