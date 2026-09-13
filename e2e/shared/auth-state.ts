import fs from 'fs';

interface OidcUser {
  access_token: string;
}

interface StorageState {
  origins?: Array<{
    origin: string;
    localStorage?: Array<{ name: string; value: string }>;
  }>;
}

/**
 * Reads the OIDC access token out of a worker's saved storage-state file.
 * Shared by the household seed (setup) and the purge teardown so both talk to
 * the API as the same Authentik user the browser is logged in as.
 */
export function extractAccessToken(authStatePath: string): string | null {
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
    const storageItem = (origin.localStorage ?? []).find((i) => i.name.startsWith('oidc.user:'));
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
