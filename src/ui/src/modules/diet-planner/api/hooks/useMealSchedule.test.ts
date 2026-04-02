import { renderHook, waitFor, act } from '@testing-library/react';
import { http, HttpResponse } from 'msw';

import { useMealSchedule, useUpdateMealSchedule } from './useMealSchedule';

import { server } from '@/test/mocks/server';
import { createWrapper } from '@/test/utils/queryWrapper';

// Silence auth logs — no real OIDC context in tests
vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue(null),
  tryRenewToken: vi.fn().mockResolvedValue(null),
  redirectToLogin: vi.fn(),
}));

const BASE = 'http://localhost:5000';

const mockSchedule = {
  id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
  userId: 'user-1',
  slots: [
    {
      id: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
      name: 'Breakfast',
      defaultTime: '07:00',
      sortOrder: 0,
    },
    {
      id: 'dddddddd-dddd-dddd-dddd-dddddddddddd',
      name: 'Lunch',
      defaultTime: '12:00',
      sortOrder: 1,
    },
  ],
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: null,
};

describe('useMealSchedule', () => {
  it('returns null when API returns 200 with null body (no schedule configured yet)', async () => {
    server.use(
      http.get(`${BASE}/api/v1/meal-schedule`, () => new HttpResponse('null', { status: 200 })),
    );

    const { result } = renderHook(() => useMealSchedule(), { wrapper: createWrapper() });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.data).toBeNull();
    expect(result.current.isError).toBe(false);
  });

  it('returns schedule data on success', async () => {
    server.use(http.get(`${BASE}/api/v1/meal-schedule`, () => HttpResponse.json(mockSchedule)));

    const { result } = renderHook(() => useMealSchedule(), { wrapper: createWrapper() });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.data).toEqual(mockSchedule);
    expect(result.current.data?.slots).toHaveLength(2);
    expect(result.current.data?.slots[0].name).toBe('Breakfast');
  });

  it('exposes error when GET API fails', async () => {
    server.use(
      http.get(`${BASE}/api/v1/meal-schedule`, () =>
        HttpResponse.json({ title: 'Server Error' }, { status: 500 }),
      ),
    );

    const { result } = renderHook(() => useMealSchedule(), { wrapper: createWrapper() });

    await waitFor(() => {
      expect(result.current.isError).toBe(true);
    });

    expect(result.current.error).toBeTruthy();
  });
});

describe('useUpdateMealSchedule', () => {
  it('sends PUT request with provided slots', async () => {
    let capturedBody: unknown;

    server.use(
      http.put(`${BASE}/api/v1/meal-schedule`, async ({ request }) => {
        capturedBody = await request.json();
        return new HttpResponse(null, { status: 204 });
      }),
    );

    const { result } = renderHook(() => useUpdateMealSchedule(), { wrapper: createWrapper() });

    const payload = {
      slots: [
        { name: 'Breakfast', defaultTime: '07:00' },
        { name: 'Lunch', defaultTime: '12:00' },
      ],
    };

    await act(async () => {
      await result.current.mutateAsync(payload);
    });

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true);
    });

    expect(capturedBody).toEqual(payload);
  });

  it('exposes error when PUT API fails', async () => {
    server.use(
      http.put(`${BASE}/api/v1/meal-schedule`, () =>
        HttpResponse.json({ title: 'Validation Error' }, { status: 400 }),
      ),
    );

    const { result } = renderHook(() => useUpdateMealSchedule(), { wrapper: createWrapper() });

    await act(async () => {
      try {
        await result.current.mutateAsync({ slots: [] });
      } catch {
        // expected
      }
    });

    await waitFor(() => {
      expect(result.current.isError).toBe(true);
    });

    expect(result.current.error).toBeTruthy();
  });
});
