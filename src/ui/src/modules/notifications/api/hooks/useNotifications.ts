import { keepPreviousData, queryOptions, useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';

import { api } from '../client';
import { notificationsQueryKeys } from '../queryKeys';

import type { components } from '../generated/schema';

export type NotificationDto = components['schemas']['NotificationDto'];
export type NotificationsPage = components['schemas']['PagedListOfNotificationDto'];

export interface UseNotificationsParams {
  page?: number;
  pageSize?: number;
  enabled?: boolean;
}

export function notificationListOptions(
  { page = 1, pageSize = 20 }: UseNotificationsParams = {},
  locale = 'en',
) {
  return queryOptions({
    queryKey: notificationsQueryKeys.notifications.list({ page, pageSize, locale }),
    queryFn: async (): Promise<NotificationsPage> => {
      const response = await api.GET('/api/notifications', {
        params: { query: { page, pageSize } },
        headers: { 'Accept-Language': locale },
      });

      // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- runtime errors are not reflected in the typed shape; data is undefined when the request fails
      if (response.error || !response.data) {
        throw new Error('Failed to fetch notifications');
      }

      return response.data;
    },
    staleTime: 30_000,
    refetchOnWindowFocus: true,
  });
}

export function useNotifications({ enabled = true, ...params }: UseNotificationsParams = {}) {
  const { i18n } = useTranslation();
  return useQuery({
    ...notificationListOptions(params, i18n.language),
    enabled,
    placeholderData: keepPreviousData,
  });
}
