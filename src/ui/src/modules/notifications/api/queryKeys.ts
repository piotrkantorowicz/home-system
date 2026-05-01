export const notificationsQueryKeys = {
  notifications: {
    all: () => ['notifications'] as const,
    list: (params: { page: number; pageSize: number }) =>
      ['notifications', 'list', params] as const,
  },
} as const;
