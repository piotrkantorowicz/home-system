import { test, expect } from './fixtures';
import { ProfilePage } from './pages/profile.page';
import { WeightPredictionPage } from './pages/weight-prediction.page';

test.describe('Weight prediction', () => {
  test('prediction card is visible on the dashboard', async ({ page }) => {
    const predictionPage = new WeightPredictionPage(page);
    await predictionPage.goto();

    await expect(predictionPage.calorieTargetInput).toBeVisible();
  });

  test('shows prompt to enter calories when input is empty', async ({ page }) => {
    const predictionPage = new WeightPredictionPage(page);
    await predictionPage.goto();

    // Clear the input — it may be prefilled from goals if goals are configured
    await predictionPage.calorieTargetInput.fill('');

    // With profile + empty input, the prompt should appear
    await expect(predictionPage.enterCaloriesMessage).toBeVisible();
  });

  test('displays BMR, TDEE and weekly change after entering calorie target', async ({ page }) => {
    const profilePage = new ProfilePage(page);
    await profilePage.goto();

    // Ensure a complete profile exists so predictions can be calculated
    await profilePage.fillForm({
      dateOfBirth: '1990-05-15',
      gender: 'Male',
      heightCm: 180,
      currentWeightKg: 80,
      targetWeightKg: 75,
      activityLevel: 'ModeratelyActive',
    });
    await profilePage.save();

    const predictionPage = new WeightPredictionPage(page);
    await predictionPage.goto();
    await predictionPage.enterCalories(2000);

    await predictionPage.expectPredictionVisible();
  });

  test('BMR and TDEE are positive numeric values', async ({ page }) => {
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

    const predictionPage = new WeightPredictionPage(page);
    await predictionPage.goto();
    await predictionPage.enterCalories(2000);

    const bmrText = await predictionPage.bmrValue.textContent();
    const tdeeText = await predictionPage.tdeeValue.textContent();

    expect(Number(bmrText?.trim())).toBeGreaterThan(0);
    expect(Number(tdeeText?.trim())).toBeGreaterThan(0);
  });

  test('shows weight loss trend when calorie target is below TDEE', async ({ page }) => {
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

    const predictionPage = new WeightPredictionPage(page);
    await predictionPage.goto();
    // 1500 kcal is well below TDEE for this profile — should show a deficit
    await predictionPage.enterCalories(1500);

    await predictionPage.expectPredictionVisible();

    // Weekly change for a deficit should show a minus prefix
    const weeklyText = await predictionPage.weeklyChangeValue.textContent();
    expect(weeklyText).toContain('−');
  });

  test('shows weight gain trend when calorie target is above TDEE', async ({ page }) => {
    const profilePage = new ProfilePage(page);
    await profilePage.goto();

    await profilePage.fillForm({
      dateOfBirth: '1990-05-15',
      gender: 'Male',
      heightCm: 180,
      currentWeightKg: 80,
      targetWeightKg: 85,
      activityLevel: 'ModeratelyActive',
    });
    await profilePage.save();

    const predictionPage = new WeightPredictionPage(page);
    await predictionPage.goto();
    // 3500 kcal is well above TDEE for this profile — should show a surplus
    await predictionPage.enterCalories(3500);

    await predictionPage.expectPredictionVisible();

    const weeklyText = await predictionPage.weeklyChangeValue.textContent();
    expect(weeklyText).toContain('+');
  });

  test('shows estimated goal date when current and target weights differ', async ({ page }) => {
    const profilePage = new ProfilePage(page);
    await profilePage.goto();

    await profilePage.fillForm({
      dateOfBirth: '1990-05-15',
      gender: 'Male',
      heightCm: 180,
      currentWeightKg: 90,
      targetWeightKg: 80,
      activityLevel: 'ModeratelyActive',
    });
    await profilePage.save();

    const predictionPage = new WeightPredictionPage(page);
    await predictionPage.goto();
    await predictionPage.enterCalories(1800);

    await expect(predictionPage.goalDateValue).toBeVisible();
    const dateText = await predictionPage.goalDateValue.textContent();
    expect(dateText?.trim().length).toBeGreaterThan(0);
  });

  test('prediction updates when calorie target is changed', async ({ page }) => {
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

    const predictionPage = new WeightPredictionPage(page);
    await predictionPage.goto();

    await predictionPage.enterCalories(1500);
    const weeklyTextLow = await predictionPage.weeklyChangeValue.textContent();

    await predictionPage.enterCalories(2500);
    const weeklyTextHigh = await predictionPage.weeklyChangeValue.textContent();

    // Different calorie targets should produce different weekly change values
    expect(weeklyTextLow).not.toBe(weeklyTextHigh);
  });

  test('returns 404 and shows incomplete-profile message when profile lacks required fields', async ({
    page,
  }) => {
    // Intercept the prediction request and return 404
    await page.route('**/api/v1/profile/prediction*', (route) =>
      route.fulfill({ status: 404, body: '' }),
    );

    const predictionPage = new WeightPredictionPage(page);
    await predictionPage.goto();

    await predictionPage.enterCalories(2000);

    await expect(predictionPage.incompleteProfileMessage).toBeVisible();
  });
});
