import { useToast } from '@shared/context/ToastContext';
import { queryOptions, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';

import { api } from '../client';
import { adminQueryKeys } from '../queryKeys';

import type { Backlog, Page, PayloadView } from './useDeliveryDeadLetters';

export interface ModuleBacklog extends Backlog {
  module: string;
}

export interface OutboxDeadLetter {
  id: string;
  eventType: string;
  occurredAt: string;
  attemptCount: number;
  lastError: string | null;
  /** The earlier outbox row this one retried; set when an admin retried it before. */
  retryOf: string | null;
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
      if (error) throw new Error('Failed to load dead-lettered events');
      return {
        items: data.items.map((m) => ({
          id: m.id,
          eventType: m.eventType,
          occurredAt: m.occurredAt,
          attemptCount: Number(m.attemptCount),
          lastError: m.lastError,
          retryOf: m.retryOf,
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

export function useRetryAllOutboxMessages() {
  const queryClient = useQueryClient();
  const toast = useToast();
  const { t } = useTranslation('admin');

  return useMutation({
    mutationFn: async (module: string): Promise<number> => {
      const { data, error } = await api.POST('/api/admin/outbox/{module}/dead-letters/retry-all', {
        params: { path: { module } },
      });
      if (error) throw new Error('Retry all failed');
      return Number(data.retried);
    },
    onSuccess: (retried) => {
      toast.success(t('retry_all_queued', { count: retried }));
    },
    onError: () => {
      toast.error(t('retry_failed'));
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: adminQueryKeys.all() }),
  });
}

/** An outbox message's serialised event; fetched only when an admin opens it, never cached. */
export function outboxPayloadOptions(module: string, id: string) {
  return queryOptions({
    queryKey: adminQueryKeys.outbox.payload(module, id),
    queryFn: async (): Promise<PayloadView> => {
      const { data, error } = await api.GET(
        '/api/admin/outbox/{module}/dead-letters/{id}/payload',
        {
          params: { path: { module, id } },
        },
      );
      if (error) throw new Error('Failed to load payload');
      return { heading: data.eventType, text: null, json: data.payload };
    },
    gcTime: 0,
  });
}
