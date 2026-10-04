import { Banner, Pagination, Skeleton } from '@shared/components/ui';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import {
  outboxPayloadOptions,
  outboxDeadLettersOptions,
  useRetryAllOutboxMessages,
  useRetryOutboxMessage,
} from '../api/hooks/useOutboxDeadLetters';
import {
  eventDisplayName,
  formatDateTime,
  shortEventType,
  sourceDisplayName,
} from '../utils/format';

import { DeadLetterRow } from './DeadLetterRow';
import { DeadLetterSection } from './DeadLetterSection';
import { RetryAllButton } from './RetryAllButton';

interface OutboxDeadLettersProps {
  module: string;
}

/** Dead-lettered integration events of one publishing module, as one section named after it. */
export function OutboxDeadLetters({ module }: OutboxDeadLettersProps) {
  const { t, i18n } = useTranslation('admin');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const { data, isLoading, isError, refetch } = useQuery(
    outboxDeadLettersOptions(module, { page, pageSize }),
  );
  const retry = useRetryOutboxMessage();
  const retryAll = useRetryAllOutboxMessages();

  // A retry can empty the last page; step back to the last page that still has rows.
  const lastPage = data ? Math.max(1, Math.ceil(data.totalCount / pageSize)) : page;
  if (page > lastPage) setPage(lastPage);

  const total = data?.totalCount ?? 0;
  const source = sourceDisplayName(module);

  return (
    <DeadLetterSection
      title={t('events.source_title', { source })}
      count={total}
      action={
        total > 0 ? (
          <RetryAllButton
            count={total}
            pending={retryAll.isPending}
            onConfirm={() => {
              retryAll.mutate(module);
            }}
          />
        ) : null
      }
    >
      {isError ? (
        <div className="px-4 pb-4 md:px-6">
          <Banner variant="error" onRetry={() => void refetch()} retryLabel={t('reload')}>
            {t('load_error')}
          </Banner>
        </div>
      ) : (
        <>
          <ul aria-label={t('events.module_table', { module: source })} aria-busy={isLoading}>
            {data === undefined ? (
              <li className="px-4 pb-4 md:px-6">
                <Skeleton className="h-14 w-full" />
              </li>
            ) : null}
            {data?.items.map((m) => (
              <DeadLetterRow
                key={m.id}
                name={eventDisplayName(m.eventType)}
                detail={shortEventType(m.eventType)}
                time={formatDateTime(m.occurredAt, i18n.language)}
                attempts={m.attemptCount}
                retryOf={m.retryOf}
                error={m.lastError}
                payloadOptions={outboxPayloadOptions(module, m.id)}
                pending={retry.isPending && retry.variables.id === m.id}
                onRetry={() => {
                  retry.mutate({ module, id: m.id });
                }}
              />
            ))}
          </ul>
          {total > pageSize && (
            <div className="border-border border-t px-4 py-3 md:px-6">
              <Pagination
                page={page}
                pageSize={pageSize}
                totalCount={total}
                onPageChange={setPage}
                onPageSizeChange={(size) => {
                  setPageSize(size);
                  setPage(1);
                }}
              />
            </div>
          )}
        </>
      )}
    </DeadLetterSection>
  );
}
