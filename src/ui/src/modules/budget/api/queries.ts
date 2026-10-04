import { useHousehold } from '@modules/household';
import {
  queryOptions,
  useMutation,
  useQueries,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query';
import { useAuth } from 'react-oidc-context';

import { api, checkResponse } from './client';
import { budgetQueryKeys } from './queryKeys';

import type {
  AccountVisibility,
  BudgetCurrency,
  ExpenseFilters,
  ExpenseInput,
  RepaymentInput,
} from '../types';

export function budgetOptions(subject: string | undefined, householdId: string | undefined) {
  return queryOptions({
    queryKey: budgetQueryKeys.budget(subject, householdId),
    queryFn: async ({ signal }) => {
      const result = await api.GET('/api/budget', { signal });
      // Not initialised yet: a real "setup required" state, not an error and not empty data.
      if (result.response.status === 404) return null;
      checkResponse(result);
      if (!result.data) throw new Error('Missing budget response');
      return result.data;
    },
  });
}

export function useBudgetQuery(enabled = true) {
  const auth = useAuth();
  const { household } = useHousehold();
  return useQuery({
    ...budgetOptions(auth.user?.profile.sub, household?.id),
    enabled: enabled && auth.isAuthenticated && !!household,
  });
}

export function accountsOptions(subject: string | undefined, householdId: string | undefined) {
  return queryOptions({
    queryKey: budgetQueryKeys.accounts(subject, householdId),
    queryFn: async ({ signal }) => {
      // Archived envelopes are returned too (they must be restorable); the page groups them.
      const result = await api.GET('/api/budget/accounts', {
        params: { query: { page: 1, pageSize: 100, includeArchived: true } },
        signal,
      });
      checkResponse(result);
      return result.data?.items ?? [];
    },
  });
}

export function useAccountsQuery(enabled = true) {
  const auth = useAuth();
  const { household } = useHousehold();
  return useQuery({
    ...accountsOptions(auth.user?.profile.sub, household?.id),
    enabled: enabled && auth.isAuthenticated && !!household,
  });
}

export function expensesOptions(
  subject: string | undefined,
  householdId: string | undefined,
  filters: ExpenseFilters,
) {
  return queryOptions({
    queryKey: budgetQueryKeys.expenses(subject, householdId, { ...filters }),
    queryFn: async ({ signal }) => {
      const result = await api.GET('/api/budget/expenses', { params: { query: filters }, signal });
      checkResponse(result);
      if (!result.data) throw new Error('Missing expenses response');
      return result.data;
    },
  });
}

export function useExpensesQuery(filters: ExpenseFilters, enabled = true) {
  const auth = useAuth();
  const { household } = useHousehold();
  return useQuery({
    ...expensesOptions(auth.user?.profile.sub, household?.id, filters),
    enabled: enabled && auth.isAuthenticated && !!household,
  });
}

/** Newest `limit` expenses across the given envelopes: the endpoint filters by one envelope only. */
export function useLatestExpensesQuery(
  accountIds: string[],
  range: { from: string; to: string },
  limit: number,
  enabled = true,
) {
  const auth = useAuth();
  const { household } = useHousehold();
  return useQueries({
    queries: accountIds.map((accountId) => ({
      ...expensesOptions(auth.user?.profile.sub, household?.id, {
        ...range,
        accountId,
        page: 1,
        pageSize: limit,
      }),
      enabled: enabled && auth.isAuthenticated && !!household,
    })),
    combine: (results) => ({
      isPending: results.some((r) => r.isPending),
      isError: results.some((r) => r.isError),
      refetch: () => {
        results.forEach((r) => {
          void r.refetch();
        });
      },
      items: results
        .flatMap((r) => r.data?.items ?? [])
        .sort(
          (a, b) =>
            b.occurredOn.localeCompare(a.occurredOn) || b.createdAt.localeCompare(a.createdAt),
        )
        .slice(0, limit),
    }),
  });
}

export function expenseOptions(
  subject: string | undefined,
  householdId: string | undefined,
  id: string,
) {
  return queryOptions({
    queryKey: budgetQueryKeys.expense(subject, householdId, id),
    queryFn: async ({ signal }) => {
      const result = await api.GET('/api/budget/expenses/{id}', {
        params: { path: { id } },
        signal,
      });
      checkResponse(result);
      if (!result.data) throw new Error('Missing expense response');
      return result.data;
    },
  });
}

export function useExpenseQuery(id: string) {
  const auth = useAuth();
  const { household } = useHousehold();
  return useQuery({
    ...expenseOptions(auth.user?.profile.sub, household?.id, id),
    enabled: auth.isAuthenticated && !!household,
  });
}

export function settlementOptions(subject: string | undefined, householdId: string | undefined) {
  return queryOptions({
    queryKey: budgetQueryKeys.settlement(subject, householdId),
    queryFn: async ({ signal }) => {
      const result = await api.GET('/api/budget/settlement', { signal });
      checkResponse(result);
      if (!result.data) throw new Error('Missing settlement response');
      return result.data;
    },
  });
}

export function useSettlementQuery(enabled = true) {
  const auth = useAuth();
  const { household } = useHousehold();
  return useQuery({
    ...settlementOptions(auth.user?.profile.sub, household?.id),
    enabled: enabled && auth.isAuthenticated && !!household,
  });
}

export function repaymentsOptions(
  subject: string | undefined,
  householdId: string | undefined,
  page: number,
  pageSize: number,
) {
  return queryOptions({
    queryKey: budgetQueryKeys.repayments(subject, householdId, page, pageSize),
    queryFn: async ({ signal }) => {
      const result = await api.GET('/api/budget/settlements', {
        params: { query: { page, pageSize } },
        signal,
      });
      checkResponse(result);
      if (!result.data) throw new Error('Missing repayments response');
      return result.data;
    },
  });
}

export function useRepaymentsQuery(page: number, pageSize: number, enabled = true) {
  const auth = useAuth();
  const { household } = useHousehold();
  return useQuery({
    ...repaymentsOptions(auth.user?.profile.sub, household?.id, page, pageSize),
    enabled: enabled && auth.isAuthenticated && !!household,
  });
}

type RepaymentAction =
  | { kind: 'record'; requestId: string; input: RepaymentInput }
  | { kind: 'void'; id: string; expectedRevision: number; reason: string };

async function mutateRepayment(action: RepaymentAction) {
  const result =
    action.kind === 'record'
      ? await api.POST('/api/budget/settlements', {
          body: { clientRequestId: action.requestId, ...action.input },
        })
      : await api.POST('/api/budget/settlements/{id}/void', {
          params: { path: { id: action.id } },
          body: { expectedRevision: action.expectedRevision, reason: action.reason },
        });
  checkResponse(result);
  return result.data;
}

export function useRepaymentMutation() {
  const client = useQueryClient();
  return useMutation({
    mutationFn: mutateRepayment,
    // Balances, suggestions and the history all hang off the budget key; a 409 refreshes them too.
    onSettled: async () => {
      await client.invalidateQueries({ queryKey: budgetQueryKeys.all() });
    },
  });
}

export type SummaryScope = 'shared' | 'personal';

export function summaryOptions(
  subject: string | undefined,
  householdId: string | undefined,
  month: string,
  scope: SummaryScope,
  owner: string | null,
) {
  return queryOptions({
    queryKey: budgetQueryKeys.summary(subject, householdId, month, scope, owner),
    queryFn: async ({ signal }) => {
      const result = await api.GET('/api/budget/summary', {
        params: { query: { month, scope, ...(owner ? { ownerPersonId: owner } : {}) } },
        signal,
      });
      checkResponse(result);
      if (!result.data) throw new Error('Missing summary response');
      return result.data;
    },
  });
}

export function useSummaryQuery(
  month: string,
  scope: SummaryScope,
  owner: string | null,
  enabled = true,
) {
  const auth = useAuth();
  const { household } = useHousehold();
  return useQuery({
    ...summaryOptions(auth.user?.profile.sub, household?.id, month, scope, owner),
    enabled: enabled && auth.isAuthenticated && !!household,
  });
}

type LimitAction =
  | {
      kind: 'set';
      accountId: string;
      month: string;
      amount: string;
      expectedRevision: number | null;
    }
  | { kind: 'clear'; accountId: string; month: string; expectedRevision: number };

async function mutateLimit(action: LimitAction) {
  const params = { path: { id: action.accountId, month: action.month } };
  const result =
    action.kind === 'set'
      ? await api.PUT('/api/budget/accounts/{id}/limits/{month}', {
          params,
          body: { amount: action.amount, expectedRevision: action.expectedRevision },
        })
      : await api.DELETE('/api/budget/accounts/{id}/limits/{month}', {
          params: { ...params, query: { expectedRevision: action.expectedRevision } },
        });
  checkResponse(result);
}

export function useLimitMutation() {
  const client = useQueryClient();
  return useMutation({
    mutationFn: mutateLimit,
    // The summary carries each limit and its revision; a 409 refreshes it too.
    onSettled: async () => {
      await client.invalidateQueries({ queryKey: budgetQueryKeys.all() });
    },
  });
}

type ExpenseAction =
  | { kind: 'create'; requestId: string; input: ExpenseInput }
  | {
      kind: 'update';
      requestId: string;
      id: string;
      expectedRevision: number;
      reason: string;
      input: ExpenseInput;
    }
  | { kind: 'void'; requestId: string; id: string; expectedRevision: number; reason: string };

async function mutateExpense(action: ExpenseAction) {
  const result = await (() => {
    if (action.kind === 'void')
      return api.POST('/api/budget/expenses/{id}/void', {
        params: { path: { id: action.id } },
        body: {
          clientRequestId: action.requestId,
          expectedRevision: action.expectedRevision,
          reason: action.reason,
        },
      });
    const { input } = action;
    const fields = {
      amount: input.amount,
      occurredOn: input.occurredOn,
      category: input.category,
      fundingSource: input.fundingSource,
      paidByPersonId: input.paidByPersonId,
      participantIds: input.participantIds,
      description: input.description ?? null,
    };
    return action.kind === 'create'
      ? api.POST('/api/budget/expenses', {
          body: { clientRequestId: action.requestId, accountId: input.accountId, ...fields },
        })
      : api.PUT('/api/budget/expenses/{id}', {
          params: { path: { id: action.id } },
          body: {
            clientRequestId: action.requestId,
            expectedRevision: action.expectedRevision,
            reason: action.reason,
            ...fields,
          },
        });
  })();
  checkResponse(result);
  return result.data;
}

export function useExpenseMutation() {
  const client = useQueryClient();
  return useMutation({
    mutationFn: mutateExpense,
    // Lists, details and (later) summaries all hang off the budget key; a 409 refreshes them too.
    onSettled: async () => {
      await client.invalidateQueries({ queryKey: budgetQueryKeys.all() });
    },
  });
}

type BudgetAction =
  | { kind: 'initialize'; currency: BudgetCurrency }
  | { kind: 'create'; name: string; visibility: AccountVisibility; ownerPersonId: string | null }
  | { kind: 'rename'; id: string; name: string; expectedRevision: number }
  | { kind: 'archive' | 'restore'; id: string; expectedRevision: number };

async function mutateBudget(action: BudgetAction) {
  const result = await (() => {
    switch (action.kind) {
      case 'initialize':
        return api.POST('/api/budget', { body: { currency: action.currency } });
      case 'create':
        return api.POST('/api/budget/accounts', {
          body: {
            name: action.name,
            visibility: action.visibility,
            ownerPersonId: action.ownerPersonId,
          },
        });
      case 'rename':
        return api.PUT('/api/budget/accounts/{id}', {
          params: { path: { id: action.id } },
          body: { name: action.name, expectedRevision: action.expectedRevision },
        });
      case 'archive':
        return api.POST('/api/budget/accounts/{id}/archive', {
          params: { path: { id: action.id } },
          body: { expectedRevision: action.expectedRevision },
        });
      case 'restore':
        return api.POST('/api/budget/accounts/{id}/restore', {
          params: { path: { id: action.id } },
          body: { expectedRevision: action.expectedRevision },
        });
    }
  })();
  checkResponse(result);
  return result.data;
}

export function useBudgetMutation() {
  const client = useQueryClient();
  return useMutation({
    mutationFn: mutateBudget,
    // A 409 means someone else changed the row: refetch either way so the next attempt is current.
    onSettled: async () => {
      await client.invalidateQueries({ queryKey: budgetQueryKeys.all() });
    },
  });
}
