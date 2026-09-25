import { createApiContext } from '../diet-planner/utils/seed';
import { inviteeAuthStatePath, inviteeInvitationEmail } from '../shared/auth-paths';

import { test, expect } from './fixtures';
import { HouseholdPage } from './pages/household.page';
import {
  ensureNoHousehold,
  revokeStalePendingInvitation,
  seedSharedShoppingListItem,
} from './utils/seed';

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
    const ownerApi = await createApiContext(page);
    const mine = await ownerApi.get('/api/households/me');
    expect(mine.ok()).toBe(true);
    const household = (await mine.json()) as { id: string; name: string };
    await revokeStalePendingInvitation(page, household.id, inviteeEmail);
    await ownerApi.dispose();
    await ensureNoHousehold(inviteePage);
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
});
