import { http, HttpResponse } from 'msw';

import { api } from './client';

import { server } from '@/test/mocks/server';

const redirect = vi.hoisted(() => vi.fn());
vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue('token'),
  tryRenewToken: vi.fn().mockResolvedValue('renewed-token'),
  redirectToLogin: redirect,
}));

it('handles a consumed replacement request without throwing during token renewal', async () => {
  server.use(
    http.put('http://localhost:5050/api/v1/recipes/r1/image', async ({ request }) => {
      await request.text();
      return HttpResponse.json({}, { status: 401 });
    }),
  );
  const response = await api.PUT('/api/v1/recipes/{id}/image', {
    params: { path: { id: 'r1' } },
    body: { file: 'image' },
    middleware: [{ onRequest: ({ request }) => new Request(request) }],
  });
  expect(response.response.status).toBe(401);
  expect(redirect).toHaveBeenCalledOnce();
});
