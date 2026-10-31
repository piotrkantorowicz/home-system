import { BasePage } from './BasePage';

import type { Locator, Page } from '@playwright/test';

/**
 * The assistant side panel, opened from the sidebar (desktop) or header (phones) on any page. With
 * `Assistant:Provider=Fake` the backend plays a scripted model (see
 * `docs/e2e/assistant.md`).
 */
export class AssistantPanel extends BasePage {
  readonly openButton: Locator;
  readonly dialog: Locator;
  readonly conversation: Locator;
  readonly message: Locator;
  readonly sendButton: Locator;
  /** The preview card of the write waiting for Confirm / Cancel. */
  readonly pendingCard: Locator;
  readonly confirmButton: Locator;

  constructor(page: Page) {
    super(page);
    // Sidebar entry on desktop, header icon on phones; only one is visible at a time.
    this.openButton = page
      .getByRole('button', { name: /^(open )?assistant$/i })
      .filter({ visible: true })
      .first();
    this.dialog = page.getByRole('dialog', { name: /^assistant$/i });
    this.conversation = this.dialog.getByRole('log');
    this.message = this.dialog.getByRole('textbox', { name: /message to the assistant/i });
    this.sendButton = this.dialog.getByRole('button', { name: /^send$/i });
    this.pendingCard = this.dialog.getByRole('region', { name: /confirm this change/i });
    this.confirmButton = this.pendingCard.getByRole('button', { name: /^confirm$/i });
  }

  async open() {
    await this.openButton.click();
    await this.message.waitFor();
  }

  async ask(text: string) {
    await this.message.fill(text);
    await this.sendButton.click();
  }

  async close() {
    await this.page.keyboard.press('Escape');
    await this.dialog.waitFor({ state: 'hidden' });
  }
}
