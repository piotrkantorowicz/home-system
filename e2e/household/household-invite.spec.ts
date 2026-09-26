import { inviteeAuthStatePath, inviteeInvitationEmail } from '../shared/auth-paths';

import { test, expect } from './fixtures';
import { HouseholdPage } from './pages/household.page';
import {
  arrangeCleanInvitation,
  ensureNoHousehold,
  joinAsMember,
  seedSharedShoppingListItem,
} from './utils/seed';

import type { Browser } from '@playwright/test';

// Every spec in this file shares the one reserved invitee identity and its
// household membership state — running them concurrently (this suite's
// default) would race two tests over the same account. File-level so the
// invitation and role describes never overlap either.
test.describe.configure({ mode: 'serial' });

test.describe('Household invitations', () => {
  test('owner invites a member, they accept, and both see the shared shopping list', async ({
    page,
    browser,
  }) => {
    const inviteeEmail = inviteeInvitationEmail();

    // `createApiContext` reads the token from the page's own localStorage, which
    // needs a same-origin document loaded first — a fresh context/page starts on
    // `about:blank`, an opaque origin that throws on any storage access.
    const ownerHousehold = new HouseholdPage(page);
    await ownerHousehold.goto();

    const inviteeContext = await browser.newContext({
      storageState: inviteeAuthStatePath(),
    });
    const inviteePage = await inviteeContext.newPage();
    const inviteeHousehold = new HouseholdPage(inviteePage);
    await inviteeHousehold.goto();

    // Arrange: the owner's own seeded household, and a clean invitee (a
    // previous run that crashed between accepting and leaving would
    // otherwise strand them in a household on this run).
    const household = await arrangeCleanInvitation(page, inviteePage, inviteeEmail);
    // Refresh the invitee's page so its household query reflects the leave above,
    // rather than whatever it cached from the first `goto()`.
    await inviteeHousehold.goto();

    try {
      // Act: owner invites the invitee by email, then plans a meal so the
      // household has one known, attributable shopping-list item.
      await ownerHousehold.inviteByEmail(inviteeEmail, 'Adult');

      const { productName } = await seedSharedShoppingListItem(page);

      // The invitee sees the pending invitation and accepts it.
      await inviteeHousehold.goto();
      await inviteeHousehold.acceptInvitationFrom(household.name);

      // Assert: both members now see the same shopping-list item.
      await inviteePage.goto('/diet-planner/shopping-list');
      await expect(inviteePage.getByText(productName)).toBeVisible();

      await page.goto('/diet-planner/shopping-list');
      await expect(page.getByText(productName)).toBeVisible();
    } finally {
      // Cleanup: the invitee leaves so the next run starts from a clean slate.
      await ensureNoHousehold(inviteePage);
      await inviteeContext.close();
    }
  });

  test('a recipient stays out of the app until they accept the invitation', async ({
    page,
    browser,
  }) => {
    const inviteeEmail = inviteeInvitationEmail();

    const ownerHousehold = new HouseholdPage(page);
    await ownerHousehold.goto();

    const inviteeContext = await browser.newContext({
      storageState: inviteeAuthStatePath(),
    });
    const inviteePage = await inviteeContext.newPage();
    const inviteeHousehold = new HouseholdPage(inviteePage);
    await inviteeHousehold.goto();

    const household = await arrangeCleanInvitation(page, inviteePage, inviteeEmail);

    try {
      // Before any invitation exists, HouseholdRequired redirects every other
      // module route back to /household — logging in alone must not be enough
      // to reach diet-planner.
      await inviteePage.goto('/diet-planner/shopping-list');
      await expect(inviteePage).toHaveURL(/\/household\/?$/);

      await ownerHousehold.inviteByEmail(inviteeEmail, 'Adult');

      await inviteeHousehold.goto();
      await inviteeHousehold.acceptInvitationFrom(household.name);

      // Accepting grants real membership, so the same route now loads instead
      // of bouncing back.
      await inviteePage.goto('/diet-planner/shopping-list');
      await expect(inviteePage).toHaveURL(/\/diet-planner\/shopping-list$/);
    } finally {
      await ensureNoHousehold(inviteePage);
      await inviteeContext.close();
    }
  });

  test('a recipient who declines stays out of the app', async ({ page, browser }) => {
    const inviteeEmail = inviteeInvitationEmail();

    const ownerHousehold = new HouseholdPage(page);
    await ownerHousehold.goto();

    const inviteeContext = await browser.newContext({
      storageState: inviteeAuthStatePath(),
    });
    const inviteePage = await inviteeContext.newPage();
    const inviteeHousehold = new HouseholdPage(inviteePage);
    await inviteeHousehold.goto();

    const household = await arrangeCleanInvitation(page, inviteePage, inviteeEmail);

    try {
      await ownerHousehold.inviteByEmail(inviteeEmail, 'Adult');

      await inviteeHousehold.goto();
      await inviteeHousehold.declineInvitationFrom(household.name);

      // Declining leaves the recipient without membership — still redirected
      // out of every other module route.
      await inviteePage.goto('/diet-planner/shopping-list');
      await expect(inviteePage).toHaveURL(/\/household\/?$/);
    } finally {
      await ensureNoHousehold(inviteePage);
      await inviteeContext.close();
    }
  });
});

