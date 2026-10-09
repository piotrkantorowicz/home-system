import { tryRefreshTokens } from '../diet-planner/fixtures/auth.fixture';
import { test, expect } from '../diet-planner/fixtures';
import { HouseholdPage } from '../household/pages/household.page';
import { ensureNoHousehold, joinAsMember } from '../household/utils/seed';
import { inviteeAuthStatePath, inviteeInvitationEmail } from '../shared/auth-paths';

import {
  archiveTrackedEnvelopes,
  createEnvelope,
  createExpense,
  ensureBudget,
  get,
  minor,
  myPersonId,
  send,
  today,
} from './utils/seed';

import type { Browser, Page } from '@playwright/test';

// The reserved invitee moves in and out of this worker's household, so the tests share state and
// must not overlap — and the playwright config keeps this file apart from household-invite.spec.ts.
test.describe.configure({ mode: 'serial' });

interface Account {
  id: string;
  name: string;
}
interface ExpenseList {
  items: { id: string; amount: string }[];
}
interface Settlement {
  balances: { personId: string; net: string }[];
}

/** A random amount with an even number of cents (so a two-way split is exact), unique per run. */
function uniqueAmount(base: number) {
  const cents = String(10 + 2 * Math.floor(Math.random() * 44));
  return `${String(base + Math.floor(Math.random() * 40))}.${cents}`;
}

async function inviteeSession(browser: Browser) {
  await tryRefreshTokens(inviteeAuthStatePath());
  const context = await browser.newContext({ storageState: inviteeAuthStatePath() });
  const page = await context.newPage();
  // `createApiContext` reads the token from the page's localStorage, so load a same-origin page.
  await new HouseholdPage(page).goto();
  return { context, page };
}

/** Records whatever the invitee still owes (or is owed) so the next run starts at zero. */
async function settleUp(owner: Page, inviteeId: string, ownerId: string) {
  const { status, body } = await get<Settlement>(owner, '/api/budget/settlement');
  if (status !== 200) return;
  const net = body.balances.find((b) => b.personId === inviteeId)?.net;
  if (!net || minor(net) === 0) return;
  const owes = net.startsWith('-');
  const code = await send(owner, 'post', '/api/budget/settlements', {
    clientRequestId: crypto.randomUUID(),
    fromPersonId: owes ? inviteeId : ownerId,
    toPersonId: owes ? ownerId : inviteeId,
    amount: net.replace('-', ''),
    paidOn: today(),
    note: 'e2e cleanup',
  });
  expect(code).toBeLessThan(300);
}

