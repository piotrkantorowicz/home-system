import { renderHook, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';

import { useMeals } from './useMeals';

import { server } from '@/test/mocks/server';
import { createWrapper } from '@/test/utils/queryWrapper';

// Silence auth logs — no real OIDC context in tests
vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue(null),
  tryRenewToken: vi.fn().mockResolvedValue(null),
  redirectToLogin: vi.fn(),
}));

describe('useMeals', () => {
  it('returns meals for date range', async () => {
    const { result } = renderHook(() => useMeals({ from: '2024-01-15', to: '2024-01-15' }), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(Array.isArray(result.current.data)).toBe(true);
    expect(result.current.data).toHaveLength(1);
  });

  it('returns empty array when no meals in range', async () => {
    server.use(http.get('http://localhost:5000/api/v1/meals', () => HttpResponse.json([])));

    const { result } = renderHook(() => useMeals(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.data).toHaveLength(0);
  });
});
