import { renderHook, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';

import { queryKeys } from '../queryKeys';

import { productListOptions, productOptions, useProduct, useProducts } from './useProducts';

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

describe('productListOptions', () => {
  it('builds its key from the queryKeys factory with the defaults applied', () => {
    expect(productListOptions({ search: 'oat' }).queryKey).toEqual(
      queryKeys.products.list({ search: 'oat', onlyMine: false, page: 1, pageSize: 50 }),
    );
  });
});

describe('useProduct', () => {
  it('maps the DTO into the view model', async () => {
    const { result } = renderHook(() => useProduct('11111111-1111-1111-1111-111111111111'), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true);
    });

    expect(result.current.data?.name).toBe('Chicken Breast');
  });

  it('resolves to null when the product does not exist', async () => {
    const { result } = renderHook(() => useProduct('missing'), { wrapper: createWrapper() });

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true);
    });

    expect(result.current.data).toBeNull();
  });

  it('shares its key with productOptions so prefetches hit the same cache entry', () => {
    expect(productOptions('p1').queryKey).toEqual(queryKeys.products.detail('p1'));
  });
});
