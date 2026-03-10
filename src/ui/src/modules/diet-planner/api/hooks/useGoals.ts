import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '../client';

export type Goal = {
  id: string;
  dailyCalorieTarget: number | null;
  proteinGrams: number | null;
  carbsGrams: number | null;
  fatGrams: number | null;
  fiberGrams: number | null;
  createdAt: string;
  updatedAt: string | null;
};

export type GoalData = {
  dailyCalorieTarget: number | null;
  proteinGrams: number | null;
  carbsGrams: number | null;
  fatGrams: number | null;
  fiberGrams: number | null;
};

export function useGoals() {
  return useQuery({
    queryKey: ['goals'],
    queryFn: async (): Promise<Goal> => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const response = await (api as any).GET('/api/v1/goals');

      if (response.error) {
        throw new Error('Failed to fetch goals');
      }

      return response.data as Goal;
    },
  });
}

export function useCreateGoals() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: GoalData) => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const response = await (api as any).POST('/api/v1/goals', {
        body: data,
      });

      if (response.error) {
        throw new Error('Failed to create goals');
      }

      return response.data as Goal;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['goals'] });
    },
  });
}

export function useUpdateGoals() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: GoalData) => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const response = await (api as any).PUT('/api/v1/goals', {
        body: data,
      });

      if (response.error) {
        throw new Error('Failed to update goals');
      }

      return response.data as Goal;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['goals'] });
    },
  });
}
