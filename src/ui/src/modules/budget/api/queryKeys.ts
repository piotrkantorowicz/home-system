// Every key carries the authenticated subject and household, so a different person or household
// can never read another's cached financial rows.
export const budgetQueryKeys = {
  all: () => ['budget'] as const,
  budget: (subject: string | undefined, householdId: string | undefined) =>
    ['budget', 'overview', subject, householdId] as const,
  accounts: (subject: string | undefined, householdId: string | undefined) =>
    ['budget', 'accounts', subject, householdId] as const,
  expenses: (
    subject: string | undefined,
    householdId: string | undefined,
    filters: Record<string, unknown>,
  ) => ['budget', 'expenses', subject, householdId, filters] as const,
  summary: (
    subject: string | undefined,
    householdId: string | undefined,
    month: string,
    scope: string,
    owner: string | null,
  ) => ['budget', 'summary', subject, householdId, month, scope, owner] as const,
  settlement: (subject: string | undefined, householdId: string | undefined) =>
    ['budget', 'settlement', subject, householdId] as const,
  repayments: (
    subject: string | undefined,
    householdId: string | undefined,
    page: number,
    pageSize: number,
  ) => ['budget', 'repayments', subject, householdId, page, pageSize] as const,
  expense: (subject: string | undefined, householdId: string | undefined, id: string) =>
    ['budget', 'expense', subject, householdId, id] as const,
} as const;
