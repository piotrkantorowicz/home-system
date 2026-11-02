import { test, expect } from '../diet-planner/fixtures';

import { InvestmentsPage, WalletDetailPage } from './pages/investments.page';
import { archiveTrackedWallets, minor, netWorth, plnPattern, trackWallet } from './utils/seed';

// Wallets made here are archived afterwards, which drops them and their deposits from net worth.
test.describe('Investments invested capital', () => {
  test.afterEach(async ({ page }) => archiveTrackedWallets(page));

  test('record a balance and a deposit, and the dashboard shows invested and gain', async ({
    page,
  }) => {
    const name = `E2E broker ${Date.now().toString(36)}`;
    const investments = new InvestmentsPage(page);
    await investments.goto();
    const before = await netWorth(page);

    // A wallet with a balance of 1000,50 PLN, 800 of which was deposited.
    const id = await investments.addWallet(name, 'Brokerage');
    trackWallet(page, id);
    await investments.walletLink(name).click();
    const detail = new WalletDetailPage(page);
    await detail.recordBalance('1000,50');
    await detail.recordDeposit('800');
    await expect(detail.contributions).toContainText(plnPattern(80000));

    // The API counts the deposit for this wallet and in the household totals.
    const after = await netWorth(page);
    const wallet = after.wallets.find((w) => w.id === id);
    expect(wallet?.invested).toBe('800.00');
    expect(minor(wallet?.gainPln ?? '0')).toBe(20050);
    expect(minor(after.invested ?? '0')).toBe(minor(before.invested ?? '0') + 80000);

    // The dashboard shows the same numbers, and the wallet's row its own gain.
    await investments.goto();
    await expect(investments.invested()).toHaveText(plnPattern(minor(after.invested ?? '0')));
    await expect(investments.gain()).toContainText(plnPattern(minor(after.gain ?? '0')));
    await expect(investments.walletRow(name)).toContainText(plnPattern(20050));
  });
});
