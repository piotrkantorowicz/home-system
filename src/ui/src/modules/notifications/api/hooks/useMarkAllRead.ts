import { useMutation, useQueryClient } from '@tanstack/react-query';

import { api } from '../client';
import { notificationsQueryKeys } from '../queryKeys';

import { fetchAllUnread } from './useUnreadNotifications';

/** Marks every unread notification read, across all pages (not just the one on screen). */
export function useMarkAllRead() {
  const queryClient = useQueryClient();

  return useMutation<undefined>({
    mutationFn: async () => {
      const ids = (await fetchAllUnread()).map((n) => n.id);
      if (ids.length === 0) return undefined;
      const response = await api.POST('/api/notifications/read', { body: { ids } });
      if (response.error || !response.response.ok) {
        throw new Error('Failed to mark notifications as read');
      }
      return undefined;
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: notificationsQueryKeys.notifications.all() });
    },
  });
}
