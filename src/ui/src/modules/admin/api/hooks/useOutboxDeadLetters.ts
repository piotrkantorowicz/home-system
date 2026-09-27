import { useToast } from '@shared/context/ToastContext';
import { queryOptions, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';

import { api } from '../client';
import { adminQueryKeys } from '../queryKeys';

import type { Backlog, Page } from './useDeliveryDeadLetters';

export interface ModuleBacklog extends Backlog {
  module: string;
}

export interface OutboxDeadLetter {
  id: string;
  eventType: string;
  occurredAt: string;
  attemptCount: number;
  lastError: string | null;
}

export function outboxBacklogOptions() {
  return queryOptions({
    queryKey: adminQueryKeys.outbox.backlog(),
    queryFn: async (): Promise<ModuleBacklog[]> => {
      const { data, error } = await api.GET('/api/admin/outbox/summary');
      if (error) throw new Error('Failed to load outbox backlog');
      return data.map((m) => ({
        module: m.module,
        deadLettered: Number(m.deadLettered),
        retrying: Number(m.retrying),
      }));
    },
    staleTime: 30_000,
  });
}

export function outboxDeadLettersOptions(
  module: string,
  params: { page: number; pageSize: number },
) {
  return queryOptions({
    queryKey: adminQueryKeys.outbox.deadLetters(module, params),
    queryFn: async (): Promise<Page<OutboxDeadLetter>> => {
      const { data, error } = await api.GET('/api/admin/outbox/{module}/dead-letters', {
        params: { path: { module }, query: params },
      });
      if (error || !data) throw new Error('Failed to load dead-lettered events');
      return {
        items: data.items.map((m) => ({
          id: m.id,
          eventType: m.eventType,
          occurredAt: m.occurredAt,
          attemptCount: Number(m.attemptCount),
          lastError: m.lastError,
        })),
        totalCount: Number(data.totalCount),
      };
    },
  });
}

export function useRetryOutboxMessage() {
  const queryClient = useQueryClient();
  const toast = useToast();
  const { t } = useTranslation('admin');

  return useMutation({
    mutationFn: async ({ module, id }: { module: string; id: string }) => {
      const { response } = await api.POST('/api/admin/outbox/{module}/dead-letters/{id}/retry', {
        params: { path: { module, id } },
      });
      if (!response.ok) throw new Error(`Retry failed: ${response.status.toString()}`);
    },
    onSuccess: () => {
      toast.success(t('retry_queued'));
    },
    onError: () => {
      toast.error(t('retry_failed'));
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: adminQueryKeys.all() }),
  });
}