test.describe('Household roles', () => {
  /** Opens the invitee's household page — a same-origin document the API seed helpers need. */
  async function openInvitee(browser: Browser) {
    const context = await browser.newContext({ storageState: inviteeAuthStatePath() });
    const page = await context.newPage();
    const household = new HouseholdPage(page);
    await household.goto();
    return { context, page, household };
  }

  for (const role of ['Adult', 'Child', 'Guest'] as const) {
    test(`${role} member sees the household without owner controls`, async ({ page, browser }) => {
      const ownerHousehold = new HouseholdPage(page);
      await ownerHousehold.goto();
      const invitee = await openInvitee(browser);

      try {
        const { household, memberName } = await joinAsMember(
          page,
          invitee.page,
          inviteeInvitationEmail(),
          role,
        );

        // The owner manages the member's role.
        await ownerHousehold.goto();
        await expect(ownerHousehold.roleSelectFor(memberName)).toHaveValue(role);

        // The member sees the same household read-only.
        await invitee.household.goto();
        await expect(invitee.household.heading).toHaveText(household.name);
        await expect(invitee.household.memberRow(memberName)).toContainText(role);
        await expect(invitee.household.roleSelects).toHaveCount(0);
        await expect(invitee.household.addMemberButton).toBeHidden();
        await expect(invitee.household.settingsHeading).toBeHidden();
        await expect(invitee.household.deleteButton).toBeHidden();
        await expect(invitee.household.leaveButton).toBeEnabled();
      } finally {
        await ensureNoHousehold(invitee.page);
        await invitee.context.close();
      }
    });
  }

  test('a member promoted to owner gains owner controls', async ({ page, browser }) => {
    const ownerHousehold = new HouseholdPage(page);
    await ownerHousehold.goto();
    const invitee = await openInvitee(browser);

    try {
      const { memberName } = await joinAsMember(
        page,
        invitee.page,
        inviteeInvitationEmail(),
        'Adult',
      );
      await invitee.household.goto();
      await expect(invitee.household.addMemberButton).toBeHidden();

      await ownerHousehold.goto();
      await ownerHousehold.changeRole(memberName, 'Owner');

      await invitee.household.goto();
      await expect(invitee.household.addMemberButton).toBeVisible();
      await expect(invitee.household.settingsHeading).toBeVisible();
      await expect(invitee.household.deleteButton).toBeVisible();
      await expect(invitee.household.roleSelects).not.toHaveCount(0);
    } finally {
      // Two owners now, so the promoted invitee may leave.
      await ensureNoHousehold(invitee.page);
      await invitee.context.close();
    }
  });
});
