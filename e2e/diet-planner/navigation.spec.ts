import { expect, test } from './fixtures';
import { NavigationPage } from './pages/navigation.page';

test('desktop navigation switches modules and finds destinations', async ({ page }) => {
  const nav = new NavigationPage(page);
  await nav.goto();

  await expect(nav.productsSectionLink).toBeVisible();

  // Footer destinations (Notifications) are pages, not modules: `/` returns to the last product module.
  await nav.waitForLastModule('diet-planner');
  await nav.notificationsLink.click();
  await expect(page).toHaveURL('/notifications');
  await page.goto('/');
  await expect(page).toHaveURL('/diet-planner');

  await page.keyboard.press('Control+k');
  await expect(nav.palette).toBeVisible();
  await nav.paletteSearch.fill('Products');
  await nav.productsPaletteOption.click();
  await expect(page).toHaveURL('/diet-planner/products');
});

test('mobile tabs and module switcher navigate between modules', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  const nav = new NavigationPage(page);
  await nav.goto();

  await expect(nav.sidebar).toBeHidden();
  await nav.recipesMobileLink.click();
  await expect(page).toHaveURL('/diet-planner/recipes');

  await nav.mobileModuleSwitcher.click();
  await nav.householdMenuItem.click();
  await expect(page).toHaveURL('/household');
  // Footer pages keep the tabs of the module you came from.
  await expect(nav.mobileNav).toBeVisible();
});

test('mobile More sheet reaches the remaining destinations, traps focus and closes on Escape', async ({
  page,
}) => {
  await page.setViewportSize({ width: 390, height: 844 });
  const nav = new NavigationPage(page);
  await nav.goto();

  for (const name of ['Dashboard', 'Meal plan', 'Recipes', 'Shopping list'])
    await expect(nav.mobileNav.getByRole('link', { name })).toBeVisible();
  await expect(nav.mobileNav.getByRole('link', { name: 'Products' })).toHaveCount(0);

  await nav.moreButton.click();
  await expect(nav.moreSheet).toBeVisible();
  for (let i = 0; i < 25; i++) await page.keyboard.press('Tab');
  await expect(nav.moreSheet.locator(':focus')).toHaveCount(1);
  await page.keyboard.press('Escape');
  await expect(nav.moreSheet).toBeHidden();
  await expect(nav.moreButton).toBeFocused();

  await nav.moreButton.click();
  await nav.moreSheet.getByRole('link', { name: 'Products' }).click();
  await expect(page).toHaveURL('/diet-planner/products');
  await expect(nav.moreSheet).toBeHidden();
  await expect(nav.moreButton).toHaveAttribute('aria-current', 'page');
});

test('keyboard alone opens and closes navigation controls and reaches a destination', async ({
  page,
}) => {
  const nav = new NavigationPage(page);
  await nav.goto();

  await nav.moduleSwitcherTrigger.focus();
  await page.keyboard.press('Enter');
  await expect(nav.switcherMenu).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(nav.switcherMenu).toBeHidden();

  await page.keyboard.press('Control+k');
  await expect(nav.palette).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(nav.palette).toBeHidden();

  await page.keyboard.press('Control+k');
  await nav.paletteSearch.fill('Products');
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL('/diet-planner/products');
});

test('Polish language switches labels and stays usable after reload', async ({ page }) => {
  const nav = new NavigationPage(page);
  await nav.goto();

  await nav.userMenuButton.click();
  await nav.languageSwitcherButton.click();
  await page.keyboard.press('Escape');
  await expect(nav.sectionLink('Produkty')).toBeVisible();

  await page.reload();
  await expect(nav.sectionLink('Produkty')).toBeVisible();

  await nav.userMenuButton.click();
  await nav.preferencesLink.click();
  await expect(page).toHaveURL('/diet-planner/preferences');
});
