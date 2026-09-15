import { queryOptions, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { api } from '../client';
import { queryKeys } from '../queryKeys';

import type { components } from '../generated/schema';

export type MealSlotDto = components['schemas']['MealSlotDto'];
export type MealScheduleConfigDto = components['schemas']['MealScheduleConfigDto'];
export type MealSlotRequest = components['schemas']['MealSlotRequest'];
export type UpdateMealScheduleRequest = components['schemas']['UpdateMealScheduleRequest'];

export function mealScheduleOptions() {
  return queryOptions({
    queryKey: queryKeys.mealSchedule.detail(),
    queryFn: async (): Promise<MealScheduleConfigDto | null> => {
      const response = await api.GET('/api/v1/meal-schedule');

      // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- openapi-typescript types 401 content as never; error is set at runtime for non-200 responses
      if (response.error) {
        throw new Error('Failed to fetch meal schedule');
      }

      // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- REASON: the server answers 200 with a JSON null body when no schedule exists; the schema declares the body required
      return response.data ?? null;
    },
  });
}

export function useMealSchedule() {
  return useQuery(mealScheduleOptions());
}

export function useUpdateMealSchedule() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: UpdateMealScheduleRequest) => {
      const response = await api.PUT('/api/v1/meal-schedule', { body: data });

      if (!response.response.ok) {
        throw new Error('Failed to update meal schedule');
      }

      return null;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.mealSchedule.detail() });
    },
  });
}
