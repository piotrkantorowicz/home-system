/**
 * Tests that meal mutations correctly invalidate the nutrition-summary cache,
 * forcing a refetch so progress bars reflect the latest data.
 */
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderHook, waitFor, act } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, it, expect, beforeEach } from 'vitest';

import { server } from '@/test/mocks/server';
import { queryKeys } from '../queryKeys';

import { useCreateMeal, useDeleteMeal, useNutritionSummary, useUpdateMeal } from './useMeals';

import type { ReactNode } from 'react';

const BASE = 'http://localhost:5000';

function createTestClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        staleTime: 1000 * 60 * 5,
      },
    },
  });
}

function createWrapper(client: QueryClient) {
  return function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
  };
}

describe('nutrition summary cache invalidation', () => {
  const range = { from: '2024-01-15', to: '2024-01-21' };

  beforeEach(() => {
    server.use(
      http.get(`${BASE}/api/v1/meals/nutrition-summary`, () =>
        HttpResponse.json([
          { date: '2024-01-15', calories: 450, protein: 20, carbs: 60, fat: 10, fiber: 5 },
        ]),
      ),
      http.get(`${BASE}/api/v1/meals`, () => HttpResponse.json([])),
      http.delete(`${BASE}/api/v1/meals/:id`, () => new HttpResponse(null, { status: 204 })),
      http.post(`${BASE}/api/v1/meals`, () =>
        HttpResponse.json('new-meal-id', { status: 201 }),
      ),
      http.put(`${BASE}/api/v1/meals/:id`, () => new HttpResponse(null, { status: 204 })),
    );
  });

  it('invalidateQueries refreshes nutrition summary data', async () => {
    const client = createTestClient();
    const wrapper = createWrapper(client);

    const { result } = renderHook(() => useNutritionSummary(range), { wrapper });

    await waitFor(() => {
      expect(result.current.data?.[0]?.calories).toBe(450);
    });

    server.use(
      http.get(`${BASE}/api/v1/meals/nutrition-summary`, () => HttpResponse.json([])),
    );

    act(() => {
      void client.invalidateQueries({ queryKey: queryKeys.nutritionSummary.all() });
    });

    await waitFor(() => {
      expect(result.current.data).toEqual([]);
    });
  });

  it('useDeleteMeal invalidates nutrition summary', async () => {
    const client = createTestClient();
    const wrapper = createWrapper(client);

    const { result: nutritionResult } = renderHook(() => useNutritionSummary(range), { wrapper });
    const { result: deleteResult } = renderHook(() => useDeleteMeal(), { wrapper });

    await waitFor(() => {
      expect(nutritionResult.current.data?.[0]?.calories).toBe(450);
    });

    server.use(
      http.get(`${BASE}/api/v1/meals/nutrition-summary`, () => HttpResponse.json([])),
    );

    await act(async () => {
      await deleteResult.current.mutateAsync('some-meal-id');
    });

    await waitFor(() => {
      expect(nutritionResult.current.data).toEqual([]);
    });
  });

  it('useCreateMeal invalidates nutrition summary', async () => {
    const client = createTestClient();
    const wrapper = createWrapper(client);

    const { result: nutritionResult } = renderHook(() => useNutritionSummary(range), { wrapper });
    const { result: createResult } = renderHook(() => useCreateMeal(), { wrapper });

    await waitFor(() => {
      expect(nutritionResult.current.data?.[0]?.calories).toBe(450);
    });

    server.use(
      http.get(`${BASE}/api/v1/meals/nutrition-summary`, () =>
        HttpResponse.json([
          { date: '2024-01-15', calories: 900, protein: 40, carbs: 120, fat: 20, fiber: 10 },
        ]),
      ),
    );

    await act(async () => {
      await createResult.current.mutateAsync({
        date: '2024-01-15',
        mealType: 'breakfast',
        recipeId: 'recipe-1',
        servings: 2,
        notes: '',
        mealTime: null,
        sequenceOrder: null,
      });
    });

    await waitFor(() => {
      expect(nutritionResult.current.data?.[0]?.calories).toBe(900);
    });
  });

  it('useUpdateMeal invalidates nutrition summary', async () => {
    const client = createTestClient();
    const wrapper = createWrapper(client);

    const { result: nutritionResult } = renderHook(() => useNutritionSummary(range), { wrapper });
    const { result: updateResult } = renderHook(() => useUpdateMeal(), { wrapper });

    await waitFor(() => {
      expect(nutritionResult.current.data?.[0]?.calories).toBe(450);
    });

    server.use(
      http.get(`${BASE}/api/v1/meals/nutrition-summary`, () =>
        HttpResponse.json([
          { date: '2024-01-15', calories: 600, protein: 30, carbs: 80, fat: 15, fiber: 7 },
        ]),
      ),
    );

    await act(async () => {
      await updateResult.current.mutateAsync({
        id: 'meal-1',
        data: {
          date: '2024-01-15',
          mealType: 'breakfast',
          recipeId: 'recipe-1',
          servings: 3,
          notes: '',
          mealTime: null,
          sequenceOrder: null,
        },
      });
    });

    await waitFor(() => {
      expect(nutritionResult.current.data?.[0]?.calories).toBe(600);
    });
  });
});
