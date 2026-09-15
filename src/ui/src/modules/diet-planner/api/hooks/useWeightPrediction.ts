import { queryOptions, useQuery } from '@tanstack/react-query';

import { api } from '../client';
import { queryKeys } from '../queryKeys';

import type { components } from '../generated/schema';

type RawWeightPrediction = components['schemas']['WeightPredictionDto'];

export interface WeightPredictionDto {
  bmr: number;
  tdee: number;
  dailyDeficit: number;
  weeklyWeightChange: number;
  estimatedGoalDate: string | null;
  currentBmi: number;
  targetBmi: number | null;
}

function normalize(raw: RawWeightPrediction): WeightPredictionDto {
  return {
    bmr: Number(raw.bmr),
    tdee: Number(raw.tdee),
    dailyDeficit: Number(raw.dailyDeficit),
    weeklyWeightChange: Number(raw.weeklyWeightChange),
    estimatedGoalDate: raw.estimatedGoalDate,
    currentBmi: Number(raw.currentBmi),
    targetBmi: raw.targetBmi !== null ? Number(raw.targetBmi) : null,
  };
}

export function weightPredictionOptions(dailyCalorieTarget: number | null) {
  return queryOptions({
    queryKey: queryKeys.weightPrediction.detail(dailyCalorieTarget),
    queryFn: async (): Promise<WeightPredictionDto | null> => {
      if (dailyCalorieTarget === null || dailyCalorieTarget <= 0) {
        return null;
      }

      const { data, response } = await api.GET('/api/v1/profile/prediction', {
        params: { query: { dailyCalorieTarget } },
      });

      if (response.status === 404) {
        return null;
      }

      if (!response.ok) {
        throw new Error('Failed to fetch weight prediction');
      }

      return data ? normalize(data) : null;
    },
  });
}

export function useWeightPrediction(dailyCalorieTarget: number | null) {
  return useQuery({
    ...weightPredictionOptions(dailyCalorieTarget),
    enabled: dailyCalorieTarget !== null && dailyCalorieTarget > 0,
  });
}
