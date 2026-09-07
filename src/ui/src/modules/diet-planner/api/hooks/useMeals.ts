import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query';

import { api } from '../client';
import { queryKeys } from '../queryKeys';

import type { components } from '../generated/schema';

export interface DailyNutrition {
  date: string;
  calories: number;
  protein: number;
  carbs: number;
  fat: number;
  fiber: number;
}

export type MealEntryDto = components['schemas']['MealEntryDto'];
type MealEntry = MealEntryDto;
type CreateMealEntryRequest = components['schemas']['CreateMealEntryRequest'];
type UpdateMealEntryRequest = components['schemas']['UpdateMealEntryRequest'];
export type OverrideMealEntryRequest = components['schemas']['OverrideMealEntryRequest'];
export type BulkCompleteMealsResponse = components['schemas']['BulkCompleteMealsResponse'];

interface MealsQueryParams {
  from?: string;
  to?: string;
}

export function useMeals(params: MealsQueryParams = {}) {
  const { from, to } = params;

  return useQuery({
    queryKey: queryKeys.meals.list({
      ...(from !== undefined ? { from } : {}),
      ...(to !== undefined ? { to } : {}),
    }),
    queryFn: async (): Promise<MealEntry[]> => {
      const response = await api.GET('/api/v1/meals', {
        params: {
          query: {
            ...(from !== undefined ? { From: from } : {}),
            ...(to !== undefined ? { To: to } : {}),
          },
        },
      });

      if (!response.data) throw new Error('Failed to fetch meals');
      return response.data;
    },
    placeholderData: keepPreviousData,
  });
}

export function useCreateMeal() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: CreateMealEntryRequest): Promise<MealEntry> => {
      const response = await api.POST('/api/v1/meals', { body: data });
      if (response.error) throw new Error('Failed to create meal entry');
      return response.data as unknown as MealEntry;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.meals.all() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.nutritionSummary.all() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.shoppingList.all() });
    },
  });
}

export function useUpdateMeal() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      id,
      data,
    }: {
      id: string;
      data: UpdateMealEntryRequest;
    }): Promise<MealEntry> => {
      const response = await api.PUT('/api/v1/meals/{id}', {
        params: { path: { id } },
        body: data,
      });
      if (response.error) throw new Error('Failed to update meal entry');
      return response.data as unknown as MealEntry;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.meals.all() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.nutritionSummary.all() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.shoppingList.all() });
    },
  });
}

export function useCompleteMeal() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string): Promise<void> => {
      const response = await api.PATCH('/api/v1/meals/{id}/complete', {
        params: { path: { id } },
      });
      if (!response.response.ok) throw new Error('Failed to complete meal');
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.meals.all() }),
        queryClient.invalidateQueries({ queryKey: queryKeys.nutritionSummary.all() }),
        queryClient.invalidateQueries({ queryKey: queryKeys.shoppingList.all() }),
      ]);
    },
  });
}

export function useOverrideMeal() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      id,
      data,
    }: {
      id: string;
      data: OverrideMealEntryRequest;
    }): Promise<void> => {
      const response = await api.PATCH('/api/v1/meals/{id}/override', {
        params: { path: { id } },
        body: data,
      });
      if (!response.response.ok) throw new Error('Failed to override meal');
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.meals.all() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.nutritionSummary.all() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.shoppingList.all() });
    },
  });
}

export function useResetMeal() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string): Promise<void> => {
      const response = await api.PATCH('/api/v1/meals/{id}/reset', {
        params: { path: { id } },
      });
      if (!response.response.ok) throw new Error('Failed to reset meal');
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.meals.all() }),
        queryClient.invalidateQueries({ queryKey: queryKeys.nutritionSummary.all() }),
        queryClient.invalidateQueries({ queryKey: queryKeys.shoppingList.all() }),
      ]);
    },
  });
}

export function useBulkCompleteMeals() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (date: string): Promise<BulkCompleteMealsResponse> => {
      const response = await api.POST('/api/v1/meals/bulk-complete', {
        body: { date },
      });
      if (!response.data) throw new Error('Failed to bulk complete meals');
      return response.data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.meals.all() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.nutritionSummary.all() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.shoppingList.all() });
    },
  });
}

export function useDeleteMeal() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string): Promise<void> => {
      const response = await api.DELETE('/api/v1/meals/{id}', {
        params: { path: { id } },
      });
      if (!response.response.ok) throw new Error('Failed to delete meal entry');
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.meals.all() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.nutritionSummary.all() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.shoppingList.all() });
    },
  });
}

export function useNutritionSummary(params: { from: string; to: string }) {
  return useQuery({
    queryKey: queryKeys.nutritionSummary.detail(params),
    queryFn: async (): Promise<DailyNutrition[]> => {
      // REASON: /api/v1/meals/nutrition-summary is not yet in the generated openapi schema — regenerate schema to remove this cast
      // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-member-access
      const response = (await (api as any).GET('/api/v1/meals/nutrition-summary', {
        params: { query: params },
      })) as { data?: DailyNutrition[]; error?: unknown };
      if (response.error) throw new Error('Failed to fetch nutrition summary');
      return response.data ?? [];
    },
    placeholderData: keepPreviousData,
  });
}

export function useValidateImport() {
  return useMutation({
    mutationFn: async (importData: components['schemas']['ImportDto']) => {
      const response = await api.POST('/api/v1/meals/validate', {
        body: importData,
      });

      if (response.error) {
        throw new Error('Validation API call failed');
      }

      if (!response.data) {
        throw new Error('Validation returned no data');
      }

      return response.data;
    },
  });
}

export function useExecuteImport() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (importData: components['schemas']['ImportDto']) => {
      const response = await api.POST('/api/v1/meals/import', {
        body: importData,
      });

      if (response.error) throw new Error('Import failed');
      if (!response.data) throw new Error('Import returned no data');
      return response.data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.meals.all() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.nutritionSummary.all() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.shoppingList.all() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.products.all() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.recipes.all() });
    },
  });
}
