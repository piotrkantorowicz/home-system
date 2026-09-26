import { renderHook, waitFor, act } from '@testing-library/react';
import { http, HttpResponse } from 'msw';

import { useDietReminderSettings, useUpdateDietReminderSettings } from './useDietReminderSettings';

import { server } from '@/test/mocks/server';
import { createWrapper } from '@/test/utils/queryWrapper';

// Silence auth logs — no real OIDC context in tests
vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue(null),
  tryRenewToken: vi.fn().mockResolvedValue(null),
  redirectToLogin: vi.fn(),
}));

const BASE = 'http://localhost:5050';

const mockSettings = {
  id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
  userId: 'user-1',
  mealRemindersEnabled: true,
  mealReminderLeadTimeMinutes: 15,
  mealMissedGraceMinutes: 30,
  waterRemindersEnabled: true,
  waterReminderIntervalMinutes: 60,
  waterWindowStart: '06:00:00',
  waterWindowEnd: '22:00:00',
  weeklySummaryEnabled: true,
  weeklySummaryDayOfWeek: 0,
  weeklySummaryTimeOfDay: '08:00:00',
  goalAlertsEnabled: true,
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: null,
};

describe('useDietReminderSettings', () => {
  it('enters error state when API returns 404 (no settings configured yet)', async () => {
    server.use(
      http.get(`${BASE}/api/v1/diet-reminder-settings`, () =>
        HttpResponse.json({ title: 'Not found' }, { status: 404 }),
      ),
    );

    const { result } = renderHook(() => useDietReminderSettings(), {
      wrapper: createWrapper(),
    });

    expect(result.current.isLoading).toBe(true);

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    // openapi-fetch treats non-2xx as errors → hook throws → query enters error state
    expect(result.current.isError).toBe(true);
    expect(result.current.data).toBeUndefined();
  });

  it('returns settings on success', async () => {
    server.use(
      http.get(`${BASE}/api/v1/diet-reminder-settings`, () => HttpResponse.json(mockSettings)),
    );

    const { result } = renderHook(() => useDietReminderSettings(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.data).toEqual(mockSettings);
    expect(result.current.data?.mealRemindersEnabled).toBe(true);
    expect(result.current.data?.mealMissedGraceMinutes).toBe(30);
    expect(result.current.data?.waterWindowStart).toBe('06:00:00');
    expect(result.current.data?.weeklySummaryDayOfWeek).toBe(0);
  });

  it('exposes error when GET API fails', async () => {
    server.use(
      http.get(`${BASE}/api/v1/diet-reminder-settings`, () =>
        HttpResponse.json({ title: 'Server Error' }, { status: 500 }),
      ),
    );

    const { result } = renderHook(() => useDietReminderSettings(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isError).toBe(true);
    });

    expect(result.current.error).toBeTruthy();
  });
});

describe('useUpdateDietReminderSettings', () => {
  const payload = {
    mealRemindersEnabled: false,
    mealReminderLeadTimeMinutes: 30,
    mealMissedGraceMinutes: 45,
    waterRemindersEnabled: false,
    waterReminderIntervalMinutes: 120,
    waterWindowStart: '07:00:00',
    waterWindowEnd: '21:00:00',
    weeklySummaryEnabled: false,
    weeklySummaryDayOfWeek: 1,
    weeklySummaryTimeOfDay: '09:00:00',
    goalAlertsEnabled: false,
  };

  it('sends PUT request with provided data', async () => {
    let capturedBody: unknown;

    server.use(
      http.put(`${BASE}/api/v1/diet-reminder-settings`, async ({ request }) => {
        capturedBody = await request.json();
        return new HttpResponse(null, { status: 204 });
      }),
    );

    const { result } = renderHook(() => useUpdateDietReminderSettings(), {
      wrapper: createWrapper(),
    });

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
      http.put(`${BASE}/api/v1/diet-reminder-settings`, () =>
        HttpResponse.json({ title: 'Validation Error' }, { status: 400 }),
      ),
    );

    const { result } = renderHook(() => useUpdateDietReminderSettings(), {
      wrapper: createWrapper(),
    });

    await act(async () => {
      try {
        await result.current.mutateAsync(payload);
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
