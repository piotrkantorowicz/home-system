import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query';

import { api } from '../client';
import { queryKeys } from '../queryKeys';

import type { components } from '../generated/schema';

interface RecipesQueryParams {
  search?: string;
  onlyMine?: boolean;
  page?: number;
  pageSize?: number;
}

export function useRecipes(params: RecipesQueryParams = {}) {
  const { search = '', onlyMine = false, page = 1, pageSize = 50 } = params;

  return useQuery({
    queryKey: queryKeys.recipes.list({ search, onlyMine, page, pageSize }),
    queryFn: async () => {
      const response = await api.GET('/api/v1/recipes', {
        params: {
          query: { Search: search, OnlyMine: onlyMine, Page: page, PageSize: pageSize },
        },
      });

      if (!response.data) {
        throw new Error('Failed to fetch recipes');
      }

      return response.data;
    },
    placeholderData: keepPreviousData,
  });
}

export function useRecipe(id: string) {
  return useQuery({
    queryKey: queryKeys.recipes.detail(id),
    queryFn: async () => {
      const response = await api.GET('/api/v1/recipes/{id}', {
        params: {
          path: { id },
        },
      });

      if (!response.data) {
        throw new Error('Failed to fetch recipe');
      }

      return response.data;
    },
    enabled: !!id,
  });
}

export function useCreateRecipe() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (recipeData: components['schemas']['CreateRecipeRequest']) => {
      const response = await api.POST('/api/v1/recipes', {
        body: recipeData,
      });

      if (!response.response.ok) {
        const detail = (response.error as { detail?: string } | undefined)?.detail ?? '';
        throw new Error(`Failed to create recipe${detail ? `: ${detail}` : ''}`);
      }

      return response.data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.recipes.all() });
    },
  });
}

export function useUpdateRecipe(id: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (recipeData: components['schemas']['UpdateRecipeRequest']) => {
      const response = await api.PUT('/api/v1/recipes/{id}', {
        params: {
          path: { id },
        },
        body: recipeData,
      });

      if (!response.response.ok) {
        throw new Error('Failed to update recipe');
      }

      return response.data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.recipes.all() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.recipes.detail(id) });
    },
  });
}

export function useDeleteRecipe() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id }: { id: string }) => {
      const response = await api.DELETE('/api/v1/recipes/{id}', {
        params: {
          path: { id },
        },
      });

      return response.data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.recipes.all() });
    },
  });
}
