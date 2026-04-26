import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

import { api } from '../client';
import { queryKeys } from '../queryKeys';

export interface DietReminderSettingsDto {
  id: string;
  userId: string;
  mealRemindersEnabled: boolean;
  mealReminderLeadTimeMinutes: number;
  mealMissedGraceMinutes: number;
  waterRemindersEnabled: boolean;
  waterReminderIntervalMinutes: number;
  waterWindowStartUtc: string;
  waterWindowEndUtc: string;
  weeklySummaryEnabled: boolean;
  weeklySummaryDayOfWeekUtc: number;
  weeklySummaryTimeOfDayUtc: string;
  goalAlertsEnabled: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface DietReminderSettingsRequest {
  mealRemindersEnabled: boolean;
  mealReminderLeadTimeMinutes: number;
  mealMissedGraceMinutes: number;
  waterRemindersEnabled: boolean;
  waterReminderIntervalMinutes: number;
  waterWindowStartUtc: string;
  waterWindowEndUtc: string;
  weeklySummaryEnabled: boolean;
  weeklySummaryDayOfWeekUtc: number;
  weeklySummaryTimeOfDayUtc: string;
  goalAlertsEnabled: boolean;
}

interface DietReminderSettingsApiResponse {
  data?: DietReminderSettingsDto;
  error?: unknown;
}

export function useDietReminderSettings() {
  return useQuery({
    queryKey: queryKeys.dietReminderSettings.detail(),
    queryFn: async (): Promise<DietReminderSettingsDto | null> => {
      // REASON: /api/v1/diet-reminder-settings is not yet in the generated openapi schema — regenerate after #155 merges
      // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-member-access
      const response = (await (api as any).GET(
        '/api/v1/diet-reminder-settings',
      )) as DietReminderSettingsApiResponse;

      if (response.error) {
        throw new Error('Failed to fetch diet reminder settings');
      }

      return response.data ?? null;
    },
  });
}

export function useUpdateDietReminderSettings() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: DietReminderSettingsRequest) => {
      // REASON: /api/v1/diet-reminder-settings is not yet in the generated openapi schema — regenerate after #155 merges
      // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-member-access
      const response = (await (api as any).PUT('/api/v1/diet-reminder-settings', {
        body: data,
      })) as DietReminderSettingsApiResponse;

      if (response.error) {
        throw new Error('Failed to update diet reminder settings');
      }
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.dietReminderSettings.detail() });
    },
  });
}
