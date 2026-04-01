import { renderHook, waitFor, act } from '@testing-library/react';
import { http, HttpResponse } from 'msw';

import {
  useNotificationPreferences,
  useUpdateNotificationPreferences,
} from './useNotificationPreferences';

import { server } from '@/test/mocks/server';
import { createWrapper } from '@/test/utils/queryWrapper';

// Silence auth logs — no real OIDC context in tests
vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue(null),
  tryRenewToken: vi.fn().mockResolvedValue(null),
  redirectToLogin: vi.fn(),
}));

const BASE = 'http://localhost:5000';

const mockPreferences = {
  id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
  userId: 'user-1',
  mealReminderEnabled: true,
  mealReminderLeadTimeMinutes: 15,
  waterReminderEnabled: true,
  waterReminderIntervalMinutes: 60,
  weeklySummaryEnabled: true,
  goalMilestoneAlertsEnabled: true,
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: null,
};

describe('useNotificationPreferences', () => {
  it('enters error state when API returns 404 (no preferences configured yet)', async () => {
    server.use(
      http.get(`${BASE}/api/v1/notification-preferences`, () =>
        HttpResponse.json({ title: 'Not found' }, { status: 404 }),
      ),
    );

    const { result } = renderHook(() => useNotificationPreferences(), {
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

  it('returns preferences on success', async () => {
    server.use(
      http.get(`${BASE}/api/v1/notification-preferences`, () => HttpResponse.json(mockPreferences)),
    );

    const { result } = renderHook(() => useNotificationPreferences(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.data).toEqual(mockPreferences);
    expect(result.current.data?.mealReminderEnabled).toBe(true);
    expect(result.current.data?.mealReminderLeadTimeMinutes).toBe(15);
    expect(result.current.data?.waterReminderIntervalMinutes).toBe(60);
  });

  it('exposes error when GET API fails', async () => {
    server.use(
      http.get(`${BASE}/api/v1/notification-preferences`, () =>
        HttpResponse.json({ title: 'Server Error' }, { status: 500 }),
      ),
    );

    const { result } = renderHook(() => useNotificationPreferences(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isError).toBe(true);
    });

    expect(result.current.error).toBeTruthy();
  });
});

describe('useUpdateNotificationPreferences', () => {
  it('sends PUT request with provided data', async () => {
    let capturedBody: unknown;

    server.use(
      http.put(`${BASE}/api/v1/notification-preferences`, async ({ request }) => {
        capturedBody = await request.json();
        return new HttpResponse(null, { status: 204 });
      }),
    );

    const { result } = renderHook(() => useUpdateNotificationPreferences(), {
      wrapper: createWrapper(),
    });

    const payload = {
      mealReminderEnabled: false,
      mealReminderLeadTimeMinutes: 30,
      waterReminderEnabled: false,
      waterReminderIntervalMinutes: 120,
      weeklySummaryEnabled: false,
      goalMilestoneAlertsEnabled: false,
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
      http.put(`${BASE}/api/v1/notification-preferences`, () =>
        HttpResponse.json({ title: 'Validation Error' }, { status: 400 }),
      ),
    );

    const { result } = renderHook(() => useUpdateNotificationPreferences(), {
      wrapper: createWrapper(),
    });

    await act(async () => {
      try {
        await result.current.mutateAsync({
          mealReminderEnabled: true,
          mealReminderLeadTimeMinutes: 15,
          waterReminderEnabled: true,
          waterReminderIntervalMinutes: 60,
          weeklySummaryEnabled: true,
          goalMilestoneAlertsEnabled: true,
        });
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
