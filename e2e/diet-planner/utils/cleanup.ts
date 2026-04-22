import fs from 'fs';
import path from 'path';

import { request } from '@playwright/test';

export async function cleanupTestData() {
  const authStatePath = path.resolve('playwright/.auth/user.json');

  if (!fs.existsSync(authStatePath)) {
    console.warn('No auth state found at ' + authStatePath + ', skipping cleanup');
    return;
  }

  try {
    const authState = JSON.parse(fs.readFileSync(authStatePath, 'utf-8'));

    // Search all origins for the OIDC user entry — the app origin must come
    // before Authentik in the match, so we scan every origin rather than
    // stopping at the first localhost hit (which may be http://localhost:9000).
    let storageItem: { name: string; value: string } | undefined;
    for (const origin of authState.origins ?? []) {
      storageItem = (origin.localStorage ?? []).find(
        (i: { name: string; value: string }) => i.name.startsWith('oidc.user:'),
      );
      if (storageItem) break;
    }

    if (!storageItem) {
      console.warn('No OIDC user found in any origin in storage, skipping cleanup');
      return;
    }

    const user = JSON.parse(storageItem.value);
    const token = user.access_token;

    const apiContext = await request.newContext({
      baseURL: 'http://localhost:5000',
      extraHTTPHeaders: {
        Authorization: `Bearer ${token}`,
      },
    });

    console.log('Purging test data via test-support endpoint...');

    const res = await apiContext.delete('/api/v1/test-support/purge-my-data');

    if (res.status() === 204) {
      console.log('  Purge succeeded (204).');
    } else if (res.status() === 404) {
      console.warn(
        '  Purge endpoint returned 404 — test-support must be enabled on the backend ' +
          '(ASPNETCORE_ENVIRONMENT=Development or E2ETestSupport:Enabled=true).',
      );
    } else {
      console.warn(`  Purge failed: HTTP ${res.status()}`);
    }

    await apiContext.dispose();
  } catch (error) {
    console.error('Error during cleanup:', error);
  }
}
