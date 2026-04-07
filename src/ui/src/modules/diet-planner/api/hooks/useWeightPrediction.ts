import { useQuery } from '@tanstack/react-query';

import { api } from '../client';

export interface WeightPredictionDto {
  bmr: number;
  tdee: number;
  dailyDeficit: number;
  weeklyWeightChange: number;
  estimatedGoalDate: string | null;
  currentBmi: number;
  targetBmi: number | null;
}

interface WeightPredictionApiResponse {
  data?: WeightPredictionDto;
  // REASON: /api/v1/profile/prediction is not yet in the generated openapi schema
  error?: unknown;
  response: Response;
}

export function useWeightPrediction(dailyCalorieTarget: number | null) {
  return useQuery({
    queryKey: ['weight-prediction', dailyCalorieTarget],
    enabled: dailyCalorieTarget !== null && dailyCalorieTarget > 0,
    queryFn: async (): Promise<WeightPredictionDto | null> => {
      if (dailyCalorieTarget === null || dailyCalorieTarget <= 0) {
        return null;
      }

      // REASON: /api/v1/profile/prediction is not yet in the generated openapi schema — regenerate schema to remove this cast
      // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-member-access
      const response = (await (api as any).GET('/api/v1/profile/prediction', {
        params: { query: { dailyCalorieTarget } },
      })) as WeightPredictionApiResponse;

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
