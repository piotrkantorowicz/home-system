import { api as adminApi } from '@modules/admin/api/client';
import { api as dietApi } from '@modules/diet-planner/api/client';
import { api as notificationsApi } from '@modules/notifications/api/client';

// openapi-fetch captures globalThis.fetch at createClient(); the tracing SDK patches it later.
describe('api clients use the live fetch', () => {
  it.each([
    ['admin', () => adminApi.GET('/api/v1/admin/users' as never)],
    ['diet-planner', () => dietApi.GET('/api/v1/products' as never)],
    ['notifications', () => notificationsApi.GET('/api/v1/notifications' as never)],
  ])('%s client calls a fetch patched after import', async (_name, call) => {
    const patched = vi.fn(() => Promise.resolve(new Response('{}', { status: 200 })));
    vi.stubGlobal('fetch', patched);
    try {
      await call();
    } catch {
      // the response shape is irrelevant; only the fetch routing is under test
    }
    vi.unstubAllGlobals();
    expect(patched).toHaveBeenCalled();
  });
});
