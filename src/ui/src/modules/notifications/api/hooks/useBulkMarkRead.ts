import { useMutation, useQueryClient } from '@tanstack/react-query';

import { api } from '../client';
import { notificationsQueryKeys } from '../queryKeys';

export function useBulkMarkRead() {
  const queryClient = useQueryClient();

  return useMutation<undefined, Error, string[]>({
    mutationFn: async (ids) => {
      const response = await api.POST('/api/notifications/read', {
        body: { ids },
      });

      // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- runtime errors are not reflected in the typed shape
      if (response.error || !response.response.ok) {
        throw new Error('Failed to mark notifications as read');
      }

      return undefined;
    },

    onSettled: () => {
      void queryClient.invalidateQueries({
        queryKey: notificationsQueryKeys.notifications.all(),
      });
    },
  });
}
