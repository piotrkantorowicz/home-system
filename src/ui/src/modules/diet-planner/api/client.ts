import { getValidToken, tryRenewToken, redirectToLogin } from '@shared/api/tokenInterceptor';
import createClient from 'openapi-fetch';

import type { paths } from './generated/schema';

const baseUrl: string =
  (import.meta.env.VITE_API_BASE_URL as string | undefined) ??
  (import.meta.env.DEV ? 'http://localhost:5050' : '');

const DEBUG = import.meta.env.DEV;

// Create the base API client
const baseClient = createClient<paths>({ baseUrl });

// Add request interceptor for authentication
baseClient.use({
  async onRequest({ request }) {
    try {
      const token = await getValidToken();
      if (token) {
        request.headers.set('Authorization', `Bearer ${token}`);
      }
    } catch (error) {
      console.error('[auth] failed to get access token:', error);
    }
    return request;
  },
  async onResponse({ request, response }) {
    if (response.status === 401) {
      console.warn('[auth] got 401, attempting renewal before redirect...');

      const newToken = await tryRenewToken();

      if (newToken) {
        if (DEBUG) console.warn('[auth] renewal after 401 succeeded, retrying request');
        const canHaveBody = request.method !== 'GET' && request.method !== 'HEAD';
        const retryRequest = new Request(request.url, {
          method: request.method,
          headers: new Headers(request.headers),
          ...(canHaveBody ? { body: request.body } : {}),
          credentials: request.credentials,
        });
        retryRequest.headers.set('Authorization', `Bearer ${newToken}`);
        return fetch(retryRequest);
      }

      console.error('[auth] all renewal attempts failed, redirecting to login');
      redirectToLogin();
    }
    return response;
  },
});

export const api = baseClient;
