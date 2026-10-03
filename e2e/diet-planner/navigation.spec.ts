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
  await nav.productsMobileLink.click();
  await expect(page).toHaveURL('/diet-planner/products');

  await nav.mobileModuleSwitcher.click();
  await nav.householdMenuItem.click();
  await expect(page).toHaveURL('/household');
  await expect(nav.householdMobileNav).toBeVisible();
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
