import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query';
import { api } from '../client';
import type { components } from '../generated/schema';

type MealEntry = components['schemas']['MealEntryDto'];
type CreateMealEntryRequest = components['schemas']['CreateMealEntryRequest'];
type UpdateMealEntryRequest = components['schemas']['UpdateMealEntryRequest'];

interface MealsQueryParams {
  from?: string;
  to?: string;
}

export function useMeals(params: MealsQueryParams = {}) {
  const { from, to } = params;

  return useQuery({
    queryKey: ['meals', { from, to }],
    queryFn: async (): Promise<MealEntry[]> => {
      const response = await api.GET('/api/v1/meals', {
        params: { query: { from, to } },
      });

      if (response.error) throw new Error('Failed to fetch meals');
      return response.data as MealEntry[];
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
      return response.data as MealEntry;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['meals'] });
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
      return response.data as MealEntry;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['meals'] });
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
      if (response.error) throw new Error('Failed to delete meal entry');
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['meals'] });
    },
  });
}

export function useValidateImport() {
  return useMutation({
    mutationFn: async (importData: components['schemas']['ImportDto']) => {
      const response = await api.POST('/api/v1/meals/validate', {
        body: importData,
      });

      if (!response.data) {
        throw new Error('Validation API call failed');
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
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['meals'] });
      queryClient.invalidateQueries({ queryKey: ['products'] });
      queryClient.invalidateQueries({ queryKey: ['recipes'] });
    },
  });
}
