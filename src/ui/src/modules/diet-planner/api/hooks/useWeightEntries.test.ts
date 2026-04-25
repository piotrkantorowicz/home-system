import { renderHook, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';

import { useDeleteWeightEntry, useLogWeightEntry, useWeightEntries } from './useWeightEntries';

import { server } from '@/test/mocks/server';
import { createWrapper } from '@/test/utils/queryWrapper';

vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue(null),
  tryRenewToken: vi.fn().mockResolvedValue(null),
  redirectToLogin: vi.fn(),
}));

const BASE = 'http://localhost:5050';

describe('useWeightEntries', () => {
  it('returns weight entries on success', async () => {
    const { result } = renderHook(() => useWeightEntries(), { wrapper: createWrapper() });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.data).toHaveLength(2);
    expect(result.current.data?.[0]?.weightKg).toBe(80);
  });

  it('throws on server error', async () => {
    server.use(
      http.get(`${BASE}/api/v1/weight-entries`, () =>
        HttpResponse.json({ title: 'Internal Server Error' }, { status: 500 }),
      ),
    );

    const { result } = renderHook(() => useWeightEntries(), { wrapper: createWrapper() });

    await waitFor(() => {
      expect(result.current.isError).toBe(true);
    });
  });
});

describe('useLogWeightEntry', () => {
  it('returns the created response', async () => {
    const { result } = renderHook(() => useLogWeightEntry(), { wrapper: createWrapper() });

    let returned: { id: string; created: boolean } | undefined;
    await waitFor(async () => {
      returned = await result.current.mutateAsync({ date: '2024-01-03', weightKg: 78 });
    });

    expect(returned?.id).toBe('99999999-9999-9999-9999-999999999999');
    expect(returned?.created).toBe(true);
  });

  it('throws on server error', async () => {
    server.use(
      http.post(`${BASE}/api/v1/weight-entries`, () =>
        HttpResponse.json({ title: 'Bad Request' }, { status: 400 }),
      ),
    );

    const { result } = renderHook(() => useLogWeightEntry(), { wrapper: createWrapper() });

    await waitFor(async () => {
      await expect(
        result.current.mutateAsync({ date: '2024-01-03', weightKg: 78 }),
      ).rejects.toThrow('Failed to log weight entry');
    });
  });
});

describe('useDeleteWeightEntry', () => {
  it('resolves on successful delete', async () => {
    const { result } = renderHook(() => useDeleteWeightEntry(), { wrapper: createWrapper() });

    await waitFor(async () => {
      await result.current.mutateAsync('77777777-7777-7777-7777-777777777777');
    });

    expect(result.current.isSuccess).toBe(true);
  });

  it('throws on server error', async () => {
    server.use(
      http.delete(`${BASE}/api/v1/weight-entries/:id`, () =>
        HttpResponse.json({ title: 'Not Found' }, { status: 404 }),
      ),
    );

    const { result } = renderHook(() => useDeleteWeightEntry(), { wrapper: createWrapper() });

    await waitFor(async () => {
      await expect(
        result.current.mutateAsync('77777777-7777-7777-7777-777777777777'),
      ).rejects.toThrow('Failed to delete weight entry');
    });
  });
});