test.describe('Budget with two adults', () => {
  test.afterEach(async ({ page }) => archiveTrackedEnvelopes(page));

  test('adults share entries, never see each other’s private ones, and a partial repayment survives reload', async ({
    page,
    browser,
  }) => {
    const stamp = Date.now().toString(36);
    const shared = uniqueAmount(40);
    const ownerPrivate = uniqueAmount(70);
    const inviteePrivate = uniqueAmount(20);
    await new HouseholdPage(page).goto();
    const invitee = await inviteeSession(browser);
    await joinAsMember(page, invitee.page, inviteeInvitationEmail(), 'Adult');
    const ownerId = await myPersonId(page);
    const inviteeId = await myPersonId(invitee.page);
    try {
      await ensureBudget(page);
      await settleUp(page, inviteeId, ownerId);

      const sharedEnvelope = await createEnvelope(page, `Shared ${stamp}`, 'Household');
      const ownerSecret = await createEnvelope(page, `Owner secret ${stamp}`, 'Personal', ownerId);
      const inviteeSecret = await createEnvelope(
        invitee.page,
        `Invitee secret ${stamp}`,
        'Personal',
        inviteeId,
      );
      await createExpense(page, {
        accountId: sharedEnvelope,
        amount: shared,
        category: 'Groceries',
        fundingSource: 'Individual',
        paidByPersonId: ownerId,
        participantIds: [ownerId, inviteeId],
      });
      const ownerSecretExpense = await createExpense(page, {
        accountId: ownerSecret,
        amount: ownerPrivate,
        category: 'Leisure',
        fundingSource: 'Individual',
        paidByPersonId: ownerId,
      });
      const inviteeSecretExpense = await createExpense(invitee.page, {
        accountId: inviteeSecret,
        amount: inviteePrivate,
        category: 'Leisure',
        fundingSource: 'Individual',
        paidByPersonId: inviteeId,
      });

      // Neither adult can find the other's private envelope or expense — by list or by id.
      const inviteeAccounts = await get<{ items: Account[] }>(
        invitee.page,
        '/api/budget/accounts?includeArchived=true&pageSize=100',
      );
      expect(inviteeAccounts.body.items.map((a) => a.name)).toContain(`Shared ${stamp}`);
      expect(inviteeAccounts.body.items.map((a) => a.name)).not.toContain(`Owner secret ${stamp}`);
      const ownerAccounts = await get<{ items: Account[] }>(
        page,
        '/api/budget/accounts?includeArchived=true&pageSize=100',
      );
      expect(ownerAccounts.body.items.map((a) => a.name)).not.toContain(`Invitee secret ${stamp}`);
      expect((await get(invitee.page, `/api/budget/accounts/${ownerSecret}`)).status).toBe(404);
      expect((await get(page, `/api/budget/accounts/${inviteeSecret}`)).status).toBe(404);
      expect(
        (await get(invitee.page, `/api/budget/expenses/${ownerSecretExpense.expenseId}`)).status,
      ).toBe(404);
      expect(
        (await get(page, `/api/budget/expenses/${inviteeSecretExpense.expenseId}`)).status,
      ).toBe(404);
      const inviteeExpenses = await get<ExpenseList>(
        invitee.page,
        '/api/budget/expenses?pageSize=100',
      );
      expect(inviteeExpenses.body.items.map((e) => e.amount)).not.toContain(ownerPrivate);

      // The same entries on screen: the shared one for both, each private one for its owner only.
      await page.goto('/budget/expenses');
      await expect(
        page.getByRole('link', { name: new RegExp(`${shared.replace('.', '\\.')}`) }).first(),
      ).toBeVisible();
      await expect(
        page.getByRole('link', { name: new RegExp(`${ownerPrivate.replace('.', '\\.')}`) }).first(),
      ).toBeVisible();
      await expect(
        page.getByRole('link', { name: new RegExp(`${inviteePrivate.replace('.', '\\.')}`) }),
      ).toHaveCount(0);
      await invitee.page.goto('/budget/expenses');
      await expect(
        invitee.page
          .getByRole('link', { name: new RegExp(`${shared.replace('.', '\\.')}`) })
          .first(),
      ).toBeVisible();
      await expect(
        invitee.page
          .getByRole('link', { name: new RegExp(`${inviteePrivate.replace('.', '\\.')}`) })
          .first(),
      ).toBeVisible();
      await expect(
        invitee.page.getByRole('link', {
          name: new RegExp(`${ownerPrivate.replace('.', '\\.')}`),
        }),
      ).toHaveCount(0);

      // Duplicate hints use the same visibility rules: a similar shared entry is flagged, a
      // similar private one belonging to the other adult is not.
      await page.getByRole('button', { name: 'Add expense' }).click();
      const dialog = page.getByRole('dialog');
      await dialog.getByText('Groceries', { exact: true }).click();
      await dialog.getByLabel(/^Amount/).fill(shared);
      await expect(dialog.getByText(/similar expense.* already exist/)).toBeVisible();
      await dialog.getByText('Leisure', { exact: true }).click();
      await dialog.getByLabel(/^Amount/).fill(inviteePrivate);
      await expect(dialog.getByText(/similar expense.* already exist/)).toBeHidden();
      await dialog.getByRole('button', { name: 'Cancel' }).first().click();

      // The invitee owes half of the shared expense. A partial repayment is recorded in the UI…
      const half = (minor(shared) / 2 / 100).toFixed(2);
      await invitee.page.goto('/budget/settlement');
      await expect(
        invitee.page.getByText(new RegExp(`owes\\s+${half.replace('.', '\\.')}`)),
      ).toBeVisible();
      await invitee.page.getByRole('button', { name: 'Record a different amount' }).click();
      const pay = invitee.page.getByRole('dialog');
      await pay.getByLabel(/^Amount/).fill('5');
      await pay.getByRole('button', { name: 'Review' }).click();
      await expect(pay.getByText(/doesn't move money/)).toBeVisible();
      await pay.getByRole('button', { name: 'Confirm payment' }).click();
      await expect(pay).toBeHidden();

      // …and survives a reload for both adults.
      const remaining = ((minor(shared) / 2 - 500) / 100).toFixed(2);
      await invitee.page.reload();
      await expect(
        invitee.page.getByText(new RegExp(`owes\\s+${remaining.replace('.', '\\.')}`)),
      ).toBeVisible();
      await expect(invitee.page.getByText(/paid .* 5\.00 PLN/).first()).toBeVisible();
      await page.goto('/budget/settlement');
      await expect(
        page.getByText(new RegExp(`is owed\\s+${remaining.replace('.', '\\.')}`)),
      ).toBeVisible();
    } finally {
      await settleUp(page, inviteeId, ownerId);
      await archiveTrackedEnvelopes(invitee.page);
      await ensureNoHousehold(invitee.page);
      await invitee.context.close();
    }
  });

  test('changing or removing an adult revokes their Budget access on their next request', async ({
    page,
    browser,
  }) => {
    const stamp = Date.now().toString(36);
    await new HouseholdPage(page).goto();
    const invitee = await inviteeSession(browser);
    const { household } = await joinAsMember(page, invitee.page, inviteeInvitationEmail(), 'Adult');
    const ownerId = await myPersonId(page);
    const inviteeId = await myPersonId(invitee.page);
    try {
      await ensureBudget(page);
      await settleUp(page, inviteeId, ownerId);
      const sharedEnvelope = await createEnvelope(page, `Revoke ${stamp}`, 'Household');
      const own = await createEnvelope(invitee.page, `Mine ${stamp}`, 'Personal', inviteeId);
      expect((await get(invitee.page, `/api/budget/accounts/${sharedEnvelope}`)).status).toBe(200);
      expect((await get(invitee.page, '/api/budget/settlement')).status).toBe(200);

      // Demoted to Child: the shared ledger closes at once, their own envelope stays theirs.
      expect(
        await send(page, 'put', `/api/households/${household.id}/members/${inviteeId}/role`, {
          role: 'Child',
        }),
      ).toBe(204);
      expect((await get(invitee.page, '/api/budget/settlement')).status).toBe(403);
      expect(
        (await get(invitee.page, `/api/budget/accounts/${sharedEnvelope}`)).status,
      ).toBeGreaterThanOrEqual(403);
      expect((await get(invitee.page, `/api/budget/accounts/${own}`)).status).toBe(200);

      // Removed: no Budget data at all.
      expect(
        await send(page, 'delete', `/api/households/${household.id}/members/${inviteeId}`),
      ).toBeLessThan(300);
      const after = await get(invitee.page, '/api/budget/accounts');
      expect([403, 404]).toContain(after.status);
      expect(
        (await get(invitee.page, `/api/budget/accounts/${own}`)).status,
      ).toBeGreaterThanOrEqual(403);
    } finally {
      await archiveTrackedEnvelopes(invitee.page);
      await ensureNoHousehold(invitee.page);
      await invitee.context.close();
    }
  });
});
