import { useQuery } from '@tanstack/react-query';

import { api } from '../client';
import { notificationsQueryKeys } from '../queryKeys';

import type { components } from '../generated/schema';

export type ChannelPreferencesDto = components['schemas']['ChannelPreferencesDto'];

export function useChannelPreferences() {
  return useQuery({
    queryKey: notificationsQueryKeys.channelPreferences.detail(),
    queryFn: async (): Promise<ChannelPreferencesDto> => {
      const response = await api.GET('/api/notification-preferences');

      // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- runtime errors are not reflected in the typed shape; data is undefined when the request fails
      if (response.error || !response.data) {
        throw new Error('Failed to fetch channel preferences');
      }

      return response.data;
    },
    staleTime: 60_000,
  });
}
