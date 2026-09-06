import { test, expect } from './fixtures';
import { ProfilePage, WeightPredictionPage } from './pages';

// Serial: every test reads/writes the same per-user profile + goals record
// (Profile → Overview's "Energy model" card has no per-test scoping), so
// parallel execution would race and flake.
test.describe.configure({ mode: 'serial' });

test.describe('Weight prediction (Profile overview → Energy model)', () => {
  test.beforeEach(async ({ page }) => {
    // Seed a complete biometrics profile before every test — every test
    // below only varies the calorie goal on top of it.
    const profilePage = new ProfilePage(page);
    await profilePage.goto();
    await profilePage.fillForm({
      dateOfBirth: '1990-05-15',
      gender: 'Male',
      heightCm: 180,
      currentWeightKg: 80,
      targetWeightKg: 75,
      activityLevel: 'ModeratelyActive',
    });
    await profilePage.save();
  });

  test('shows the empty-state hint when no calorie goal is set', async ({ page }) => {
    // A goal is usually already set for the worker user (cross-spec state),
    // so mock the goals endpoint as "not configured" to exercise the branch
    // deterministically.
    await page.route('**/api/v1/goals', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ status: 200, contentType: 'application/json', body: 'null' });
      }
      return route.continue();
    });

    const predictionPage = new WeightPredictionPage(page);
    await predictionPage.goto();

    await expect(predictionPage.energyModelEmptyMessage).toBeVisible();
  });

  test('displays BMR, TDEE and weekly change once a calorie goal is set', async ({ page }) => {
    const predictionPage = new WeightPredictionPage(page);
    await predictionPage.setDailyCalorieTarget(2000);
    await predictionPage.goto();

    await predictionPage.expectEnergyModelVisible();
  });

  test('BMR and TDEE are positive numeric values', async ({ page }) => {
    const predictionPage = new WeightPredictionPage(page);
    await predictionPage.setDailyCalorieTarget(2000);
    await predictionPage.goto();

    // formatNumber() inserts a U+2009 thin-space thousands separator, so strip
    // everything but digits before parsing.
    const toInt = (s: string | null) => Number((s ?? '').replace(/[^\d]/g, ''));
    expect(toInt(await predictionPage.bmrValue.textContent())).toBeGreaterThan(0);
    expect(toInt(await predictionPage.tdeeValue.textContent())).toBeGreaterThan(0);
  });

  test('shows a weight-loss trend when the calorie goal is below TDEE', async ({ page }) => {
    const predictionPage = new WeightPredictionPage(page);
    // 1500 kcal is well below TDEE for this profile — should show a deficit.
    await predictionPage.setDailyCalorieTarget(1500);
    await predictionPage.goto();
    await predictionPage.expectEnergyModelVisible();

    const weeklyText = await predictionPage.weeklyChangeValue.textContent();
    expect(weeklyText).toContain('−');
  });

  test('shows a weight-gain trend when the calorie goal is above TDEE', async ({ page }) => {
    const predictionPage = new WeightPredictionPage(page);
    // 3500 kcal is well above TDEE for this profile — should show a surplus.
    await predictionPage.setDailyCalorieTarget(3500);
    await predictionPage.goto();
    await predictionPage.expectEnergyModelVisible();

    const weeklyText = await predictionPage.weeklyChangeValue.textContent();
    expect(weeklyText).toContain('+');
  });

  test('the prediction changes when the calorie goal changes', async ({ page }) => {
    const predictionPage = new WeightPredictionPage(page);

    await predictionPage.setDailyCalorieTarget(1500);
    await predictionPage.goto();
    const weeklyTextLow = await predictionPage.weeklyChangeValue.textContent();

    await predictionPage.setDailyCalorieTarget(2500);
    await predictionPage.goto();
    const weeklyTextHigh = await predictionPage.weeklyChangeValue.textContent();

    expect(weeklyTextLow).not.toBe(weeklyTextHigh);
  });
});
