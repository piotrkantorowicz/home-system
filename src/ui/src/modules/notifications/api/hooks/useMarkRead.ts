import { useMutation, useQueryClient } from '@tanstack/react-query';

import { api } from '../client';
import { notificationsQueryKeys } from '../queryKeys';

import type { NotificationsPage } from './useNotifications';

interface MarkReadContext {
  previous: Map<readonly unknown[], NotificationsPage | undefined>;
}

export function useMarkRead() {
  const queryClient = useQueryClient();

  return useMutation<undefined, Error, string, MarkReadContext>({
    mutationFn: async (id: string): Promise<undefined> => {
      await api.POST('/api/notifications/{id}/read', {
        params: { path: { id } },
      });
      return undefined;
    },

    onMutate: async (id) => {
      await queryClient.cancelQueries({ queryKey: notificationsQueryKeys.notifications.all() });

      const cached = queryClient.getQueriesData<NotificationsPage>({
        queryKey: notificationsQueryKeys.notifications.all(),
      });

      const previous = new Map<readonly unknown[], NotificationsPage | undefined>();
      const now = new Date().toISOString();

      for (const [queryKey, page] of cached) {
        previous.set(queryKey, page);
        if (!page?.items) continue;
        const next: NotificationsPage = {
          ...page,
          items: page.items.map((item) =>
            item.id === id && !item.readAt ? { ...item, readAt: now } : item,
          ),
        };
        queryClient.setQueryData(queryKey, next);
      }

      return { previous };
    },

    onError: (_err, _id, context) => {
      if (!context) return;
      for (const [queryKey, page] of context.previous) {
        queryClient.setQueryData(queryKey, page);
      }
    },

    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: notificationsQueryKeys.notifications.all() });
    },
  });
}
