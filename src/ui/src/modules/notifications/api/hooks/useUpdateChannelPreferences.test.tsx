import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderHook, waitFor, act } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import React from 'react';
import { describe, it, expect, vi } from 'vitest';

import { notificationsQueryKeys } from '../queryKeys';

import { useUpdateChannelPreferences } from './useUpdateChannelPreferences';

import type { ChannelPreferencesDto } from './useChannelPreferences';

import { server } from '@/test/mocks/server';

vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue(null),
  tryRenewToken: vi.fn().mockResolvedValue(null),
  redirectToLogin: vi.fn(),
}));

const BASE = 'http://localhost:5050';

const initial: ChannelPreferencesDto = {
  consoleEnabled: true,
  emailEnabled: false,
  webSocketEnabled: false,
};

function setup() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  client.setQueryData(notificationsQueryKeys.channelPreferences.detail(), initial);
  const wrapper = ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={client}>{children}</QueryClientProvider>
  );
  return { client, wrapper };
}

describe('useUpdateChannelPreferences', () => {
  it('optimistically updates the cache before the request resolves', async () => {
    const { client, wrapper } = setup();
    server.use(
      http.put(
        `${BASE}/api/notification-preferences`,
        () => new HttpResponse(null, { status: 204 }),
      ),
    );

    const { result } = renderHook(() => useUpdateChannelPreferences(), { wrapper });

    const next: ChannelPreferencesDto = { ...initial, consoleEnabled: false };

    await act(async () => {
      await result.current.mutateAsync(next);
    });

    const cached = client.getQueryData<ChannelPreferencesDto>(
      notificationsQueryKeys.channelPreferences.detail(),
    );
    expect(cached?.consoleEnabled).toBe(false);
  });

  it('rolls the cache back when the request fails', async () => {
    const { client, wrapper } = setup();
    server.use(
      http.put(`${BASE}/api/notification-preferences`, () =>
        HttpResponse.json({ title: 'boom' }, { status: 500 }),
      ),
    );

    const { result } = renderHook(() => useUpdateChannelPreferences(), { wrapper });

    await expect(
      result.current.mutateAsync({ ...initial, consoleEnabled: false }),
    ).rejects.toThrow();

    await waitFor(() => {
      const cached = client.getQueryData<ChannelPreferencesDto>(
        notificationsQueryKeys.channelPreferences.detail(),
      );
      expect(cached?.consoleEnabled).toBe(true);
    });
  });
});
