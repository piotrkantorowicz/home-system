import { useHousehold } from '@modules/household';
import { queryOptions, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useAuth } from 'react-oidc-context';

import { api, checkResponse } from './client';
import { budgetQueryKeys } from './queryKeys';

import type { AccountVisibility, BudgetCurrency } from '../types';

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
