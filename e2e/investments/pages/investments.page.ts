import { BasePage } from '../../diet-planner/pages/BasePage';

import type { Locator, Page } from '@playwright/test';

/** `/investments` — net worth dashboard with the wallet list underneath. */
export class InvestmentsPage extends BasePage {
  readonly heading: Locator;
  readonly addWalletButton: Locator;
  readonly updateBalancesLink: Locator;
  readonly walletDialog: Locator;

  constructor(page: Page) {
    super(page);
    this.heading = page.getByRole('heading', { level: 1, name: 'Net worth' });
    this.addWalletButton = page.getByRole('button', { name: 'Add wallet' });
    this.updateBalancesLink = page.getByRole('link', { name: 'Update balances' });
    this.walletDialog = page.getByRole('dialog');
  }

  async goto() {
    await this.page.goto('/investments');
    await this.addWalletButton.waitFor();
  }

  /** The net-worth total (shared test id with `NetWorthSummary.test.tsx`). */
  total(): Locator {
    return this.page.getByTestId('net-worth-total');
  }

  walletLink(name: string): Locator {
    return this.page.getByRole('link', { name, exact: true });
  }

  /** Fills and submits the add-wallet dialog; resolves with the created wallet's id. */
  async addWallet(name: string, kind: string): Promise<string> {
    await this.addWalletButton.click();
    await this.walletDialog.getByLabel('Name', { exact: true }).fill(name);
    await this.walletDialog.getByLabel('Kind').selectOption({ label: kind });
    const created = this.page.waitForResponse(
      (r) => r.request().method() === 'POST' && r.url().endsWith('/api/investments/wallets'),
    );
    await this.walletDialog.getByRole('button', { name: 'Save' }).click();
    const body = (await (await created).json()) as { id: string };
    await this.walletDialog.waitFor({ state: 'hidden' });
    return body.id;
  }
}

/** `/investments/wallets/:id`. */
export class WalletDetailPage extends BasePage {
  readonly amountInput: Locator;
  readonly saveBalanceButton: Locator;
  readonly history: Locator;

  constructor(page: Page) {
    super(page);
    this.amountInput = page.getByLabel(/^Balance \(/);
    this.saveBalanceButton = page.getByRole('button', { name: 'Save balance' });
    this.history = page.getByRole('table');
  }

  async recordBalance(amount: string) {
    await this.amountInput.fill(amount);
    const saved = this.page.waitForResponse(
      (r) => r.request().method() === 'PUT' && r.url().includes('/snapshots/'),
    );
    await this.saveBalanceButton.click();
    await saved;
  }
}

/** `/investments/check-in`. */
export class CheckInPage extends BasePage {
  readonly heading: Locator;

  constructor(page: Page) {
    super(page);
    this.heading = page.getByRole('heading', { level: 1, name: 'Monthly check-in' });
  }

  async goto() {
    await this.page.goto('/investments/check-in');
    await this.heading.waitFor();
  }

  balanceFor(walletName: string): Locator {
    return this.page.getByLabel(walletName, { exact: true });
  }

  /** The live "change since last balance" hint, e.g. `+199.50 PLN`. */
  changeHint(pattern: RegExp): Locator {
    return this.page.getByText(pattern);
  }

  saveButton(count: number): Locator {
    return this.page.getByRole('button', {
      name: `Save ${String(count)} balance${count === 1 ? '' : 's'}`,
    });
  }
}
