import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { api } from '../client';
import { queryKeys } from '../queryKeys';

import type { components } from '../generated/schema';

export type WeightEntryDto = components['schemas']['WeightEntryDto'];
export type LogWeightEntryRequest = components['schemas']['LogWeightEntryRequest'];
export type LogWeightEntryResponse = components['schemas']['LogWeightEntryResponse'];

export interface WeightEntriesRange {
  from?: string;
  to?: string;
}

export function useWeightEntries(range: WeightEntriesRange = {}) {
  return useQuery({
    queryKey: queryKeys.weightEntries.list(range),
    queryFn: async (): Promise<WeightEntryDto[]> => {
      const query: { from?: string; to?: string } = {};
      if (range.from !== undefined) query.from = range.from;
      if (range.to !== undefined) query.to = range.to;
      const response = await api.GET('/api/v1/weight-entries', { params: { query } });

      if (!response.response.ok) {
        throw new Error('Failed to fetch weight entries');
      }

      return response.data ?? [];
    },
  });
}

export function useLogWeightEntry() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: LogWeightEntryRequest): Promise<LogWeightEntryResponse> => {
      const response = await api.POST('/api/v1/weight-entries', { body: data });

      if (!response.response.ok || !response.data) {
        throw new Error('Failed to log weight entry');
      }

      return response.data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.weightEntries.all() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.profile.detail() });
    },
  });
}

export function useDeleteWeightEntry() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string): Promise<void> => {
      const response = await api.DELETE('/api/v1/weight-entries/{id}', {
        params: { path: { id } },
      });

      if (!response.response.ok) {
        throw new Error('Failed to delete weight entry');
      }
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.weightEntries.all() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.profile.detail() });
    },
  });
}
