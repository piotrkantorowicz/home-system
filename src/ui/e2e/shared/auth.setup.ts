import { test as setup, expect } from '@playwright/test';

const authFile = 'playwright/.auth/user.json';

setup('authenticate', async ({ page }) => {
  console.log('Starting authentication setup...');

  // Enable more detailed logging
  page.on('console', (msg) => { console.log('PAGE LOG:', msg.text()); });

  await page.goto('/');

  // 1. Handle Username
  console.log('Waiting for username field...');
  const usernameInput = page.locator('input[name="uidField"]');
  await usernameInput.waitFor({ state: 'visible', timeout: 10000 });
  await usernameInput.fill('E2eTestsUser');
  console.log('Username filled.');

  // 2. Click Continue/Submit to go to password page (Authentik uses multi-stage)
  console.log('Clicking Continue to proceed to password page...');
  const firstSubmitButton = page.locator('button[type="submit"]').first();
  await firstSubmitButton.click();

  console.log('Waiting for password field to appear...');
  // Wait for navigation to password page
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(1000);

  // 3. Fill password field
  console.log('Looking for password field...');
  const passwordInput = page.locator('input[type="password"]').first();
  await passwordInput.waitFor({ state: 'visible', timeout: 10000 });

  console.log('Password field found. Filling with password...');
  // Try filling multiple times to ensure it works
  await passwordInput.click(); // Focus the field first
  await passwordInput.fill(''); // Clear any existing value
  await passwordInput.fill('Password321!');

  // Verify the value was set
  const passwordValue = await passwordInput.inputValue();
  console.log('Password field value length:', passwordValue.length);

  if (passwordValue !== 'Password321!') {
    console.log('Password not set correctly, trying again with type...');
    await passwordInput.clear();
    await passwordInput.pressSequentially('Password321!', { delay: 50 });
  }

  console.log('Password filled successfully.');

  // 4. Submit the password form
  console.log('Submitting password form...');
  const submitButton = page.locator('button[type="submit"]').first();
  await submitButton.click();

  // 5. Handle Post-Login (App Selection / Redirect / Consent)
  console.log('Waiting for post-login redirect...');
  await page.waitForLoadState('networkidle', { timeout: 10000 });

  const currentUrl = page.url();
  console.log('Current URL after login:', currentUrl);

  if (currentUrl.includes(':9000')) {
    console.log('Still on Authentik (port 9000). Checking for app selection or consent...');

    // Wait a moment for any UI to render
    await page.waitForTimeout(1000);

    // Check for various possible post-login screens
    // 1. App selection screen (multiple apps)
    const appLink = page.getByText('Diet Planner', { exact: false }).first();

    // 2. Consent/Authorization screen
    const consentButton = page
      .getByRole('button', { name: /accept|continue|authorize|allow/i })
      .first();

    // 3. Generic continue button
    const continueButton = page.getByRole('button', { name: /continue/i }).first();

    if (await appLink.isVisible({ timeout: 2000 }).catch(() => false)) {
      console.log('Found Diet Planner app link, clicking...');
      await appLink.click();
    } else if (await consentButton.isVisible({ timeout: 2000 }).catch(() => false)) {
      console.log('Found consent/authorization button, clicking...');
      await consentButton.click();
    } else if (await continueButton.isVisible({ timeout: 2000 }).catch(() => false)) {
      console.log('Found continue button, clicking...');
      await continueButton.click();
    } else {
      console.log('No additional action needed, waiting for automatic redirect...');
    }
  }

  // 6. Verify Redirect to app
  console.log('Waiting for redirect to localhost:5173...');
  await page.waitForURL('http://localhost:5173/**', { timeout: 30000 });

  console.log('Successfully redirected to app. Current URL:', page.url());

  // 7. Verify Session - wait for the app to load
  console.log('Waiting for app to load...');
  await page.waitForLoadState('networkidle');

  // Check for Dashboard or any authenticated content
  const dashboard = page.getByText('Dashboard', { exact: false }).first();
  await expect(dashboard).toBeVisible({ timeout: 10000 });

  console.log('Authentication successful. Saving state...');
  await page.context().storageState({ path: authFile });
  console.log('Auth state saved to:', authFile);
});
