export const notificationsQueryKeys = {
  notifications: {
    all: () => ['notifications'] as const,
    list: (params: { page: number; pageSize: number; locale?: string }) =>
      ['notifications', 'list', params] as const,
    unreadList: (locale: string) => ['notifications', 'list', 'unread', locale] as const,
    unreadCount: () => ['notifications', 'unread-count'] as const,
  },
  channelPreferences: {
    all: () => ['notification-channel-preferences'] as const,
    detail: () => ['notification-channel-preferences', 'detail'] as const,
  },
} as const;
