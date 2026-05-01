import { renderHook, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, it, expect, vi } from 'vitest';

import { useChannelPreferences } from './useChannelPreferences';

import { server } from '@/test/mocks/server';
import { createWrapper } from '@/test/utils/queryWrapper';

vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue(null),
  tryRenewToken: vi.fn().mockResolvedValue(null),
  redirectToLogin: vi.fn(),
}));

const BASE = 'http://localhost:5050';

describe('useChannelPreferences', () => {
  it('returns the preferences on success', async () => {
    server.use(
      http.get(`${BASE}/api/notification-preferences`, () =>
        HttpResponse.json({ consoleEnabled: true, emailEnabled: false, webSocketEnabled: false }),
      ),
    );

    const { result } = renderHook(() => useChannelPreferences(), { wrapper: createWrapper() });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });
    expect(result.current.data?.consoleEnabled).toBe(true);
    expect(result.current.data?.emailEnabled).toBe(false);
    expect(result.current.data?.webSocketEnabled).toBe(false);
  });

  it('throws on server error', async () => {
    server.use(
      http.get(`${BASE}/api/notification-preferences`, () =>
        HttpResponse.json({ title: 'Internal Server Error' }, { status: 500 }),
      ),
    );

    const { result } = renderHook(() => useChannelPreferences(), { wrapper: createWrapper() });

    await waitFor(() => {
      expect(result.current.isError).toBe(true);
    });
  });
});
