import { renderHook, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';

import { useShoppingList } from './useShoppingList';

import { server } from '@/test/mocks/server';
import { createWrapper } from '@/test/utils/queryWrapper';

const BASE = 'http://localhost:5050';

vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue(null),
  tryRenewToken: vi.fn().mockResolvedValue(null),
  redirectToLogin: vi.fn(),
}));

describe('useShoppingList', () => {
  it('returns aggregated items for date range', async () => {
    server.use(
      http.get(`${BASE}/api/v1/meals/shopping-list`, () =>
        HttpResponse.json([
          { productId: 'p-1', productName: 'Flour', totalAmount: 500, unit: 'g' },
          { productId: 'p-2', productName: 'Eggs', totalAmount: 3, unit: 'piece' },
        ]),
      ),
    );

    const { result } = renderHook(() => useShoppingList({ from: '2024-01-15', to: '2024-01-21' }), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.data).toHaveLength(2);
    expect(result.current.data?.[0]?.productName).toBe('Flour');
    expect(result.current.data?.[0]?.totalAmount).toBe(500);
    expect(result.current.data?.[0]?.unit).toBe('g');
  });

  it('returns empty array when no items for range', async () => {
    server.use(http.get(`${BASE}/api/v1/meals/shopping-list`, () => HttpResponse.json([])));

    const { result } = renderHook(() => useShoppingList({ from: '2024-01-15', to: '2024-01-21' }), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.data).toHaveLength(0);
  });
});
