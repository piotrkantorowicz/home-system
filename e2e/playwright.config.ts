import { defineConfig, devices } from '@playwright/test';
import { config as loadDotenv } from 'dotenv';

// Load `e2e/.env` before the test runner reads any process.env values.
// TEST_USER_PASSWORD is required — see e2e/.env.example. `quiet: true`
// silences dotenv's per-worker "tips" noise.
loadDotenv({ quiet: true });

const isCi = !!process.env.CI;

export default defineConfig({
  testDir: '.',
  fullyParallel: true,
  forbidOnly: isCi,
  retries: isCi ? 2 : 0,
  // Each worker owns a dedicated Authentik user (E2eWorker0..3). Local dev
  // runs all four in parallel; CI stays at 2 because the runner (2 vCPU /
  // 7 GB) is already carrying Authentik + Postgres + Redis + the backend +
  // Vite + four Chromium instances, and over-subscription produced timing
  // flakes in early runs (see #141 phase 1).
  workers: isCi ? 2 : 4,
  reporter: [['html', { open: 'never' }], ['list']],
  globalTeardown: './shared/global-teardown.ts',
  timeout: 60000,

  // `expect(...).toBeVisible()` and friends use this timeout, distinct from
  // actionTimeout. Default is 5s, which is too tight on CI when a page is
  // waiting for 2+ API calls AND Vite's first-time lazy-chunk compile.
  expect: {
    timeout: isCi ? 15_000 : 5_000,
  },

  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    // CI runner is slower than local — give every action 2× the headroom.
    actionTimeout: isCi ? 30_000 : 15_000,
    navigationTimeout: isCi ? 60_000 : 30_000,
  },

  projects: [
    { name: 'setup', testMatch: /.*\.setup\.ts/ },
    {
      name: 'chromium',
      // storageState is resolved per-worker inside auth.fixture.ts using
      // testInfo.workerIndex, so there is no global setting here.
      use: { ...devices['Desktop Chrome'] },
      dependencies: ['setup'],
    },
  ],

  webServer: {
    command: 'npm --prefix ../src/ui run dev',
    url: 'http://localhost:5173',
    reuseExistingServer: !process.env.CI,
  },
});
