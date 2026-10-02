// Every key carries the authenticated subject and household, so a different person or household
// can never read another's cached financial rows.
export const budgetQueryKeys = {
  all: () => ['budget'] as const,
  budget: (subject: string | undefined, householdId: string | undefined) =>
    ['budget', 'overview', subject, householdId] as const,
  accounts: (subject: string | undefined, householdId: string | undefined) =>
    ['budget', 'accounts', subject, householdId] as const,
} as const;
