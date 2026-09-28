export const adminQueryKeys = {
  all: () => ['admin'] as const,
  deliveries: {
    backlog: () => ['admin', 'deliveries', 'backlog'] as const,
    deadLetters: (params: { page: number; pageSize: number }) =>
      ['admin', 'deliveries', 'dead-letters', params] as const,
    content: (id: string) => ['admin', 'deliveries', 'content', id] as const,
  },
  outbox: {
    backlog: () => ['admin', 'outbox', 'backlog'] as const,
    deadLetters: (module: string, params: { page: number; pageSize: number }) =>
      ['admin', 'outbox', module, 'dead-letters', params] as const,
    payload: (module: string, id: string) => ['admin', 'outbox', module, 'payload', id] as const,
  },
} as const;
