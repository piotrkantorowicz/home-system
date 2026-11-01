import { getValidToken, tryRenewToken, redirectToLogin } from '@shared/api/tokenInterceptor';

const baseUrl =
  (import.meta.env.VITE_API_BASE_URL as string | undefined) ??
  (import.meta.env.DEV ? 'http://localhost:5050' : '');

/**
 * Authenticated fetch for `/api/assistant`. Not an openapi-fetch client: the messages endpoint
 * answers with a server-sent event stream the panel reads as it arrives.
 */
export async function assistantFetch(path: string, init: RequestInit = {}): Promise<Response> {
  const request = new Request(`${baseUrl}/api/assistant${path}`, init);
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
}
