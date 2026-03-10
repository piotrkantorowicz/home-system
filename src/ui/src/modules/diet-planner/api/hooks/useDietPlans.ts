import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query';
import { api } from '../client';
import type { components } from '../generated/schema';

type DietPlan = {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  totalDays: number;
  totalMeals: number;
  createdAt: string;
};

type DietPlansResponse = {
  items: DietPlan[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

type Meal = components['schemas']['MealEntryDto'];

interface DietPlansQueryParams {
  page?: number;
  pageSize?: number;
}

export function useDietPlans(params: DietPlansQueryParams = {}) {
  const { page = 1, pageSize = 50 } = params;

  return useQuery({
    queryKey: ['diet-plans', { page, pageSize }],
    queryFn: async (): Promise<DietPlansResponse> => {
      const response = await api.GET('/api/v1/diet-plans', {
        params: {
          query: { page, pageSize },
        },
      });

      if (response.error) {
        throw new Error('Failed to fetch diet plans');
      }

      return response.data as DietPlansResponse;
    },
    placeholderData: keepPreviousData,
  });
}

export function useDietPlan(id: string) {
  return useQuery({
    queryKey: ['diet-plans', id],
    queryFn: async (): Promise<DietPlan> => {
      const response = await api.GET('/api/v1/diet-plans/{id}', {
        params: {
          path: { id },
        },
      });

      if (response.error) {
        throw new Error('Failed to fetch diet plan');
      }

      return response.data as DietPlan;
    },
    enabled: !!id,
  });
}

export function useMeals(id: string, from?: string, to?: string) {
  return useQuery({
    queryKey: ['diet-plans', id, 'meals', { from, to }],
    queryFn: async (): Promise<Meal[]> => {
      const response = await api.GET('/api/v1/diet-plans/{id}/meals', {
        params: {
          path: { id },
          query: { from, to },
        },
      });

      if (response.error) {
        throw new Error('Failed to fetch meals');
      }

      return response.data as Meal[];
    },
    enabled: !!id,
  });
}

interface CreateDietPlanData {
  name: string;
  startDate: string;
  endDate: string;
}

export function useCreateDietPlan() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: CreateDietPlanData): Promise<DietPlan> => {
      const response = await api.POST('/api/v1/diet-plans', {
        body: data as never,
      });

      if (response.error) throw new Error('Failed to create diet plan');
      return response.data as DietPlan;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['diet-plans'] });
    },
  });
}

export function useValidateImport() {
  return useMutation({
    mutationFn: async (importData: components['schemas']['ImportDto']) => {
      const response = await api.POST('/api/v1/diet-plans/validate', {
        body: importData,
      });

      if (response.error) {
        // If the server returns validation results with issues, that's not an error - it's expected
        // Only throw if the API call itself failed
        const errorMessage =
          typeof response.error === 'object' && response.error !== null
            ? JSON.stringify(response.error)
            : 'Validation API call failed';
        throw new Error(errorMessage);
      }

      return response.data;
    },
  });
}

export function useExecuteImport() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (importData: components['schemas']['ImportDto']) => {
      const response = await api.POST('/api/v1/diet-plans/import', {
        body: importData,
      });

      if (response.error) {
        throw new Error('Import failed');
      }

      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['diet-plans'] });
      queryClient.invalidateQueries({ queryKey: ['products'] });
      queryClient.invalidateQueries({ queryKey: ['recipes'] });
    },
  });
}

interface CreateMealEntryData {
  date: string;
  mealType: string;
  recipeId: string;
  servings: number;
  notes?: string;
  mealTime?: string;
  sequenceOrder?: number;
}

export function useCreateMeal(dietPlanId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: CreateMealEntryData) => {
      const response = await api.POST('/api/v1/diet-plans/{id}/meals', {
        params: { path: { id: dietPlanId } },
        body: data as never,
      });

      if (response.error) throw new Error('Failed to create meal entry');
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['diet-plans', dietPlanId, 'meals'] });
      queryClient.invalidateQueries({ queryKey: ['diet-plans', dietPlanId] });
    },
  });
}

export function useUpdateMeal(dietPlanId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ mealId, data }: { mealId: string; data: CreateMealEntryData }) => {
      const response = await api.PUT('/api/v1/diet-plans/{id}/meals/{mealId}', {
        params: { path: { id: dietPlanId, mealId } },
        body: data as never,
      });

      if (response.error) throw new Error('Failed to update meal entry');
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['diet-plans', dietPlanId, 'meals'] });
    },
  });
}

export function useDeleteMeal(dietPlanId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (mealId: string) => {
      const response = await api.DELETE('/api/v1/diet-plans/{id}/meals/{mealId}', {
        params: { path: { id: dietPlanId, mealId } },
      });

      if (response.error) throw new Error('Failed to delete meal entry');
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['diet-plans', dietPlanId, 'meals'] });
      queryClient.invalidateQueries({ queryKey: ['diet-plans', dietPlanId] });
    },
  });
}

export function useDeleteDietPlan() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, permanent = false }: { id: string; permanent?: boolean }) => {
      const response = await api.DELETE('/api/v1/diet-plans/{id}', {
        params: {
          path: { id },
          query: { permanent },
        },
      });

      if (response.error) {
        throw new Error('Failed to delete diet plan');
      }

      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['diet-plans'] });
    },
  });
}
