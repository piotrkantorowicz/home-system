import { renderHook, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';

import { queryKeys } from '../queryKeys';

import { recipeListOptions, recipeOptions, useRecipe, useRecipes } from './useRecipes';

import { server } from '@/test/mocks/server';
import { createWrapper } from '@/test/utils/queryWrapper';

// Silence auth logs — no real OIDC context in tests
vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue(null),
  tryRenewToken: vi.fn().mockResolvedValue(null),
  redirectToLogin: vi.fn(),
}));

describe('useRecipes', () => {
  it('returns the paged list on success', async () => {
    server.use(
      http.get('http://localhost:5050/api/v1/recipes', () =>
        HttpResponse.json({
          items: [{ id: 'r1', name: 'Oat bowl', servings: 2, isOwner: true }],
          page: 1,
          pageSize: 50,
          totalCount: 1,
          totalPages: 1,
        }),
      ),
    );

    const { result } = renderHook(() => useRecipes(), { wrapper: createWrapper() });

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true);
    });

    expect(result.current.data?.items).toHaveLength(1);
  });

  it('builds its key from the queryKeys factory with the defaults applied', () => {
    expect(recipeListOptions({ onlyMine: true }).queryKey).toEqual(
      queryKeys.recipes.list({ search: '', onlyMine: true, page: 1, pageSize: 50 }),
    );
  });
});

describe('useRecipe', () => {
  it('returns the recipe on success', async () => {
    const { result } = renderHook(() => useRecipe('11111111-1111-1111-1111-111111111111'), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true);
    });

    expect(result.current.data?.name).toBe('Test Recipe');
  });

  it('resolves to null when the recipe does not exist', async () => {
    const { result } = renderHook(() => useRecipe('missing'), { wrapper: createWrapper() });

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true);
    });

    expect(result.current.data).toBeNull();
  });

  it('shares its key with recipeOptions so prefetches hit the same cache entry', () => {
    expect(recipeOptions('r1').queryKey).toEqual(queryKeys.recipes.detail('r1'));
  });
});
