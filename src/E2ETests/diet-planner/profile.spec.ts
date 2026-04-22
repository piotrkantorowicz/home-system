import { test, expect } from './fixtures';
import { ProfilePage } from './pages/profile.page';

test.describe('Profile', () => {
  test('profile page loads and shows the form', async ({ page }) => {
    const profilePage = new ProfilePage(page);
    await profilePage.goto();

    await expect(page.getByRole('heading', { name: /my profile/i })).toBeVisible();
    await expect(profilePage.saveButton).toBeVisible();
    await expect(profilePage.heightInput).toBeVisible();
    await expect(profilePage.genderSelect).toBeVisible();
  });

  test('user can save their biometrics profile', async ({ page }) => {
    const profilePage = new ProfilePage(page);
    await profilePage.goto();

    await profilePage.fillForm({
      gender: 'Male',
      heightCm: 180,
      currentWeightKg: 80,
      targetWeightKg: 75,
      activityLevel: 'ModeratelyActive',
    });

    await profilePage.save();
  });

  test('saved profile values are pre-filled on next visit', async ({ page }) => {
    const profilePage = new ProfilePage(page);
    await profilePage.goto();

    await profilePage.fillForm({
      heightCm: 175,
      currentWeightKg: 72,
      targetWeightKg: 68,
      activityLevel: 'LightlyActive',
    });

    await profilePage.save();

    // Navigate away and back
    await page.goto('/diet-planner');
    await profilePage.goto();

    await profilePage.expectFieldValue('heightCm', 175);
    await profilePage.expectFieldValue('currentWeightKg', 72);
    await profilePage.expectFieldValue('targetWeightKg', 68);
  });

  test('user can update an existing profile', async ({ page }) => {
    const profilePage = new ProfilePage(page);
    await profilePage.goto();

    // Initial save
    await profilePage.fillForm({ heightCm: 170, currentWeightKg: 85, targetWeightKg: 80 });
    await profilePage.save();

    // Update
    await profilePage.fillForm({ currentWeightKg: 83, targetWeightKg: 78, activityLevel: 'VeryActive' });
    await profilePage.save();

    await profilePage.expectFieldValue('currentWeightKg', 83);
  });

  test('save button is disabled when form is not dirty', async ({ page }) => {
    const profilePage = new ProfilePage(page);
    await profilePage.goto();

    // Button should be disabled initially (form not dirty)
    await expect(profilePage.saveButton).toBeDisabled();
  });

  test('save button becomes enabled after editing a field', async ({ page }) => {
    const profilePage = new ProfilePage(page);
    await profilePage.goto();

    await profilePage.heightInput.fill('182');

    await expect(profilePage.saveButton).toBeEnabled();
  });
});
