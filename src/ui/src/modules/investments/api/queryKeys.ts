// Every key carries the authenticated subject and household, so a different person or household
// can never read another's cached financial rows.
export const investmentsQueryKeys = {
  all: () => ['investments'] as const,
  wallets: (subject: string | undefined, householdId: string | undefined) =>
    ['investments', 'wallets', subject, householdId] as const,
} as const;
