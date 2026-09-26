import { queryOptions, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useAuth } from 'react-oidc-context';

import { api, checkResponse } from './client';
import { householdQueryKeys } from './queryKeys';

import type { HouseholdRole } from '../types';

export function householdOptions(subject: string | undefined) {
  return queryOptions({
    queryKey: householdQueryKeys.me(subject),
    queryFn: async ({ signal }) => {
      // Sync before loading the authoritative household state and admitting feature routes.
      // Signing in never joins a household by itself — only an explicit invitation accept does.
      const sync = await api.POST('/api/persons/me/sync', { signal });
      checkResponse(sync);
      const myPersonId = sync.data?.personId ?? null;

      const result = await api.GET('/api/households/me', { signal });
      if (result.response.status === 404) return { household: null, myPersonId };
      checkResponse(result);
      if (!result.data) throw new Error('Missing household response');

      return { household: result.data, myPersonId };
    },
  });
}

export function useHouseholdQuery() {
  const auth = useAuth();
  return useQuery({
    ...householdOptions(auth.user?.profile.sub),
    enabled: auth.isAuthenticated,
  });
}

export function pickablePersonsOptions(subject: string | undefined) {
  return queryOptions({
    queryKey: householdQueryKeys.pickable(subject),
    queryFn: async ({ signal }) => {
      const result = await api.GET('/api/households/pickable-persons', { signal });
      checkResponse(result);
      return result.data ?? [];
    },
  });
}

export function usePickablePersons(enabled: boolean) {
  const auth = useAuth();
  return useQuery({ ...pickablePersonsOptions(auth.user?.profile.sub), enabled });
}

export function invitationsOptions(id: string, subject: string | undefined) {
  return queryOptions({
    queryKey: householdQueryKeys.invitations(id, subject),
    queryFn: async ({ signal }) => {
      const result = await api.GET('/api/households/{id}/invitations', {
        params: { path: { id } },
        signal,
      });
      checkResponse(result);
      return result.data ?? [];
    },
  });
}

export function useInvitations(id: string, enabled: boolean) {
  const auth = useAuth();
  return useQuery({ ...invitationsOptions(id, auth.user?.profile.sub), enabled });
}

export function myInvitationsOptions(subject: string | undefined) {
  return queryOptions({
    queryKey: householdQueryKeys.mine(subject),
    queryFn: async ({ signal }) => {
      const result = await api.GET('/api/households/invitations/mine', { signal });
      checkResponse(result);
      return result.data ?? [];
    },
  });
}

export function useMyInvitations(enabled: boolean) {
  const auth = useAuth();
  return useQuery({ ...myInvitationsOptions(auth.user?.profile.sub), enabled });
}

type HouseholdAction =
  | { kind: 'create'; name: string }
  | { kind: 'rename'; id: string; name: string }
  | { kind: 'delete' | 'leave'; id: string }
  | { kind: 'remove'; id: string; personId: string }
  | { kind: 'role'; id: string; personId: string; role: HouseholdRole }
  | { kind: 'existing'; id: string; personId: string; role: HouseholdRole }
  | { kind: 'managed'; id: string; displayName: string; role: HouseholdRole }
  | { kind: 'invite'; id: string; email: string; role: HouseholdRole }
  | { kind: 'revoke'; id: string; invitationId: string }
  | { kind: 'accept-invitation'; invitationId: string }
  | { kind: 'decline-invitation'; invitationId: string };

async function mutateHousehold(action: HouseholdAction) {
  const result = await (() => {
    if (action.kind === 'create')
      return api.POST('/api/households', { body: { name: action.name } });
    if (action.kind === 'accept-invitation' || action.kind === 'decline-invitation') {
      const verb = action.kind === 'accept-invitation' ? 'accept' : 'decline';
      return api.POST(`/api/households/invitations/{invitationId}/${verb}`, {
        params: { path: { invitationId: action.invitationId } },
      });
    }
    const params = { path: { id: action.id } };
    switch (action.kind) {
      case 'rename':
        return api.PUT('/api/households/{id}', { params, body: { name: action.name } });
      case 'delete':
        return api.DELETE('/api/households/{id}', { params });
      case 'leave':
        return api.POST('/api/households/{id}/leave', { params });
      case 'remove':
        return api.DELETE('/api/households/{id}/members/{personId}', { params: { path: action } });
      case 'role':
        return api.PUT('/api/households/{id}/members/{personId}/role', {
          params: { path: action },
          body: { role: action.role },
        });
      case 'existing':
        return api.POST('/api/households/{id}/members', {
          params,
          body: { personId: action.personId, role: action.role, nickname: null },
        });
      case 'managed':
        return api.POST('/api/households/{id}/managed-members', {
          params,
          body: { displayName: action.displayName, role: action.role, email: null, nickname: null },
        });
      case 'invite':
        return api.POST('/api/households/{id}/invitations', {
          params,
          body: { email: action.email, role: action.role },
        });
      case 'revoke':
        return api.DELETE('/api/households/{id}/invitations/{invitationId}', {
          params: { path: action },
        });
    }
  })();
  checkResponse(result);
  return result.data;
}

export function useHouseholdMutation() {
  const client = useQueryClient();
  return useMutation({
    mutationFn: mutateHousehold,
    onSuccess: async () => {
      await client.invalidateQueries({ queryKey: householdQueryKeys.all() });
      // Shared resources may now have a different household scope.
      await client.invalidateQueries({ predicate: (query) => query.queryKey[0] !== 'household' });
    },
  });
}
