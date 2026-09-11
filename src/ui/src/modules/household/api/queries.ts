import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useAuth } from 'react-oidc-context';

import { api, checkResponse } from './client';

import type { HouseholdRole } from '../types';

export const householdKeys = { all: ['household'] as const };

export function useHouseholdQuery() {
  const auth = useAuth();
  return useQuery({
    queryKey: [...householdKeys.all, 'me', auth.user?.profile.sub],
    enabled: auth.isAuthenticated,
    queryFn: async ({ signal }) => {
      // Sync person first to ensure they exist
      const sync = await api.POST('/api/persons/me/sync', { signal });
      checkResponse(sync);

      // Now check household state
      const result = await api.GET('/api/households/me', { signal });
      if (result.response.status === 404) return { household: null, joined: false };
      checkResponse(result);
      if (!result.data) throw new Error('Missing household response');
      return { household: result.data, joined: false };
    },
  });
}

export function usePickablePersons(enabled: boolean) {
  const auth = useAuth();
  return useQuery({
    queryKey: [...householdKeys.all, 'pickable', auth.user?.profile.sub],
    enabled,
    queryFn: async ({ signal }) => {
      const result = await api.GET('/api/households/pickable-persons', { signal });
      checkResponse(result);
      return result.data ?? [];
    },
  });
}

export function useInvitations(id: string, enabled: boolean) {
  const auth = useAuth();
  return useQuery({
    queryKey: [...householdKeys.all, 'invitations', id, auth.user?.profile.sub],
    enabled,
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

type HouseholdAction =
  | { kind: 'create'; name: string }
  | { kind: 'rename'; id: string; name: string }
  | { kind: 'delete' | 'leave'; id: string }
  | { kind: 'remove'; id: string; personId: string }
  | { kind: 'role'; id: string; personId: string; role: HouseholdRole }
  | { kind: 'existing'; id: string; personId: string; role: HouseholdRole }
  | { kind: 'managed'; id: string; displayName: string; role: HouseholdRole }
  | { kind: 'invite'; id: string; email: string; role: HouseholdRole }
  | { kind: 'revoke'; id: string; invitationId: string };

async function mutateHousehold(action: HouseholdAction) {
  const result = await (() => {
    if (action.kind === 'create')
      return api.POST('/api/households', { body: { name: action.name } });
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
      await client.invalidateQueries({ queryKey: householdKeys.all });
      // Shared resources may now have a different household scope.
      await client.invalidateQueries({ predicate: (query) => query.queryKey[0] !== 'household' });
    },
  });
}
