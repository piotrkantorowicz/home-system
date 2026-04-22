import type { Locator, Page } from '@playwright/test';

/**
 * Shared base for all page objects in the diet-planner suite.
 * Provides the `page` property and a small set of utilities every POM uses.
 */
export abstract class BasePage {
  constructor(protected readonly page: Page) {}

  /**
   * Wait for the page to settle into a quiescent network state.
   * Use after `goto()` calls to avoid asserting on partially-rendered UI.
   */
  async waitForPageReady(): Promise<void> {
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * The toast / status region used by the app for transient feedback.
   * The diet-planner UI renders toasts inside an element with `role="status"`.
   */
  getToast(): Locator {
    return this.page.getByRole('status');
  }
}
