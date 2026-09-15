import { queryOptions, useQuery } from '@tanstack/react-query';

import { api } from '../client';
import { notificationsQueryKeys } from '../queryKeys';

import type { components } from '../generated/schema';

export type UnreadCountDto = components['schemas']['UnreadCountDto'];

export function unreadCountOptions() {
  return queryOptions({
    queryKey: notificationsQueryKeys.notifications.unreadCount(),
    queryFn: async (): Promise<UnreadCountDto> => {
      const response = await api.GET('/api/notifications/unread-count');

      // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- runtime errors are not reflected in the typed shape
      if (response.error || !response.data) {
        throw new Error('Failed to fetch unread count');
      }

      return response.data;
    },
    staleTime: 30_000,
    refetchOnWindowFocus: true,
  });
}

export function useUnreadCount() {
  return useQuery(unreadCountOptions());
}
