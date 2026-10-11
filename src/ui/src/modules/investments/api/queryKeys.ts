// Every key carries the authenticated subject and household, so a different person or household
// can never read another's cached financial rows.
export const investmentsQueryKeys = {
  all: () => ['investments'] as const,
  wallets: (subject: string | undefined, householdId: string | undefined) =>
    ['investments', 'wallets', subject, householdId] as const,
  wallet: (subject: string | undefined, householdId: string | undefined, id: string) =>
    ['investments', 'wallet', subject, householdId, id] as const,
  snapshots: (
    subject: string | undefined,
    householdId: string | undefined,
    id: string,
    page: number,
    pageSize: number,
  ) => ['investments', 'snapshots', subject, householdId, id, page, pageSize] as const,
  netWorth: (subject: string | undefined, householdId: string | undefined, scope: string) =>
    ['investments', 'net-worth', subject, householdId, scope] as const,
  netWorthHistory: (subject: string | undefined, householdId: string | undefined, scope: string) =>
    ['investments', 'net-worth-history', subject, householdId, scope] as const,
} as const;
