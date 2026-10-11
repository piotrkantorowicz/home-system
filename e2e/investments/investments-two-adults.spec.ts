import { tryRefreshTokens } from '../diet-planner/fixtures/auth.fixture';
import { test, expect } from '../diet-planner/fixtures';
import { HouseholdPage } from '../household/pages/household.page';
import { ensureNoHousehold, joinAsMember } from '../household/utils/seed';
import { inviteeAuthStatePath, inviteeInvitationEmail } from '../shared/auth-paths';

import { InvestmentsPage } from './pages/investments.page';
import { archiveTrackedWallets, createWallet, listWallets, netWorth, status } from './utils/seed';

import type { Browser } from '@playwright/test';

// The reserved invitee moves in and out of this worker's household; the playwright config runs
// this file after the other invitee specs so they never overlap.
test.describe.configure({ mode: 'serial' });

async function inviteeSession(browser: Browser) {
  await tryRefreshTokens(inviteeAuthStatePath());
  const context = await browser.newContext({ storageState: inviteeAuthStatePath() });
  const page = await context.newPage();
  // `createApiContext` reads the token from the page's localStorage, so load a same-origin page.
  await new HouseholdPage(page).goto();
  return { context, page };
}

test.describe('Investments with two adults', () => {
  test("an adult's private wallet never reaches the other adult — not by list, id, total or screen", async ({
    page,
    browser,
  }) => {
    const stamp = Date.now().toString(36);
    const shared = `E2E shared ${stamp}`;
    const secret = `E2E secret ${stamp}`;
    await new HouseholdPage(page).goto();
    const invitee = await inviteeSession(browser);
    await joinAsMember(page, invitee.page, inviteeInvitationEmail(), 'Adult');
    try {
      await createWallet(page, shared, { balance: '100' });
      const secretId = await createWallet(invitee.page, secret, {
        visibility: 'Private',
        kind: 'IKZE',
        balance: '777',
      });

      // The invitee sees both; the owner sees only the shared one.
      expect((await listWallets(invitee.page)).items.map((w) => w.name)).toEqual(
        expect.arrayContaining([shared, secret]),
      );
      expect((await listWallets(page)).items.map((w) => w.name)).not.toContain(secret);
      expect(await status(page, `/api/investments/wallets/${secretId}`)).toBe(404);
      expect(await status(page, `/api/investments/wallets/${secretId}/snapshots`)).toBe(404);
      expect((await netWorth(page)).wallets.map((w) => w.id)).not.toContain(secretId);
      expect((await netWorth(invitee.page)).wallets.map((w) => w.id)).toContain(secretId);

      // On screen too.
      const investments = new InvestmentsPage(page);
      await investments.goto();
      await expect(investments.walletLink(shared)).toBeVisible();
      await expect(investments.walletLink(secret)).toHaveCount(0);
    } finally {
      await archiveTrackedWallets(page);
      await archiveTrackedWallets(invitee.page);
      await ensureNoHousehold(invitee.page);
      await invitee.context.close();
    }
  });
});
