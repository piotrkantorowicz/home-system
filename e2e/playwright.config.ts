import { defineConfig, devices } from '@playwright/test';
import { config as loadDotenv } from 'dotenv';

// Load `e2e/.env` before the test runner reads any process.env values.
// TEST_USER_PASSWORD is required — see e2e/.env.example.
loadDotenv();

const INVITEE_SPECS = /(household-invite|budget-two-adults)\.spec\.ts/;

export default defineConfig({
  testDir: '.',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  // Each worker owns a dedicated Authentik user (E2eWorker0..3) so tests can
  // run fully in parallel without touching each other's data.
  workers: 4,
  reporter: [['html', { open: 'never' }], ['list']],
  globalTeardown: './shared/global-teardown.ts',
  timeout: 60000,

  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    actionTimeout: 15000,
    navigationTimeout: 30_000,
  },

  projects: [
    { name: 'setup', testMatch: /.*\.setup\.ts/ },
    {
      name: 'chromium',
      // storageState is resolved per-worker inside auth.fixture.ts using
      // testInfo.workerIndex, so there is no global setting here.
      use: { ...devices['Desktop Chrome'] },
      testIgnore: INVITEE_SPECS,
      dependencies: ['setup'],
    },
    // Specs that borrow the one reserved invitee identity must never overlap: each file moves
    // that account in and out of households. Chained projects run one file set after the other.
    {
      name: 'chromium-invitee-household',
      use: { ...devices['Desktop Chrome'] },
      testMatch: /household-invite\.spec\.ts/,
      dependencies: ['setup'],
    },
    {
      name: 'chromium-invitee-budget',
      use: { ...devices['Desktop Chrome'] },
      testMatch: /budget-two-adults\.spec\.ts/,
      dependencies: ['chromium-invitee-household'],
    },
  ],

  webServer: {
    command: 'npm --prefix ../src/ui run dev',
    url: 'http://localhost:5173',
    reuseExistingServer: !process.env.CI,
  },
});
