import { expect } from '@playwright/test';

import type { Page, Locator } from '@playwright/test';

export class ImportPage {
  readonly page: Page;
  readonly jsonInput: Locator;
  readonly continueButton: Locator;
  readonly validateButton: Locator;
  readonly importButton: Locator;
  readonly loadSampleButton: Locator;

  constructor(page: Page) {
    this.page = page;
    this.jsonInput = page.getByLabel(/json/i);
    this.continueButton = page.getByRole('button', { name: /continue/i });
    this.validateButton = page.getByRole('button', { name: /validate data|re-validate/i });
    this.importButton = page.getByRole('button', { name: /confirm.*import/i });
    this.loadSampleButton = page.getByRole('button', { name: /load sample/i });
  }

  async goto() {
    await this.page.goto('/diet-planner/import');
    await this.page.waitForLoadState('networkidle');
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
   *  Step 1 → paste/load JSON, click Continue
   *  Step 2 → click Validate, wait for Step 3
   *  Step 3 → click Confirm Import, wait for redirect to /calendar
   */
  async runImportWizard(json?: object) {
    if (json) {
      await this.setJson(json);
    } else {
      await this.loadSample();
    }

    // Step 1 → 2
    await this.continueButton.click();
    await expect(this.page.getByText(/step 2/i)).toBeVisible({ timeout: 10000 });

    // Step 2: Validate
    await this.clickAndWaitForResponse(
      this.validateButton,
      '/meals/validate',
      this.page.getByText(/step 3/i),
    );

    // Step 3: Execute import
    await this.clickAndWaitForResponse(this.importButton, '/meals/import', null);

    await this.page.waitForURL(/\/diet-planner\/calendar/, { timeout: 15000 });
  }

  private async clickAndWaitForResponse(
    button: Locator,
    urlPattern: string,
    successLocator: Locator | null,
  ) {
    const responsePromise = this.page.waitForResponse(
      (resp) => resp.url().includes(urlPattern) && resp.request().method() === 'POST',
      { timeout: 30000 },
    );
    await button.click();
    const response = await responsePromise;

    if (!response.ok()) {
      throw new Error(`Import step failed with status ${String(response.status())}`);
    }

    if (successLocator) {
      await expect(successLocator).toBeVisible({ timeout: 15000 });
    }
  }
}
