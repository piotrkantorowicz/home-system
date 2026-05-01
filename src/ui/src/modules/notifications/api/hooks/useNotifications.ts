import { useQuery, keepPreviousData } from '@tanstack/react-query';

import { api } from '../client';
import { notificationsQueryKeys } from '../queryKeys';

import type { components } from '../generated/schema';

export type NotificationDto = components['schemas']['NotificationDto'];
export type NotificationsPage = components['schemas']['PagedListOfNotificationDto'];

export interface UseNotificationsParams {
  page?: number;
  pageSize?: number;
}

export function useNotifications({ page = 1, pageSize = 20 }: UseNotificationsParams = {}) {
  return useQuery({
    queryKey: notificationsQueryKeys.notifications.list({ page, pageSize }),
    queryFn: async (): Promise<NotificationsPage> => {
      const response = await api.GET('/api/notifications', {
        params: { query: { page, pageSize } },
      });

      // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- runtime errors are not reflected in the typed shape; data is undefined when the request fails
      if (response.error || !response.data) {
        throw new Error('Failed to fetch notifications');
      }

      return response.data;
    },
    placeholderData: keepPreviousData,
    staleTime: 30_000,
    refetchOnWindowFocus: true,
  });
}
