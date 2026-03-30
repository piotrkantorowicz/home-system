import { userManager } from '@shared/auth/userManager';

const DEBUG = import.meta.env.DEV;

// Renew token if it expires within this many seconds
const TOKEN_EXPIRY_BUFFER_SECONDS = 30;

let isRenewing = false;
let renewPromise: Promise<string | null> | null = null;

/**
 * Attempt silent token renewal via the shared UserManager.
 * Deduplicates concurrent renewal attempts.
 */
export async function tryRenewToken(): Promise<string | null> {
  if (isRenewing && renewPromise) {
    if (DEBUG) console.warn('[auth] renewal already in progress, waiting...');
    return renewPromise;
  }

  const user = await userManager.getUser();
  if (DEBUG) {
    console.warn('[auth] attempting token renewal', {
      hasRefreshToken: !!user?.refresh_token,
      expired: user?.expired,
      expiresAt: user?.expires_at
        ? new Date(user.expires_at * 1000).toLocaleTimeString()
        : 'unknown',
    });
  }

  isRenewing = true;
  renewPromise = userManager
    .signinSilent()
    .then((renewedUser) => {
      if (DEBUG) {
        console.warn('[auth] renewal succeeded', {
          hasNewRefreshToken: !!renewedUser?.refresh_token,
          expiresAt: renewedUser?.expires_at
            ? new Date(renewedUser.expires_at * 1000).toLocaleTimeString()
            : 'unknown',
        });
      }
      return renewedUser?.access_token ?? null;
    })
    .catch((err: unknown) => {
      const msg = err instanceof Error ? err.message : String(err);
      console.warn('[auth] renewal failed:', msg);
      return null;
    })
    .finally(() => {
      isRenewing = false;
      renewPromise = null;
    });

  return renewPromise;
}

/**
 * Check if the token is expired or will expire within the buffer window.
 */
function isTokenExpiringSoon(expiresAt: number | undefined): boolean {
  if (!expiresAt) return true;
  const now = Math.floor(Date.now() / 1000);
  return expiresAt - now < TOKEN_EXPIRY_BUFFER_SECONDS;
}

/**
 * Get a valid access token, renewing proactively if needed.
 */
export async function getValidToken(): Promise<string | null> {
  const user = await userManager.getUser();

  if (user?.access_token && !isTokenExpiringSoon(user.expires_at)) {
    return user.access_token;
  }

  if (DEBUG) {
    const ttl = user?.expires_at ? user.expires_at - Math.floor(Date.now() / 1000) : -1;
    console.warn('[auth] token expiring soon or missing, proactive renewal', {
      ttlSeconds: ttl,
      hasUser: !!user,
      hasRefreshToken: !!user?.refresh_token,
    });
  }

  if (user) {
    const newToken = await tryRenewToken();
    if (newToken) return newToken;
  }

  return null;
}

/**
 * Redirect to root so ProtectedRoute triggers a full re-auth.
 * Saves the current URL so AuthCallback can restore it.
 */
export function redirectToLogin(): void {
  const currentPath = window.location.pathname + window.location.search;
  if (currentPath !== '/' && currentPath.startsWith('/') && !currentPath.startsWith('//')) {
    sessionStorage.setItem('returnUrl', currentPath);
  }
  window.location.href = '/';
}
