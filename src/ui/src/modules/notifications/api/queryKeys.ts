export const notificationsQueryKeys = {
  notifications: {
    all: () => ['notifications'] as const,
    list: (params: { page: number; pageSize: number }) =>
      ['notifications', 'list', params] as const,
  },
  channelPreferences: {
    all: () => ['notification-channel-preferences'] as const,
    detail: () => ['notification-channel-preferences', 'detail'] as const,
  },
} as const;
