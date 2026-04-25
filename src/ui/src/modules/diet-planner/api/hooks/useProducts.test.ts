import { renderHook, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';

import { useProducts } from './useProducts';

import { server } from '@/test/mocks/server';
import { createWrapper } from '@/test/utils/queryWrapper';

// Silence auth logs — no real OIDC context in tests
vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue(null),
  tryRenewToken: vi.fn().mockResolvedValue(null),
  redirectToLogin: vi.fn(),
}));

describe('useProducts', () => {
  it('returns products on success', async () => {
    const { result } = renderHook(() => useProducts(), {
      wrapper: createWrapper(),
    });

    expect(result.current.isLoading).toBe(true);

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.data?.items).toHaveLength(1);
    expect(result.current.data?.items[0]?.name).toBe('Chicken Breast');
  });

  it('exposes error when API fails', async () => {
    server.use(
      http.get('http://localhost:5050/api/v1/products', () =>
        HttpResponse.json({ title: 'Server Error' }, { status: 500 }),
      ),
    );

    const { result } = renderHook(() => useProducts(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isError).toBe(true);
    });
    expect(result.current.error).toBeTruthy();
  });
});
