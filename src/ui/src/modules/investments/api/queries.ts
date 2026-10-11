import { useHousehold } from '@modules/household';
import { queryOptions, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useAuth } from 'react-oidc-context';

import { api, checkResponse, errorStatus } from './client';
import { investmentsQueryKeys } from './queryKeys';

import type { NetWorthScope, NewWallet, WalletVisibility } from '../types';

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

export function walletOptions(
  subject: string | undefined,
  householdId: string | undefined,
  id: string,
) {
  return queryOptions({
    queryKey: investmentsQueryKeys.wallet(subject, householdId, id),
    queryFn: async ({ signal }) => {
      const result = await api.GET('/api/investments/wallets/{id}', {
        params: { path: { id } },
        signal,
      });
      checkResponse(result);
      if (!result.data) throw new Error('Missing wallet response');
      return result.data;
    },
    // A missing or invisible wallet is a 404 that will not go away on retry.
    retry: (count, error) => errorStatus(error) !== 404 && count < 2,
  });
}

export function useWalletQuery(id: string) {
  const auth = useAuth();
  const { household } = useHousehold();
  return useQuery({
    ...walletOptions(auth.user?.profile.sub, household?.id, id),
    enabled: auth.isAuthenticated && !!household,
  });
}

export function snapshotsOptions(
  subject: string | undefined,
  householdId: string | undefined,
  id: string,
  page: number,
  pageSize: number,
) {
  return queryOptions({
    queryKey: investmentsQueryKeys.snapshots(subject, householdId, id, page, pageSize),
    queryFn: async ({ signal }) => {
      const result = await api.GET('/api/investments/wallets/{id}/snapshots', {
        params: { path: { id }, query: { page, pageSize } },
        signal,
      });
      checkResponse(result);
      if (!result.data) throw new Error('Missing snapshots response');
      return result.data;
    },
  });
}

export function useSnapshotsQuery(id: string, page: number, pageSize: number) {
  const auth = useAuth();
  const { household } = useHousehold();
  return useQuery({
    ...snapshotsOptions(auth.user?.profile.sub, household?.id, id, page, pageSize),
    enabled: auth.isAuthenticated && !!household,
  });
}

export type SnapshotAction =
  | { kind: 'record'; walletId: string; asOf: string; amount: string; note: string | null }
  | { kind: 'delete'; walletId: string; asOf: string };

async function mutateSnapshot(action: SnapshotAction) {
  const params = { path: { id: action.walletId, asOf: action.asOf } };
  const result =
    action.kind === 'record'
      ? await api.PUT('/api/investments/wallets/{id}/snapshots/{asOf}', {
          params,
          body: { amount: action.amount, note: action.note },
        })
      : await api.DELETE('/api/investments/wallets/{id}/snapshots/{asOf}', { params });
  checkResponse(result);
}

export function useSnapshotMutation() {
  const client = useQueryClient();
  return useMutation({
    mutationFn: mutateSnapshot,
    // The wallet's latest balance and the history both change.
    onSettled: async () => {
      await client.invalidateQueries({ queryKey: investmentsQueryKeys.all() });
    },
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

export function netWorthOptions(
  subject: string | undefined,
  householdId: string | undefined,
  scope: NetWorthScope,
) {
  return queryOptions({
    queryKey: investmentsQueryKeys.netWorth(subject, householdId, scope),
    queryFn: async ({ signal }) => {
      const result = await api.GET('/api/investments/net-worth', {
        params: { query: { scope } },
        signal,
      });
      checkResponse(result);
      if (!result.data) throw new Error('Missing net worth response');
      return result.data;
    },
  });
}

export function useNetWorthQuery(scope: NetWorthScope) {
  const auth = useAuth();
  const { household } = useHousehold();
  return useQuery({
    ...netWorthOptions(auth.user?.profile.sub, household?.id, scope),
    enabled: auth.isAuthenticated && !!household,
  });
}

/** The last 12 months by month end — always includes last 31 December for "since 1 January". */
export function netWorthHistoryOptions(
  subject: string | undefined,
  householdId: string | undefined,
  scope: NetWorthScope,
) {
  return queryOptions({
    queryKey: investmentsQueryKeys.netWorthHistory(subject, householdId, scope),
    queryFn: async ({ signal }) => {
      const result = await api.GET('/api/investments/net-worth/history', {
        params: { query: { scope } },
        signal,
      });
      checkResponse(result);
      if (!result.data) throw new Error('Missing net worth history response');
      return result.data.points;
    },
  });
}

export function useNetWorthHistoryQuery(scope: NetWorthScope) {
  const auth = useAuth();
  const { household } = useHousehold();
  return useQuery({
    ...netWorthHistoryOptions(auth.user?.profile.sub, household?.id, scope),
    enabled: auth.isAuthenticated && !!household,
  });
}

export interface CheckInInput {
  asOf: string;
  entries: { walletId: string; amount: string }[];
}

/** Why the server rejected a check-in, per wallet id (from the 422 `errors`), if it said. */
export function checkInErrors(error: unknown): Record<string, string[]> | null {
  return typeof error === 'object' &&
    error !== null &&
    'errors' in error &&
    typeof error.errors === 'object' &&
    error.errors !== null
    ? (error.errors as Record<string, string[]>)
    : null;
}

export function useCheckInMutation() {
  const client = useQueryClient();
  return useMutation({
    mutationFn: async (input: CheckInInput) => {
      const result = await api.PUT('/api/investments/snapshots', {
        body: { asOf: input.asOf, entries: input.entries.map((e) => ({ ...e, note: null })) },
      });
      if (!result.response.ok) {
        const problem = result.error as { errors?: Record<string, string[]> } | undefined;
        throw Object.assign(new Error(`Check-in failed (${String(result.response.status)})`), {
          status: result.response.status,
          errors: problem?.errors ?? null,
        });
      }
      return result.data;
    },
    onSettled: async () => {
      await client.invalidateQueries({ queryKey: investmentsQueryKeys.all() });
    },
  });
}
