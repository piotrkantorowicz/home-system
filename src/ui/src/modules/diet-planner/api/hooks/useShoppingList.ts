import {
  keepPreviousData,
  queryOptions,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query';

import { api } from '../client';
import { queryKeys } from '../queryKeys';

import type { components } from '../generated/schema';

export type ShoppingListItemDto = components['schemas']['ShoppingListItemDto'];

interface ShoppingListParams {
  from: string;
  to: string;
}

export function shoppingListOptions(params: ShoppingListParams) {
  return queryOptions({
    queryKey: queryKeys.shoppingList.detail(params),
    queryFn: async (): Promise<ShoppingListItemDto[]> => {
      const response = await api.GET('/api/v1/meals/shopping-list', {
        params: { query: { From: params.from, To: params.to } },
      });
      if (!response.data) throw new Error('Failed to fetch shopping list');
      return response.data;
    },
  });
}

export function useShoppingList(params: ShoppingListParams) {
  return useQuery({ ...shoppingListOptions(params), placeholderData: keepPreviousData });
}

/** Ticks or unticks one row for the whole household; the row flips at once and rolls back if the API refuses. */
export function useSetShoppingCheck(params: ShoppingListParams) {
  const queryClient = useQueryClient();
  const key = queryKeys.shoppingList.detail(params);
  return useMutation({
    mutationFn: async (v: { productId: string; unit: string; isChecked: boolean }) => {
      const { response } = await api.PUT('/api/v1/meals/shopping-list/checks', {
        body: { from: params.from, to: params.to, ...v },
      });
      if (!response.ok) throw new Error('Failed to save shopping check');
    },
    onMutate: async (v) => {
      await queryClient.cancelQueries({ queryKey: key });
      const previous = queryClient.getQueryData<ShoppingListItemDto[]>(key);
      queryClient.setQueryData<ShoppingListItemDto[]>(key, (items) =>
        items?.map((i) =>
          i.productId === v.productId && i.unit === v.unit ? { ...i, isChecked: v.isChecked } : i,
        ),
      );
      return { previous };
    },
    onError: (_e, _v, ctx) => {
      queryClient.setQueryData(key, ctx?.previous);
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: key }),
  });
}

export function useClearShoppingChecks(params: ShoppingListParams) {
  const queryClient = useQueryClient();
  const key = queryKeys.shoppingList.detail(params);
  return useMutation({
    mutationFn: async () => {
      const { response } = await api.DELETE('/api/v1/meals/shopping-list/checks', {
        params: { query: { From: params.from, To: params.to } },
      });
      if (!response.ok) throw new Error('Failed to clear shopping checks');
    },
    onMutate: async () => {
      await queryClient.cancelQueries({ queryKey: key });
      const previous = queryClient.getQueryData<ShoppingListItemDto[]>(key);
      queryClient.setQueryData<ShoppingListItemDto[]>(key, (items) =>
        items?.map((i) => ({ ...i, isChecked: false })),
      );
      return { previous };
    },
    onError: (_e, _v, ctx) => {
      queryClient.setQueryData(key, ctx?.previous);
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: key }),
  });
}
