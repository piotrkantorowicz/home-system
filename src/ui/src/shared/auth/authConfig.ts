import { WebStorageStateStore } from 'oidc-client-ts';

import type { UserManagerSettings } from 'oidc-client-ts';

// Deploy-time settings from /config.js (public/config.js, replaced by a ConfigMap in the cluster),
// so one image serves every environment. They win over the build-time VITE_* variables.
interface RuntimeConfig {
  authentikDomain?: string;
  authentikApp?: string;
  oidcClientId?: string;
  redirectUri?: string;
  /** Browser tracing (src/shared/telemetry); off unless true. */
  tracingEnabled?: boolean;
}

declare global {
  interface Window {
    __CONFIG__?: RuntimeConfig;
  }
}

const runtimeConfig: RuntimeConfig = window.__CONFIG__ ?? {};

function requireEnv(name: string, runtimeValue: string | undefined, fallback?: string): string {
  const raw: unknown = import.meta.env[name];
  const value = runtimeValue ?? (typeof raw === 'string' ? raw : undefined) ?? fallback;
  if (!value) {
    throw new Error(`Missing required environment variable: ${name}`);
  }
  return value;
}

const AUTHENTIK_DOMAIN = requireEnv(
  'VITE_AUTHENTIK_DOMAIN',
  runtimeConfig.authentikDomain,
  import.meta.env.DEV ? 'http://localhost:9000' : undefined,
);
const AUTHENTIK_APP = runtimeConfig.authentikApp ?? 'home-system';
const CLIENT_ID = requireEnv(
  'VITE_OIDC_CLIENT_ID',
  runtimeConfig.oidcClientId,
  import.meta.env.DEV ? 'I476Ik4ahckZS00sx9zmad8ennJdhDr7Fb1LpoMH' : undefined,
);
const REDIRECT_URI = requireEnv(
  'VITE_REDIRECT_URI',
  runtimeConfig.redirectUri,
  import.meta.env.DEV ? 'http://localhost:5173' : undefined,
);

export const oidcConfig: UserManagerSettings = {
  authority: `${AUTHENTIK_DOMAIN}/application/o/${AUTHENTIK_APP}/`,
  client_id: CLIENT_ID,
  redirect_uri: `${REDIRECT_URI}/callback`,
  post_logout_redirect_uri: REDIRECT_URI,
  response_type: 'code',
  // offline_access requests a refresh token from Authentik.
  // When available, signinSilent() uses the refresh token endpoint directly
  // instead of an iframe — no dependency on the Authentik session cookie.
  // roles: custom Authentik scope that emits the `roles` claim (admin pages).
  scope: 'openid profile email offline_access roles',

  // Token storage
  userStore: new WebStorageStateStore({ store: window.localStorage }),

  // Silent renewal via refresh token (no iframe needed)
  automaticSilentRenew: true,

  // Start renewal 120s before token expires (default 60s is too tight)
  accessTokenExpiringNotificationTimeInSeconds: 120,

  // Revoke refresh token on logout for security
  revokeTokensOnSignout: true,
  // Authentik's revocation endpoint does not return CORS headers. For the
  // local stack, use the existing same-origin Vite proxy for this endpoint.
  ...(import.meta.env.DEV && AUTHENTIK_DOMAIN === 'http://localhost:9000'
    ? {
        metadataSeed: {
          revocation_endpoint: `${window.location.origin}/authentik/application/o/revoke/`,
        },
      }
    : {}),

  // Token settings
  loadUserInfo: true,

  // PKCE is automatically enabled for public clients
};
