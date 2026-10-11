import { test, expect } from '../diet-planner/fixtures';

import { CheckInPage, InvestmentsPage, WalletDetailPage } from './pages/investments.page';
import {
  archiveTrackedWallets,
  createWallet,
  minor,
  netWorth,
  netWorthHistory,
  plnPattern,
  trackWallet,
} from './utils/seed';

// Each worker owns its own household; wallets made here are archived afterwards so the
// household's total only moves by what this test records.
test.describe('Investments check-in', () => {
  test.afterEach(async ({ page }) => archiveTrackedWallets(page));

  test('create a wallet, record a balance, check in two wallets, and the dashboard reflects them', async ({
    page,
  }) => {
    const stamp = Date.now().toString(36);
    const ike = `E2E IKE ${stamp}`;
    const savings = `E2E savings ${stamp}`;
    const before = minor((await netWorth(page)).total);
    const investments = new InvestmentsPage(page);

    // A wallet created in the UI, with a first balance recorded on its page.
    await investments.goto();
    trackWallet(page, await investments.addWallet(ike, 'IKE'));
    await investments.walletLink(ike).click();
    const detail = new WalletDetailPage(page);
    await detail.recordBalance('1000,50');
    await expect(detail.history).toContainText(plnPattern(100050));

    // A second wallet, then one check-in for both — the same date replaces the first balance.
    await createWallet(page, savings);
    const checkIn = new CheckInPage(page);
    await checkIn.goto();
    await checkIn.balanceFor(ike).fill('1200');
    await checkIn.balanceFor(savings).fill('300,25');
    await expect(checkIn.changeHint(/\+199[.,]50 PLN/)).toBeVisible();
    await checkIn.saveButton(2).click();

    // Back on the dashboard: the total moved by exactly what was recorded, and so did history.
    await expect(page).toHaveURL('/investments');
    const expected = before + 120000 + 30025;
    await expect(investments.total()).toHaveText(plnPattern(expected));
    expect(minor((await netWorth(page)).total)).toBe(expected);
    const points = (await netWorthHistory(page)).points;
    expect(minor(points.at(-1)?.total ?? '0')).toBe(expected);
  });
});
