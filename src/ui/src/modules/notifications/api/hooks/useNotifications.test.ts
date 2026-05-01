import { renderHook, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, it, expect, vi } from 'vitest';

import { useNotifications } from './useNotifications';

import { server } from '@/test/mocks/server';
import { createWrapper } from '@/test/utils/queryWrapper';

vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue(null),
  tryRenewToken: vi.fn().mockResolvedValue(null),
  redirectToLogin: vi.fn(),
}));

const BASE = 'http://localhost:5050';

describe('useNotifications', () => {
  it('returns the paged list on success', async () => {
    server.use(
      http.get(`${BASE}/api/notifications`, () =>
        HttpResponse.json({
          items: [
            {
              id: '11111111-1111-1111-1111-111111111111',
              type: 'MealReminder',
              title: 'Time for lunch',
              body: 'Your scheduled lunch starts now.',
              createdAt: '2026-05-01T12:00:00Z',
              readAt: null,
            },
          ],
          page: 1,
          pageSize: 20,
          totalCount: 1,
          totalPages: 1,
        }),
      ),
    );

    const { result } = renderHook(() => useNotifications(), { wrapper: createWrapper() });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });
    expect(result.current.data?.items).toHaveLength(1);
    expect(result.current.data?.items?.[0]?.title).toBe('Time for lunch');
  });

  it('passes page and pageSize to the request', async () => {
    let capturedPage: string | null = null;
    let capturedPageSize: string | null = null;
    server.use(
      http.get(`${BASE}/api/notifications`, ({ request }) => {
        const url = new URL(request.url);
        capturedPage = url.searchParams.get('page');
        capturedPageSize = url.searchParams.get('pageSize');
        return HttpResponse.json({ items: [], page: 2, pageSize: 5, totalCount: 0, totalPages: 0 });
      }),
    );

    const { result } = renderHook(() => useNotifications({ page: 2, pageSize: 5 }), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });
    expect(capturedPage).toBe('2');
    expect(capturedPageSize).toBe('5');
  });

  it('throws on server error', async () => {
    server.use(
      http.get(`${BASE}/api/notifications`, () =>
        HttpResponse.json({ title: 'Internal Server Error' }, { status: 500 }),
      ),
    );

    const { result } = renderHook(() => useNotifications(), { wrapper: createWrapper() });

    await waitFor(() => {
      expect(result.current.isError).toBe(true);
    });
  });
});
