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

/**
 * Throws an error carrying the HTTP `status` so callers can tell 403/404/409 apart, and the
 * problem body (`error`) when the API sent one, so per-field errors can be shown.
 */
export function checkResponse(result: { response: Response; error?: unknown }): void {
  if (!result.response.ok)
    throw Object.assign(new Error(`Budget request failed (${String(result.response.status)})`), {
      status: result.response.status,
      problem: result.error,
    });
}

export function errorStatus(error: unknown): number | undefined {
  return typeof error === 'object' && error !== null && 'status' in error
    ? (error.status as number)
    : undefined;
}

export function errorProblem(error: unknown): unknown {
  return typeof error === 'object' && error !== null && 'problem' in error
    ? error.problem
    : undefined;
}
