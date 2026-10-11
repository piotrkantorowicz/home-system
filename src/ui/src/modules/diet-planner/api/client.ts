import { getValidToken, tryRenewToken, redirectToLogin } from '@shared/api/tokenInterceptor';
import createClient from 'openapi-fetch';

import type { paths } from './generated/schema';

const baseUrl: string =
  (import.meta.env.VITE_API_BASE_URL as string | undefined) ??
  (import.meta.env.DEV ? 'http://localhost:5050' : '');

const DEBUG = import.meta.env.DEV;

// Create the base API client
const baseClient = createClient<paths>({ baseUrl });
const retryRequests = new WeakMap<Request, Request>();

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
    // Keep multipart bytes readable after fetch consumes the original request body.
    retryRequests.set(request, request.clone());
    return request;
  },
  async onResponse({ request, response }) {
    const savedRequest = retryRequests.get(request);
    retryRequests.delete(request);
    if (response.status === 401) {
      console.warn('[auth] got 401, attempting renewal before redirect...');

      const newToken = await tryRenewToken();

      if (newToken) {
        if (DEBUG) console.warn('[auth] renewal after 401 succeeded, retrying request');
        const retryRequest = savedRequest ?? request.clone();
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
