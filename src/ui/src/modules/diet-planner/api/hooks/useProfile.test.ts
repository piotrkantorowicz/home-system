import { renderHook, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';

import { useProfile, useCreateProfile, useUpdateProfile } from './useProfile';

import { server } from '@/test/mocks/server';
import { createWrapper } from '@/test/utils/queryWrapper';

// Silence auth logs — no real OIDC context in tests
vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue(null),
  tryRenewToken: vi.fn().mockResolvedValue(null),
  redirectToLogin: vi.fn(),
}));

const BASE = 'http://localhost:5050';

describe('useProfile', () => {
  it('returns profile data on success', async () => {
    const { result } = renderHook(() => useProfile(), { wrapper: createWrapper() });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.data).not.toBeNull();
    expect(result.current.data?.userId).toBe('user-1');
    expect(result.current.data?.heightCm).toBe(180);
    expect(result.current.data?.gender).toBe('Male');
  });

  it('returns null when profile does not exist (404)', async () => {
    server.use(http.get(`${BASE}/api/v1/profile`, () => new HttpResponse(null, { status: 404 })));

    const { result } = renderHook(() => useProfile(), { wrapper: createWrapper() });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    // openapi-fetch does not set error for schema-defined 404 with no body; data is null
    expect(result.current.data).toBeNull();
  });

  it('throws on server error', async () => {
    server.use(
      http.get(`${BASE}/api/v1/profile`, () =>
        HttpResponse.json({ title: 'Internal Server Error' }, { status: 500 }),
      ),
    );

    const { result } = renderHook(() => useProfile(), { wrapper: createWrapper() });

    await waitFor(() => {
      expect(result.current.isError).toBe(true);
    });
  });
});

describe('useCreateProfile', () => {
  it('calls POST /api/v1/profile and returns the new id', async () => {
    const { result } = renderHook(() => useCreateProfile(), { wrapper: createWrapper() });

    let returnedId: string | undefined;
    await waitFor(async () => {
      returnedId = await result.current.mutateAsync({
        dateOfBirth: '1990-05-15',
        gender: 'Male',
        heightCm: 180,
        currentWeightKg: 80,
        targetWeightKg: 75,
        activityLevel: 'ModeratelyActive',
      });
    });

    expect(returnedId).toBe('66666666-6666-6666-6666-666666666666');
  });

  it('throws on server error', async () => {
    server.use(
      http.post(`${BASE}/api/v1/profile`, () =>
        HttpResponse.json({ title: 'Internal Server Error' }, { status: 500 }),
      ),
    );

    const { result } = renderHook(() => useCreateProfile(), { wrapper: createWrapper() });

    await waitFor(async () => {
      await expect(
        result.current.mutateAsync({
          dateOfBirth: null,
          gender: null,
          heightCm: null,
          currentWeightKg: null,
          targetWeightKg: null,
          activityLevel: null,
        }),
      ).rejects.toThrow('Failed to create profile');
    });
  });
});

describe('useUpdateProfile', () => {
  it('calls PUT /api/v1/profile and resolves', async () => {
    const { result } = renderHook(() => useUpdateProfile(), { wrapper: createWrapper() });

    await waitFor(async () => {
      await result.current.mutateAsync({
        dateOfBirth: null,
        gender: 'Female',
        heightCm: 165,
        currentWeightKg: 60,
        targetWeightKg: 55,
        activityLevel: 'LightlyActive',
      });
    });

    expect(result.current.isSuccess).toBe(true);
  });

  it('throws on server error', async () => {
    server.use(
      http.put(`${BASE}/api/v1/profile`, () =>
        HttpResponse.json({ title: 'Not Found' }, { status: 404 }),
      ),
    );

    const { result } = renderHook(() => useUpdateProfile(), { wrapper: createWrapper() });

    await waitFor(async () => {
      await expect(
        result.current.mutateAsync({
          dateOfBirth: null,
          gender: null,
          heightCm: null,
          currentWeightKg: null,
          targetWeightKg: null,
          activityLevel: null,
        }),
      ).rejects.toThrow('Failed to update profile');
    });
  });
});
