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

      if (!response.data) {
        throw new Error('Failed to fetch notifications');
      }
      return response.data;
    },
    placeholderData: keepPreviousData,
    staleTime: 30_000,
    refetchOnWindowFocus: true,
  });
}
