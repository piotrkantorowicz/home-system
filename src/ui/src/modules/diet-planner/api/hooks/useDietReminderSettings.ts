import { queryOptions, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { api } from '../client';
import { queryKeys } from '../queryKeys';

import type { components } from '../generated/schema';

export type DietReminderSettingsDto = components['schemas']['DietReminderSettingsDto'];
export type DietReminderSettingsRequest = components['schemas']['DietReminderSettingsRequest'];

export function dietReminderSettingsOptions() {
  return queryOptions({
    queryKey: queryKeys.dietReminderSettings.detail(),
    queryFn: async (): Promise<DietReminderSettingsDto | null> => {
      const response = await api.GET('/api/v1/diet-reminder-settings');

      // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- openapi-typescript types 401 content as never; error is set at runtime for non-200 responses
      if (response.error) {
        throw new Error('Failed to fetch diet reminder settings');
      }

      return response.data ?? null;
    },
  });
}

export function useDietReminderSettings() {
  return useQuery(dietReminderSettingsOptions());
}

export function useUpdateDietReminderSettings() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: DietReminderSettingsRequest) => {
      const response = await api.PUT('/api/v1/diet-reminder-settings', { body: data });

      if (response.error) {
        throw new Error('Failed to update diet reminder settings');
      }
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.dietReminderSettings.detail() });
    },
  });
}
