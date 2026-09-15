export const householdQueryKeys = {
  all: () => ['household'] as const,
  me: (subject: string | undefined) => ['household', 'me', subject] as const,
  pickable: (subject: string | undefined) => ['household', 'pickable', subject] as const,
  invitations: (id: string, subject: string | undefined) =>
    ['household', 'invitations', id, subject] as const,
} as const;
