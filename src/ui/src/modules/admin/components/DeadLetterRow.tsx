import { useTranslation } from 'react-i18next';

import { RetriedBadge } from './RetriedBadge';
import { RetryButton } from './RetryButton';
import { ViewPayloadButton } from './ViewPayloadButton';

import type { PayloadView } from '../api/hooks/useDeliveryDeadLetters';
import type { QueryKey, UseQueryOptions } from '@tanstack/react-query';

interface DeadLetterRowProps<TKey extends QueryKey> {
  name: string;
  /** Type, channel and recipient on one muted line. */
  detail: string;
  /** Local time of the last attempt or of the event. */
  time: string;
  attempts: number;
  retryOf: string | null;
  /** The full error; the row shows one truncated mono line and the View panel the rest. */
  error: string | null;
  payloadOptions: UseQueryOptions<PayloadView, Error, PayloadView, TKey>;
  pending: boolean;
  onRetry: () => void;
}

/** A failed message: what it was, when, how many tries, the error on one line, View and Retry. */
export function DeadLetterRow<TKey extends QueryKey>({
  name,
  detail,
  time,
  attempts,
  retryOf,
  error,
  payloadOptions,
  pending,
  onRetry,
}: DeadLetterRowProps<TKey>) {
  const { t } = useTranslation('admin');
  return (
    <li className="border-border flex flex-wrap items-center gap-x-4 gap-y-1 border-t px-4 py-3 md:px-6 lg:flex-nowrap">
      <div className="min-w-0 basis-full lg:basis-[32%]">
        <div className="truncate font-semibold" title={name}>
          {name}
        </div>
        <div className="text-muted-foreground truncate text-xs" title={detail}>
          {detail}
        </div>
      </div>
      <div className="text-text-2 tnum shrink-0 text-sm whitespace-nowrap">{time}</div>
      <div className="text-text-2 tnum shrink-0 text-sm whitespace-nowrap">
        {t('tries', { count: attempts })}
        <RetriedBadge retryOf={retryOf} />
      </div>
      <div
        className="text-text-2 min-w-0 basis-full truncate font-mono text-xs lg:grow lg:basis-0"
        title={error ?? undefined}
      >
        {error ?? '—'}
      </div>
      <div className="ml-auto flex shrink-0 items-center gap-1">
        <ViewPayloadButton options={payloadOptions} error={error} />
        <RetryButton pending={pending} onRetry={onRetry} />
      </div>
    </li>
  );
}
