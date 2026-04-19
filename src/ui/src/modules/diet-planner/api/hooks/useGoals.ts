import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

import { api } from '../client';
import { queryKeys } from '../queryKeys';

export interface Goal {
  id: string;
  dailyCalorieTarget: number | null;
  proteinGrams: number | null;
  carbsGrams: number | null;
  fatGrams: number | null;
  fiberGrams: number | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface GoalData {
  dailyCalorieTarget: number | null;
  proteinGrams: number | null;
  carbsGrams: number | null;
  fatGrams: number | null;
  fiberGrams: number | null;
}

interface GoalApiResponse {
  data?: Goal;
  error?: unknown;
}

export function useGoals() {
  return useQuery({
    queryKey: queryKeys.goals.detail(),
    queryFn: async (): Promise<Goal | null> => {
      // REASON: /api/v1/goals is not yet in the generated openapi schema — regenerate schema to remove this cast
      // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-member-access
      const response = (await (api as any).GET('/api/v1/goals')) as GoalApiResponse;

      if (response.error) {
        throw new Error('Failed to fetch goals');
      }

      // Server returns null body when no goals have been set yet — that is a valid success state
      return response.data ?? null;
    },
  });
}

export function useCreateGoals() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: GoalData) => {
      // REASON: /api/v1/goals is not yet in the generated openapi schema — regenerate schema to remove this cast
      // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-member-access
      const response = (await (api as any).POST('/api/v1/goals', {
        body: data,
      })) as GoalApiResponse;

      if (response.error) {
        throw new Error('Failed to create goals');
      }

      if (!response.data) {
        throw new Error('Failed to create goals');
      }

      return response.data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.goals.detail() });
    },
  });
}

export function useUpdateGoals() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: GoalData) => {
      // REASON: /api/v1/goals is not yet in the generated openapi schema — regenerate schema to remove this cast
      // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-member-access
      const response = (await (api as any).PUT('/api/v1/goals', {
        body: data,
      })) as GoalApiResponse;

      if (response.error) {
        throw new Error('Failed to update goals');
      }

      // PUT returns 204 No Content — no body is a success
      return response.data ?? null;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.goals.detail() });
    },
  });
}
