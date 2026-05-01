import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderHook, waitFor, act } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import React from 'react';
import { describe, it, expect, vi } from 'vitest';

import { notificationsQueryKeys } from '../queryKeys';

import { useMarkRead } from './useMarkRead';

import { server } from '@/test/mocks/server';

vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue(null),
  tryRenewToken: vi.fn().mockResolvedValue(null),
  redirectToLogin: vi.fn(),
}));

const BASE = 'http://localhost:5050';
const ID = '11111111-1111-1111-1111-111111111111';

function makePage(readAt: string | null = null) {
  return {
    items: [
      {
        id: ID,
        type: 'MealReminder',
        title: 'Lunch',
        body: 'Body',
        createdAt: '2026-05-01T12:00:00Z',
        readAt,
      },
    ],
    page: 1,
    pageSize: 20,
    totalCount: 1,
    totalPages: 1,
  };
}

function setup() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  client.setQueryData(
    notificationsQueryKeys.notifications.list({ page: 1, pageSize: 20 }),
    makePage(),
  );
  const wrapper = ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={client}>{children}</QueryClientProvider>
  );
  return { client, wrapper };
}

describe('useMarkRead', () => {
  it('optimistically marks the matching notification as read', async () => {
    const { client, wrapper } = setup();
    server.use(
      http.post(
        `${BASE}/api/notifications/${ID}/read`,
        () => new HttpResponse(null, { status: 204 }),
      ),
    );

    const { result } = renderHook(() => useMarkRead(), { wrapper });

    await act(async () => {
      await result.current.mutateAsync(ID);
    });

    const cached = client.getQueryData<ReturnType<typeof makePage>>(
      notificationsQueryKeys.notifications.list({ page: 1, pageSize: 20 }),
    );
    const firstReadAt = cached?.items.at(0)?.readAt;
    expect(firstReadAt).not.toBeNull();
  });

  it('rolls back the cache when the request fails', async () => {
    const { client, wrapper } = setup();
    server.use(
      http.post(`${BASE}/api/notifications/${ID}/read`, () =>
        HttpResponse.json({ title: 'boom' }, { status: 500 }),
      ),
    );

    const { result } = renderHook(() => useMarkRead(), { wrapper });

    await expect(result.current.mutateAsync(ID)).rejects.toThrow();

    await waitFor(() => {
      const cached = client.getQueryData<ReturnType<typeof makePage>>(
        notificationsQueryKeys.notifications.list({ page: 1, pageSize: 20 }),
      );
      const firstReadAt = cached?.items.at(0)?.readAt;
      expect(firstReadAt).toBeNull();
    });
  });
});
