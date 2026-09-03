import { useQuery, keepPreviousData } from '@tanstack/react-query';

import { api } from '../client';
import { queryKeys } from '../queryKeys';

import type { components } from '../generated/schema';

export type ShoppingListItemDto = components['schemas']['ShoppingListItemDto'];

interface ShoppingListParams {
  from: string;
  to: string;
}

export function useShoppingList(params: ShoppingListParams) {
  return useQuery({
    queryKey: queryKeys.shoppingList.detail(params),
    queryFn: async (): Promise<ShoppingListItemDto[]> => {
      const response = await api.GET('/api/v1/meals/shopping-list', {
        params: { query: { From: params.from, To: params.to } },
      });
      if (!response.data) throw new Error('Failed to fetch shopping list');
      return response.data;
    },
    placeholderData: keepPreviousData,
  });
}
