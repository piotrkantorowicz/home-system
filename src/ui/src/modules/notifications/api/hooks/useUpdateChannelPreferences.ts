import { useMutation, useQueryClient } from '@tanstack/react-query';

import { api } from '../client';
import { notificationsQueryKeys } from '../queryKeys';

import type { ChannelPreferencesDto } from './useChannelPreferences';

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
      const queryKey = notificationsQueryKeys.channelPreferences.detail();
      await queryClient.cancelQueries({ queryKey });

      const previous = queryClient.getQueryData<ChannelPreferencesDto>(queryKey);
      queryClient.setQueryData<ChannelPreferencesDto>(queryKey, preferences);

      return { previous };
    },

    onError: (_err, _vars, context) => {
      if (!context) return;
      queryClient.setQueryData(
        notificationsQueryKeys.channelPreferences.detail(),
        context.previous,
      );
    },

    onSettled: () => {
      void queryClient.invalidateQueries({
        queryKey: notificationsQueryKeys.channelPreferences.all(),
      });
    },
  });
}
