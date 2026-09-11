import { getValidToken, tryRenewToken, redirectToLogin } from '@shared/api/tokenInterceptor';
import createClient from 'openapi-fetch';

import type { paths } from './generated/schema';

const baseUrl =
  (import.meta.env.VITE_API_BASE_URL as string | undefined) ??
  (import.meta.env.DEV ? 'http://localhost:5050' : '');

export const api = createClient<paths>({
  baseUrl,
  fetch: async (request: Request) => {
    const token = await getValidToken();
    if (token) request.headers.set('Authorization', `Bearer ${token}`);
    const retryRequest = request.clone();
    const response = await fetch(request);
    if (response.status !== 401) return response;
    const renewed = await tryRenewToken();
    if (!renewed) {
      redirectToLogin();
      return response;
    }
    retryRequest.headers.set('Authorization', `Bearer ${renewed}`);
    return fetch(retryRequest);
  },
});

export function checkResponse(result: { response: Response }): void {
  if (!result.response.ok)
    throw Object.assign(new Error(`Household request failed (${String(result.response.status)})`), {
      status: result.response.status,
    });
}
