import { useMutation, useQueryClient } from '@tanstack/react-query';

import { api } from '../client';
import { notificationsQueryKeys } from '../queryKeys';

import { channelPreferencesOptions, type ChannelPreferencesDto } from './useChannelPreferences';

interface UpdateContext {
  previous: ChannelPreferencesDto | undefined;
}

export function useUpdateChannelPreferences() {
  const queryClient = useQueryClient();

  return useMutation<undefined, Error, ChannelPreferencesDto, UpdateContext>({
    mutationFn: async (preferences) => {
      const response = await api.PUT('/api/notification-preferences', {
        body: preferences,
      });

      if (!response.response.ok) {
        throw new Error('Failed to update channel preferences');
      }

      return undefined;
    },

    onMutate: async (preferences) => {
      const { queryKey } = channelPreferencesOptions();
      await queryClient.cancelQueries({ queryKey });

      const previous = queryClient.getQueryData(queryKey);
      queryClient.setQueryData(queryKey, preferences);

      return { previous };
    },

    onError: (_err, _vars, context) => {
      if (!context) return;
      queryClient.setQueryData(channelPreferencesOptions().queryKey, context.previous);
    },

    onSettled: () => {
      void queryClient.invalidateQueries({
        queryKey: notificationsQueryKeys.channelPreferences.all(),
      });
    },
  });
}
