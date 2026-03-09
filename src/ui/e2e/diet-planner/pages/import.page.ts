import { Page, Locator, expect } from '@playwright/test';

export class ImportPage {
  readonly page: Page;
  readonly jsonInput: Locator;
  readonly continueButton: Locator;
  readonly validateButton: Locator;
  readonly nextButton: Locator;
  readonly importButton: Locator;
  readonly loadSampleButton: Locator;
  readonly successMessage: Locator;

  constructor(page: Page) {
    this.page = page;
    this.jsonInput = page.locator('textarea#json-input');
    this.continueButton = page.getByRole('button', { name: /continue/i });
    this.validateButton = page.getByRole('button', { name: /validate data|re-validate/i });
    this.nextButton = page.getByRole('button', { name: /next/i });
    this.importButton = page.getByRole('button', { name: /confirm.*import/i });
    this.loadSampleButton = page.getByRole('button', { name: /load sample/i });
    this.successMessage = page.getByText(/import successful/i);
  }

  async goto() {
    await this.page.goto('/diet-plans/import');
  }

  async loadSample() {
    await this.loadSampleButton.click();
  }

  async setJson(json: object | string) {
    const jsonString = typeof json === 'string' ? json : JSON.stringify(json, null, 2);
    await this.jsonInput.fill(jsonString);
  }

  /**
   * Wait for rate limit window to reset based on retryAfter header or error body.
   */
  private async waitForRateLimitReset(retryAfterSeconds?: number) {
    const waitSeconds = retryAfterSeconds ?? 60;
    console.log(`Rate limited, waiting ${waitSeconds}s before retry...`);
    await this.page.waitForTimeout(waitSeconds * 1000 + 1000);
  }

  async runImportWizard(json?: object) {
    if (json) {
      await this.setJson(json);
    } else {
      await this.loadSample();
    }

    // Step 1 -> 2
    await this.continueButton.click();
    await expect(this.page.getByText('Step 2: Validate Import Data')).toBeVisible();

    // Step 2: Validate — intercept response to detect rate limit
    await this.clickAndHandleRateLimit(
      this.validateButton,
      '/diet-plans/validate',
      this.page.getByText('Step 3: Review & Confirm'),
      this.page.getByRole('button', { name: /re-validate/i })
    );

    // Step 3: Import — intercept response to detect rate limit
    await this.clickAndHandleRateLimit(
      this.importButton,
      '/diet-plans/import',
      null, // no locator to wait for — we wait for URL redirect
      this.importButton
    );

    await this.page.waitForURL('/diet-plans', { timeout: 15000 });
  }

  private async clickAndHandleRateLimit(
    button: Locator,
    urlPattern: string,
    successLocator: Locator | null,
    retryButton: Locator
  ) {
    const responsePromise = this.page.waitForResponse(
      (resp) => resp.url().includes(urlPattern) && resp.request().method() === 'POST',
      { timeout: 30000 }
    );
    await button.click();
    const response = await responsePromise;

    if (response.status() === 429) {
      const retryAfter = parseInt(response.headers()['retry-after'] ?? '60', 10);
      await this.waitForRateLimitReset(retryAfter);

      // Retry
      const retryResponsePromise = this.page.waitForResponse(
        (resp) => resp.url().includes(urlPattern) && resp.request().method() === 'POST',
        { timeout: 30000 }
      );
      await retryButton.click();
      const retryResponse = await retryResponsePromise;
      if (!retryResponse.ok()) {
        throw new Error(`Import step failed after retry: ${retryResponse.status()}`);
      }
    } else if (!response.ok()) {
      throw new Error(`Import step failed: ${response.status()}`);
    }

    if (successLocator) {
      await expect(successLocator).toBeVisible({ timeout: 15000 });
    }
  }
}
