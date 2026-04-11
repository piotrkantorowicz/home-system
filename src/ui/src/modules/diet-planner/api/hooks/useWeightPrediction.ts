import { useQuery } from '@tanstack/react-query';

import { api } from '../client';

import type { components } from '../generated/schema';

export type WeightPredictionDto = components['schemas']['WeightPredictionDto'];

export function useWeightPrediction(dailyCalorieTarget: number | null) {
  return useQuery({
    queryKey: ['weight-prediction', dailyCalorieTarget],
    enabled: dailyCalorieTarget !== null && dailyCalorieTarget > 0,
    queryFn: async (): Promise<WeightPredictionDto | null> => {
      if (dailyCalorieTarget === null || dailyCalorieTarget <= 0) {
        return null;
      }

      const response = await api.GET('/api/v1/profile/prediction', {
        params: { query: { dailyCalorieTarget } },
      });

      if (response.response.status === 404) {
        return null;
      }

      if (!response.response.ok) {
        throw new Error('Failed to fetch weight prediction');
      }

      return response.data ?? null;
    },
  });
}
