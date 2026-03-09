import type { UserManagerSettings } from 'oidc-client-ts';
import { WebStorageStateStore } from 'oidc-client-ts';

function requireEnv(name: string, fallback?: string): string {
  const value = import.meta.env[name] || fallback;
  if (!value) {
    throw new Error(`Missing required environment variable: ${name}`);
  }
  return value;
}

const AUTHENTIK_DOMAIN = requireEnv(
  'VITE_AUTHENTIK_DOMAIN',
  import.meta.env.DEV ? 'http://localhost:9000' : undefined
);
const CLIENT_ID = requireEnv(
  'VITE_OIDC_CLIENT_ID',
  import.meta.env.DEV ? 'I476Ik4ahckZS00sx9zmad8ennJdhDr7Fb1LpoMH' : undefined
);
const REDIRECT_URI = requireEnv(
  'VITE_REDIRECT_URI',
  import.meta.env.DEV ? 'http://localhost:5173' : undefined
);

export const oidcConfig: UserManagerSettings = {
  authority: `${AUTHENTIK_DOMAIN}/application/o/diet-planner-ui/`,
  client_id: CLIENT_ID,
  redirect_uri: `${REDIRECT_URI}/callback`,
  post_logout_redirect_uri: REDIRECT_URI,
  response_type: 'code',
  // offline_access requests a refresh token from Authentik.
  // When available, signinSilent() uses the refresh token endpoint directly
  // instead of an iframe — no dependency on the Authentik session cookie.
  scope: 'openid profile email offline_access',

  // Token storage
  userStore: new WebStorageStateStore({ store: window.localStorage }),

  // Silent renewal via refresh token (no iframe needed)
  automaticSilentRenew: true,

  // Start renewal 120s before token expires (default 60s is too tight)
  accessTokenExpiringNotificationTimeInSeconds: 120,

  // Revoke refresh token on logout for security
  revokeTokensOnSignout: true,

  // Token settings
  loadUserInfo: true,

  // PKCE is automatically enabled for public clients
};

// API configuration
export const API_BASE_URL = requireEnv(
  'VITE_API_BASE_URL',
  import.meta.env.DEV ? 'http://localhost:5000' : undefined
);
