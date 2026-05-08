import { useQueryClient } from '@tanstack/react-query';
import { useEffect } from 'react';

import {
  ensureNotificationConnection,
  shutdownNotificationConnection,
  type NotificationPayload,
} from '../notificationStream';
import { notificationsQueryKeys } from '../queryKeys';

const HUB_METHOD = 'notification';
const ACK_METHOD = 'Acknowledge';

export function useNotificationStream(enabled: boolean): void {
  const queryClient = useQueryClient();

  useEffect(() => {
    if (!enabled) {
      void shutdownNotificationConnection();
      return;
    }

    const handler = (payload: NotificationPayload): void => {
      // Single invalidation: 'notifications' prefix matches both the list and
      // unreadCount query keys — TanStack treats the array prefix-style.
      void queryClient.invalidateQueries({ queryKey: notificationsQueryKeys.notifications.all() });
      void ensureNotificationConnection().then((c) =>
        c.invoke(ACK_METHOD, payload.deliveryId).catch((err: unknown) => {
          console.error('[notifications] ACK failed', err);
        }),
      );
    };

    void ensureNotificationConnection()
      .then((c) => {
        c.on(HUB_METHOD, handler);
      })
      .catch((err: unknown) => {
        console.error('[notifications] hub start failed', err);
      });

    return () => {
      void ensureNotificationConnection().then((c) => {
        c.off(HUB_METHOD, handler);
      });
    };
  }, [enabled, queryClient]);
}
