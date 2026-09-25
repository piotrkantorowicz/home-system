import { inviteeAuthStatePath, inviteeInvitationEmail } from '../shared/auth-paths';

import { test, expect } from './fixtures';
import { HouseholdPage } from './pages/household.page';
import { arrangeCleanInvitation, ensureNoHousehold, seedSharedShoppingListItem } from './utils/seed';

test.describe('Household invitations', () => {
  // All three specs share the one reserved invitee identity and its household
  // membership state — running them concurrently (this suite's default) would
  // race two tests over the same account.
  test.describe.configure({ mode: 'serial' });

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
