import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

import { api } from '../client';

export interface MealSlotDto {
  id: string;
  name: string;
  defaultTime: string;
  sortOrder: number;
}

export interface MealScheduleConfigDto {
  id: string;
  userId: string;
  slots: MealSlotDto[];
  createdAt: string;
  updatedAt: string | null;
}

export interface MealSlotRequest {
  name: string;
  defaultTime: string;
}

export interface UpdateMealScheduleRequest {
  slots: MealSlotRequest[];
}

interface MealScheduleApiResponse {
  data?: MealScheduleConfigDto;
  error?: unknown;
}

export function useMealSchedule() {
  return useQuery({
    queryKey: ['meal-schedule'],
    queryFn: async (): Promise<MealScheduleConfigDto | null> => {
      // REASON: /api/v1/meal-schedule is not yet in the generated openapi schema — regenerate schema to remove this cast
      // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-member-access
      const response = (await (api as any).GET('/api/v1/meal-schedule')) as MealScheduleApiResponse;

      if (response.error) {
        throw new Error('Failed to fetch meal schedule');
      }

      return response.data ?? null;
    },
  });
}

export function useUpdateMealSchedule() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: UpdateMealScheduleRequest) => {
      // REASON: /api/v1/meal-schedule is not yet in the generated openapi schema — regenerate schema to remove this cast
      // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-member-access
      const response = (await (api as any).PUT('/api/v1/meal-schedule', {
        body: data,
      })) as MealScheduleApiResponse;

      if (response.error) {
        throw new Error('Failed to update meal schedule');
      }

      return response.data ?? null;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['meal-schedule'] });
    },
  });
}
