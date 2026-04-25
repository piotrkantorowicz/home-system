import fs from 'fs';
import path from 'path';

import { request } from '@playwright/test';

interface OidcUser {
  access_token: string;
}

interface StorageState {
  origins?: Array<{
    origin: string;
    localStorage?: Array<{ name: string; value: string }>;
  }>;
}

function extractAccessToken(authStatePath: string): string | null {
  if (!fs.existsSync(authStatePath)) return null;

  let authState: StorageState;
  try {
    authState = JSON.parse(fs.readFileSync(authStatePath, 'utf-8')) as StorageState;
  } catch {
    return null;
  }

  // Search all origins — the app origin must come before Authentik in the
  // match, so we scan every origin rather than stopping at the first
  // localhost hit (which may be http://localhost:9000).
  for (const origin of authState.origins ?? []) {
    const storageItem = (origin.localStorage ?? []).find((i) =>
      i.name.startsWith('oidc.user:'),
    );
    if (storageItem) {
      try {
        return (JSON.parse(storageItem.value) as OidcUser).access_token;
      } catch {
        return null;
      }
    }
  }

  return null;
}

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
