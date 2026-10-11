import { useHousehold } from '@modules/household';
import { queryOptions, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useAuth } from 'react-oidc-context';

import { api, checkResponse } from './client';
import { investmentsQueryKeys } from './queryKeys';

import type { NewWallet, WalletVisibility } from '../types';

export function walletsOptions(subject: string | undefined, householdId: string | undefined) {
  return queryOptions({
    queryKey: investmentsQueryKeys.wallets(subject, householdId),
    queryFn: async ({ signal }) => {
      // Archived wallets are returned too (they must be restorable); the page groups them.
      const result = await api.GET('/api/investments/wallets', {
        params: { query: { page: 1, pageSize: 100, includeArchived: true } },
        signal,
      });
      checkResponse(result);
      return result.data?.items ?? [];
    },
  });
}

export function useWalletsQuery() {
  const auth = useAuth();
  const { household } = useHousehold();
  return useQuery({
    ...walletsOptions(auth.user?.profile.sub, household?.id),
    enabled: auth.isAuthenticated && !!household,
  });
}

export type WalletAction =
  | { kind: 'create'; wallet: NewWallet }
  | { kind: 'update'; id: string; name: string; visibility: WalletVisibility; version: number }
  | { kind: 'archive' | 'restore'; id: string };

async function mutateWallet(action: WalletAction) {
  const result = await (() => {
    switch (action.kind) {
      case 'create':
        return api.POST('/api/investments/wallets', { body: action.wallet });
      case 'update':
        return api.PUT('/api/investments/wallets/{id}', {
          params: { path: { id: action.id } },
          body: { name: action.name, visibility: action.visibility, version: action.version },
        });
      case 'archive':
        return api.POST('/api/investments/wallets/{id}/archive', {
          params: { path: { id: action.id } },
        });
      case 'restore':
        return api.POST('/api/investments/wallets/{id}/restore', {
          params: { path: { id: action.id } },
        });
    }
  })();
  checkResponse(result);
  return result.data;
}

export function useWalletMutation() {
  const client = useQueryClient();
  return useMutation({
    mutationFn: mutateWallet,
    // A 409 means the list is stale too, so refresh on failure as well.
    onSettled: async () => {
      await client.invalidateQueries({ queryKey: investmentsQueryKeys.all() });
    },
  });
}
