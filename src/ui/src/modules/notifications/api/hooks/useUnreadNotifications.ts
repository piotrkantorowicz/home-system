import { queryOptions, useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';

import { api } from '../client';
import { notificationsQueryKeys } from '../queryKeys';

import type { NotificationDto, NotificationsPage } from './useNotifications';

const PAGE_SIZE = 100; // backend maximum

/**
 * Every unread notification, newest first. The API has no unread filter, so this walks the list
 * until it has collected the real unread total (or runs out of pages). Used by the Unread chip and
 * by "Mark all as read", so both cover the whole list rather than the visible page.
 */
export async function fetchAllUnread(locale = 'en'): Promise<NotificationDto[]> {
  const count = await api.GET('/api/notifications/unread-count');
  // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- runtime errors are not reflected in the typed shape
  if (count.error || !count.data) throw new Error('Failed to fetch unread count');
  const total = Number(count.data.total);

  const unread: NotificationDto[] = [];
  for (let page = 1; unread.length < total; page++) {
    const res = await api.GET('/api/notifications', {
      params: { query: { page, pageSize: PAGE_SIZE } },
      headers: { 'Accept-Language': locale },
    });
    // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- runtime errors are not reflected in the typed shape
    if (res.error || !res.data) throw new Error('Failed to fetch notifications');
    unread.push(...res.data.items.filter((n) => !n.readAt));
    if (!res.data.hasNextPage) break;
  }
  return unread;
}

export function unreadNotificationsOptions(enabled: boolean, locale = 'en') {
  return queryOptions({
    queryKey: notificationsQueryKeys.notifications.unreadList(locale),
    queryFn: async (): Promise<NotificationsPage> => {
      const items = await fetchAllUnread(locale);
      return { items, totalCount: items.length, page: 1, pageSize: items.length };
    },
    enabled,
    staleTime: 30_000,
    refetchOnWindowFocus: true,
  });
}

export function useUnreadNotifications(enabled: boolean) {
  const { i18n: currentLanguage } = useTranslation();
  return useQuery(unreadNotificationsOptions(enabled, currentLanguage.language));
}
