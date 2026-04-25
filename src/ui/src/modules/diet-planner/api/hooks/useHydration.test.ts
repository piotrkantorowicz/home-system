import { renderHook, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';

import {
  useHydrationConfig,
  useUpdateHydrationConfig,
  useWaterIntake,
  useLogWaterIntake,
  useDeleteWaterIntake,
} from './useHydration';

import { server } from '@/test/mocks/server';
import { createWrapper } from '@/test/utils/queryWrapper';

// Silence auth logs — no real OIDC context in tests
vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue(null),
  tryRenewToken: vi.fn().mockResolvedValue(null),
  redirectToLogin: vi.fn(),
}));

const BASE = 'http://localhost:5050';

describe('useHydrationConfig', () => {
  it('returns hydration config data on success', async () => {
    const { result } = renderHook(() => useHydrationConfig(), { wrapper: createWrapper() });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.data).not.toBeNull();
    expect(result.current.data?.dailyWaterTargetMl).toBe(2500);
    expect(result.current.data?.glassSizeMl).toBe(250);
    expect(result.current.data?.trackWaterIntake).toBe(true);
  });

  it('returns null when config body is null', async () => {
    server.use(http.get(`${BASE}/api/v1/hydration/config`, () => HttpResponse.json(null)));

    const { result } = renderHook(() => useHydrationConfig(), { wrapper: createWrapper() });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.data).toBeNull();
  });

  it('throws on server error', async () => {
    server.use(
      http.get(`${BASE}/api/v1/hydration/config`, () =>
        HttpResponse.json({ title: 'Internal Server Error' }, { status: 500 }),
      ),
    );

    const { result } = renderHook(() => useHydrationConfig(), { wrapper: createWrapper() });

    await waitFor(() => {
      expect(result.current.isError).toBe(true);
    });
  });
});

describe('useUpdateHydrationConfig', () => {
  it('calls PUT /api/v1/hydration/config and resolves', async () => {
    const { result } = renderHook(() => useUpdateHydrationConfig(), { wrapper: createWrapper() });

    await waitFor(async () => {
      await result.current.mutateAsync({
        dailyWaterTargetMl: 3000,
        glassSizeMl: 300,
        trackWaterIntake: true,
      });
    });

    expect(result.current.isSuccess).toBe(true);
  });

  it('throws on server error', async () => {
    server.use(
      http.put(`${BASE}/api/v1/hydration/config`, () =>
        HttpResponse.json({ title: 'Bad Request' }, { status: 400 }),
      ),
    );

    const { result } = renderHook(() => useUpdateHydrationConfig(), { wrapper: createWrapper() });

    await waitFor(async () => {
      await expect(
        result.current.mutateAsync({
          dailyWaterTargetMl: 0,
          glassSizeMl: 0,
          trackWaterIntake: false,
        }),
      ).rejects.toThrow('Failed to update hydration config');
    });
  });
});

describe('useWaterIntake', () => {
  it('returns water intake data on success', async () => {
    const { result } = renderHook(() => useWaterIntake('2024-01-15'), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.data).not.toBeNull();
    expect(result.current.data?.totalMl).toBe(500);
    expect(result.current.data?.entries).toHaveLength(2);
  });

  it('throws on server error', async () => {
    server.use(
      http.get(`${BASE}/api/v1/hydration/intake`, () =>
        HttpResponse.json({ title: 'Internal Server Error' }, { status: 500 }),
      ),
    );

    const { result } = renderHook(() => useWaterIntake('2024-01-15'), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isError).toBe(true);
    });
  });
});

describe('useLogWaterIntake', () => {
  it('calls POST /api/v1/hydration/intake and returns id', async () => {
    const { result } = renderHook(() => useLogWaterIntake(), { wrapper: createWrapper() });

    let returnedId: string | undefined;
    await waitFor(async () => {
      returnedId = await result.current.mutateAsync({ date: '2024-01-15', amountMl: 250 });
    });

    expect(returnedId).toBe('dddddddd-dddd-dddd-dddd-dddddddddddd');
  });

  it('throws on server error', async () => {
    server.use(
      http.post(`${BASE}/api/v1/hydration/intake`, () =>
        HttpResponse.json({ title: 'Bad Request' }, { status: 400 }),
      ),
    );

    const { result } = renderHook(() => useLogWaterIntake(), { wrapper: createWrapper() });

    await waitFor(async () => {
      await expect(result.current.mutateAsync({ date: '2024-01-15', amountMl: 0 })).rejects.toThrow(
        'Failed to log water intake',
      );
    });
  });
});

describe('useDeleteWaterIntake', () => {
  it('calls DELETE /api/v1/hydration/intake/{id} and resolves', async () => {
    const { result } = renderHook(() => useDeleteWaterIntake(), { wrapper: createWrapper() });

    await waitFor(async () => {
      await result.current.mutateAsync({
        id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
        date: '2024-01-15',
      });
    });

    expect(result.current.isSuccess).toBe(true);
  });

  it('throws on server error', async () => {
    server.use(
      http.delete(`${BASE}/api/v1/hydration/intake/:id`, () =>
        HttpResponse.json({ title: 'Not Found' }, { status: 404 }),
      ),
    );

    const { result } = renderHook(() => useDeleteWaterIntake(), { wrapper: createWrapper() });

    await waitFor(async () => {
      await expect(
        result.current.mutateAsync({ id: 'nonexistent', date: '2024-01-15' }),
      ).rejects.toThrow('Failed to delete water intake entry');
    });
  });
});
