import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

import { api } from '../client';
import { queryKeys } from '../queryKeys';

export interface NotificationPreferencesDto {
  id: string;
  userId: string;
  mealReminderEnabled: boolean;
  mealReminderLeadTimeMinutes: number;
  waterReminderEnabled: boolean;
  waterReminderIntervalMinutes: number;
  weeklySummaryEnabled: boolean;
  goalMilestoneAlertsEnabled: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface NotificationPreferencesRequest {
  mealReminderEnabled: boolean;
  mealReminderLeadTimeMinutes: number;
  waterReminderEnabled: boolean;
  waterReminderIntervalMinutes: number;
  weeklySummaryEnabled: boolean;
  goalMilestoneAlertsEnabled: boolean;
}

interface NotificationPreferencesApiResponse {
  data?: NotificationPreferencesDto;
  error?: unknown;
}

export function useNotificationPreferences() {
  return useQuery({
    queryKey: queryKeys.notificationPreferences.detail(),
    queryFn: async (): Promise<NotificationPreferencesDto | null> => {
      // REASON: /api/v1/notification-preferences is not yet in the generated openapi schema — regenerate schema to remove this cast
      // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-member-access
      const response = (await (api as any).GET(
        '/api/v1/notification-preferences',
      )) as NotificationPreferencesApiResponse;

      if (response.error) {
        throw new Error('Failed to fetch notification preferences');
      }

      return response.data ?? null;
    },
  });
}

export function useUpdateNotificationPreferences() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: NotificationPreferencesRequest) => {
      // REASON: /api/v1/notification-preferences is not yet in the generated openapi schema — regenerate schema to remove this cast
      // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-member-access
      const response = (await (api as any).PUT('/api/v1/notification-preferences', {
        body: data,
      })) as NotificationPreferencesApiResponse;

      if (response.error) {
        throw new Error('Failed to update notification preferences');
      }
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.notificationPreferences.detail() });
    },
  });
}
