import { test, expect } from './fixtures';
import { ImportPage } from './pages';
import { generateWeeklyPlan } from './utils/data-generator';
import { seedGoals } from './utils/seed';

test('consumed nutrition, undo, and responsive agenda work with real meals', async ({
  page,
}, testInfo) => {
  test.setTimeout(180000);
  const importer = new ImportPage(page);
  await importer.goto();
  await importer.runImportWizard(generateWeeklyPlan(new Date()));
  // Goals are worker-shared state: another spec may already have set them, in
  // which case the dashboard's "Set up goals" CTA is gone. Seed via the API so
  // the hero renders the nutrition view regardless of ordering.
  await seedGoals(page);
  await page.goto('/diet-planner');
  await expect(page.getByText('Nothing logged', { exact: true })).toBeVisible();
  const mealActions = page.getByRole('button', {
    name: 'Mark eaten',
    exact: true,
  });
  const plannedCount = await mealActions.count();
  expect(plannedCount).toBeGreaterThan(0);
  await mealActions.first().click();
  await expect(page.locator('summary').filter({ hasText: 'Logged meals (1)' })).toBeVisible();
  await expect(page.getByText('Nothing logged', { exact: true })).not.toBeVisible();
  await page.getByRole('button', { name: 'Undo', exact: true }).click();
  await expect(mealActions).toHaveCount(plannedCount);
  await expect(page.getByText('Nothing logged', { exact: true })).toBeVisible();
  for (const dismiss of await page.getByRole('button', { name: 'Dismiss notification' }).all()) {
    await dismiss.click();
  }
  await expect(page.getByRole('button', { name: 'Dismiss notification' })).toHaveCount(0);

  await page.emulateMedia({ reducedMotion: 'reduce' });
  for (const width of [390, 768, 1280, 1600]) {
    await page.setViewportSize({ width, height: 1000 });
    for (const theme of ['light', 'dark']) {
      await page.evaluate((value) => {
        document.documentElement.classList.remove('light', 'dark');
        document.documentElement.classList.add(value);
      }, theme);
      await expect
        .poll(() =>
          page.locator('main').evaluate((element) => element.scrollWidth <= element.clientWidth),
        )
        .toBe(true);
      await page.screenshot({
        path: testInfo.outputPath(`today-${width}-${theme}.png`),
        fullPage: true,
        animations: 'disabled',
      });
    }
  }
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/diet-planner/calendar?date=2026-09-03');
  await expect(page.getByRole('group', { name: 'Choose a day' })).toBeVisible();
  await page.getByRole('button', { name: 'Friday, September 4', exact: true }).click();
  await expect(page).toHaveURL(/date=2026-09-04/);
  await expect(
    page.getByRole('heading', { name: 'Friday, September 4', exact: true }),
  ).toBeVisible();
  await page.screenshot({
    path: testInfo.outputPath('calendar-mobile.png'),
    fullPage: true,
    animations: 'disabled',
  });
  await page.goBack();
  await expect(page).toHaveURL(/date=2026-09-03/);
});
