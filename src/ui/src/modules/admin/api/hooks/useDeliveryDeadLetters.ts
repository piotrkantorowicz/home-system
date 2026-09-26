import { useToast } from '@shared/context/ToastContext';
import { queryOptions, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';

import { api } from '../client';
import { adminQueryKeys } from '../queryKeys';

import type { components } from '../generated/schema';

type DeadLetterDeliveryWire = components['schemas']['DeadLetterDeliveryDto'];

export interface Backlog {
  deadLettered: number;
  retrying: number;
}

export interface DeadLetterDelivery {
  deliveryId: string;
  userId: string;
  type: string;
  title: string;
  channel: string;
  attemptCount: number;
  lastAttemptAt: string | null;
  failureReason: string | null;
}

export interface Page<T> {
  items: T[];
  totalCount: number;
}

function toDelivery(d: DeadLetterDeliveryWire): DeadLetterDelivery {
  return {
    deliveryId: d.deliveryId,
    userId: d.userId,
    type: d.type,
    title: d.title,
    channel: d.channel,
    attemptCount: Number(d.attemptCount),
    lastAttemptAt: d.lastAttemptAt,
    failureReason: d.failureReason,
  };
}

export function deliveryBacklogOptions() {
  return queryOptions({
    queryKey: adminQueryKeys.deliveries.backlog(),
    queryFn: async (): Promise<Backlog> => {
      const { data, error } = await api.GET('/api/admin/notifications/deliveries/summary');
      if (error) throw new Error('Failed to load delivery backlog');
      return { deadLettered: Number(data.deadLettered), retrying: Number(data.retrying) };
    },
    staleTime: 30_000,
  });
}

export function deliveryDeadLettersOptions(params: { page: number; pageSize: number }) {
  return queryOptions({
    queryKey: adminQueryKeys.deliveries.deadLetters(params),
    queryFn: async (): Promise<Page<DeadLetterDelivery>> => {
      const { data, error } = await api.GET('/api/admin/notifications/deliveries/dead-letters', {
        params: { query: params },
      });
      if (error) throw new Error('Failed to load dead-lettered deliveries');
      return { items: data.items.map(toDelivery), totalCount: Number(data.totalCount) };
    },
  });
}

export function useRetryDelivery() {
  const queryClient = useQueryClient();
  const toast = useToast();
  const { t } = useTranslation('admin');

  return useMutation({
    mutationFn: async (id: string) => {
      const { response } = await api.POST('/api/admin/notifications/deliveries/{id}/retry', {
        params: { path: { id } },
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
