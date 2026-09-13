import path from 'path';

import { request } from '@playwright/test';

import { extractAccessToken } from '../../shared/auth-state';

export async function cleanupWorker(workerIndex: number): Promise<void> {
  const authStatePath = path.resolve(`playwright/.auth/user-${workerIndex}.json`);
  const token = extractAccessToken(authStatePath);

  if (!token) {
    console.warn(
      `  [worker ${workerIndex}] No usable auth state at ${authStatePath}, skipping cleanup`,
    );
    return;
  }

  const apiContext = await request.newContext({
    baseURL: 'http://localhost:5050',
    extraHTTPHeaders: { Authorization: `Bearer ${token}` },
  });

  try {
    const res = await apiContext.delete('/api/v1/test-support/purge-my-data');

    if (res.status() === 204) {
      console.log(`  [worker ${workerIndex}] Purge succeeded (204).`);
    } else if (res.status() === 404) {
      console.warn(
        `  [worker ${workerIndex}] Purge endpoint returned 404 — test-support must be enabled on the backend ` +
          '(ASPNETCORE_ENVIRONMENT=Development or E2ETestSupport:Enabled=true).',
      );
    } else {
      console.warn(`  [worker ${workerIndex}] Purge failed: HTTP ${res.status()}`);
    }
  } catch (error) {
    console.error(`  [worker ${workerIndex}] Error during cleanup:`, error);
  } finally {
    await apiContext.dispose();
  }
}
