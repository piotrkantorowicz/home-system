import { queryOptions, useMutation, useQueryClient } from '@tanstack/react-query';
import { useEffect, useRef } from 'react';

import { api } from '../client';
import { queryKeys } from '../queryKeys';

import type { RecipeDto } from './useRecipes';

export class RecipeImageError extends Error {
  readonly status: number;

  constructor(status: number) {
    super('Recipe image request failed');
    this.status = status;
  }
}

export function recipeImageOptions(id: string, version: string, scope: string) {
  return queryOptions({
    queryKey: ['recipe-images', scope, id, version],
    queryFn: async ({ signal }) => {
      const response = await api.GET('/api/v1/recipes/{id}/image', {
        params: { path: { id } },
        parseAs: 'blob',
        cache: 'no-store',
        signal,
      });
      if ([401, 403, 404].includes(response.response.status)) return null;
      if (!response.response.ok || !response.data) {
        throw new RecipeImageError(response.response.status);
      }
      return response.data;
    },
    gcTime: 0,
    staleTime: 0,
    retry: false,
  });
}

export function useChangeRecipeImage(id: string, scope: string) {
  const client = useQueryClient();
  const active = useRef(true);
  const isCurrentScope = () => active.current;
  useEffect(() => {
    active.current = true;
    return () => {
      active.current = false;
    };
  }, []);
  return useMutation({
    mutationFn: async (file: File | null) => {
      if (!file) {
        const response = await api.DELETE('/api/v1/recipes/{id}/image', {
          params: { path: { id } },
        });
        if (!response.response.ok) throw new RecipeImageError(response.response.status);
        return null;
      }
      const form = new FormData();
      form.append('file', file);
      const response = await api.PUT('/api/v1/recipes/{id}/image', {
        params: { path: { id } },
        // OpenAPI represents binary files as strings; serialize the actual File as multipart.
        body: { file: file.name },
        bodySerializer: () => form,
      });
      if (!response.response.ok || !response.data) {
        throw new RecipeImageError(response.response.status);
      }
      return response.data;
    },
    onSuccess: async (image) => {
      if (!isCurrentScope()) return;
      await client.cancelQueries({ queryKey: ['recipe-images', scope, id] });
      if (!isCurrentScope()) return;
      client.removeQueries({ queryKey: ['recipe-images', scope, id] });
      client.setQueryData<RecipeDto | null>(queryKeys.recipes.detail(id), (recipe) =>
        recipe ? { ...recipe, image } : recipe,
      );
      await client.invalidateQueries({ queryKey: queryKeys.recipes.all() });
    },
    onError: async (error) => {
      if (!isCurrentScope()) return;
      if (error instanceof RecipeImageError && [401, 403, 404].includes(error.status)) {
        await client.cancelQueries({ queryKey: ['recipe-images', scope, id] });
        client.removeQueries({ queryKey: ['recipe-images', scope, id] });
        await client.invalidateQueries({ queryKey: queryKeys.recipes.detail(id) });
      }
    },
  });
}
