import { HouseholdPage } from '../household/pages/household.page';
import { ensureNoHousehold, joinAsMember } from '../household/utils/seed';
import {
  authStatePath,
  credentialsFor,
  inviteeAuthStatePath,
  inviteeCredentials,
  inviteeInvitationEmail,
} from '../shared/auth-paths';
import { loginViaAuthentik } from '../shared/authentik-login';

import { test, expect } from './fixtures';
import { tryRefreshTokens } from './fixtures/auth.fixture';
import { RecipesPage } from './pages';
import { LogoutPage } from './pages/logout.page';
import {
  PHOTO_B,
  deleteRecipe,
  photoStatus,
  putPhoto,
  removePhotoStatus,
  seedPhotoRecipe,
  setRecipeVisibility,
} from './utils/recipe-photos';

import type { Browser, Page } from '@playwright/test';

// The reserved invitee moves in and out of this worker's household, so the tests share state and
// must not overlap — the playwright config chains this file after the other invitee specs.
test.describe.configure({ mode: 'serial' });

async function inviteeSession(browser: Browser) {
  await tryRefreshTokens(inviteeAuthStatePath());
  const context = await browser.newContext({ storageState: inviteeAuthStatePath() });
  const page = await context.newPage();
  // `createApiContext` reads the token from the page's localStorage, so load a same-origin page.
  await new HouseholdPage(page).goto();
  return { context, page };
}

function imageRead(page: Page, recipeId: string) {
  return page.waitForResponse(
    (resp) =>
      resp.url().endsWith(`/api/v1/recipes/${recipeId}/image`) && resp.request().method() === 'GET',
  );
}

test.describe('Recipe photo access', () => {
  const stamp = Date.now();

  test('a household guest sees the photo but cannot change it, and loses it with access', async ({
    page,
    browser,
  }) => {
    await new RecipesPage(page).goto();
    const recipe = await seedPhotoRecipe(page, `Shared photo ${stamp}`, 'Household');
    const invitee = await inviteeSession(browser);
    try {
      expect(await putPhoto(page, recipe.id)).toBe(200);
      await joinAsMember(page, invitee.page, inviteeInvitationEmail(), 'Guest');
      const guest = new RecipesPage(invitee.page);

      // A household reader finds the thumbnail and opens the photo.
      await guest.goto();
      await guest.searchFor(recipe.name);
      await expect(guest.thumbnailFor(recipe.name)).toBeVisible();
      await guest.viewRecipe(recipe.name);
      await expect(guest.detailPhoto(recipe.name)).toBeVisible();

      // A Guest gets no controls, and the API refuses the mutations outright.
      await expect(guest.photoInput()).toHaveCount(0);
      await expect(invitee.page.getByRole('button', { name: /remove photo/i })).toHaveCount(0);
      expect(await putPhoto(invitee.page, recipe.id, PHOTO_B)).toBe(403);
      expect(await removePhotoStatus(invitee.page, recipe.id)).toBe(403);
      expect(await photoStatus(page, recipe.id)).toBe(200);

      // Private: the next read is denied, and the cached recipe page drops the photo it showed.
      await invitee.page.goBack();
      await expect(guest.searchInput).toBeVisible();
      await setRecipeVisibility(page, recipe, 'Private');
      expect(await photoStatus(invitee.page, recipe.id)).toBe(404);
      const denied = imageRead(invitee.page, recipe.id);
      await invitee.page.goForward();
      expect((await denied).status()).toBe(404);
      await expect(invitee.page.getByText(/photo unavailable/i)).toBeVisible();
      await expect(guest.detailPhoto(recipe.name)).toHaveCount(0);

      // Back to Household, then the guest leaves: as an unrelated user, a copied URL is denied.
      await setRecipeVisibility(page, recipe, 'Household');
      expect(await photoStatus(invitee.page, recipe.id)).toBe(200);
      await ensureNoHousehold(invitee.page);
      expect(await photoStatus(invitee.page, recipe.id)).toBe(404);
      expect(await photoStatus(invitee.page, recipe.id, 'thumbnail')).toBe(404);
    } finally {
      await ensureNoHousehold(invitee.page);
      await invitee.context.close();
      await deleteRecipe(page, recipe.id);
    }
  });

  test('signing out and in as another member shows none of the previous user’s photo', async ({
    page,
    browser,
  }, testInfo) => {
    test.setTimeout(120_000); // two interactive Authentik logins
    await new RecipesPage(page).goto();
    const recipe = await seedPhotoRecipe(page, `Session photo ${stamp}`, 'Private');
    const invitee = await inviteeSession(browser);
    // Copy the session without the fixture's init script, which would reinsert the owner's
    // tokens on every navigation after logout (see logout.spec.ts).
    const tab = await browser.newContext({ storageState: await page.context().storageState() });
    try {
      expect(await putPhoto(page, recipe.id)).toBe(200);
      await joinAsMember(page, invitee.page, inviteeInvitationEmail(), 'Adult');
      const browserTab = await tab.newPage();
      const recipes = new RecipesPage(browserTab);
      await recipes.gotoRecipe(recipe.id, recipe.name);
      await expect(recipes.detailPhoto(recipe.name)).toBeVisible();

      const logout = new LogoutPage(browserTab);
      await logout.logout();
      await logout.expectLoggedOut();
      await logout.logoutOfAuthentik();
      const { username, password } = inviteeCredentials();
      await loginViaAuthentik(browserTab, username, password);

      // Same browser, same household, another identity: the private recipe and its photo are gone.
      await browserTab.goto(`/diet-planner/recipes/${recipe.id}`);
      await expect(browserTab.getByRole('button', { name: /user menu/i })).toBeVisible();
      await expect(browserTab.getByRole('heading', { name: recipe.name })).toHaveCount(0);
      await expect(recipes.detailPhoto(recipe.name)).toHaveCount(0);
      expect(await photoStatus(browserTab, recipe.id)).toBe(404);
      expect(await photoStatus(browserTab, recipe.id, 'thumbnail')).toBe(404);
    } finally {
      await tab.close();
      await ensureNoHousehold(invitee.page);
      await invitee.context.close();
      // Logout revoked the owner's tokens: sign in again, like the fixture's fallback, to clean up.
      // An explicit empty state: a bare newContext() inherits the fixture's (revoked) owner state.
      const owner = await browser.newContext({ storageState: { cookies: [], origins: [] } });
      const ownerPage = await owner.newPage();
      const { username, password } = credentialsFor(testInfo.parallelIndex);
      await loginViaAuthentik(ownerPage, username, password);
      await owner.storageState({ path: authStatePath(testInfo.parallelIndex) });
      await deleteRecipe(ownerPage, recipe.id);
      await owner.close();
    }
  });
});
