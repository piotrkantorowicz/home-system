import {
  Banner,
  Pagination,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@shared/components/ui';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { outboxDeadLettersOptions, useRetryOutboxMessage } from '../api/hooks/useOutboxDeadLetters';
import { formatDateTime, shortEventType } from '../utils/format';

import { RetriedBadge } from './RetriedBadge';
import { RetryButton } from './RetryButton';

interface OutboxDeadLettersProps {
  module: string;
}

/** Dead-lettered integration events of one publishing module. */
export function OutboxDeadLetters({ module }: OutboxDeadLettersProps) {
  const { t, i18n } = useTranslation('admin');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const { data, isLoading, isError, refetch } = useQuery(
    outboxDeadLettersOptions(module, { page, pageSize }),
  );
  const retry = useRetryOutboxMessage();

  // A retry can empty the last page; step back to the last page that still has rows.
  const lastPage = data ? Math.max(1, Math.ceil(data.totalCount / pageSize)) : page;
  if (page > lastPage) setPage(lastPage);

  if (isError) {
    return (
      <Banner variant="error" onRetry={() => void refetch()} retryLabel={t('reload')}>
        {t('load_error')}
      </Banner>
    );
  }

  return (
    <>
      <Table aria-label={t('events.module_table', { module })} aria-busy={isLoading}>
        <TableHeader>
          <TableRow>
            <TableHead>{t('events.event')}</TableHead>
            <TableHead>{t('events.occurred_at')}</TableHead>
            <TableHead>{t('attempts')}</TableHead>
            <TableHead>{t('last_error')}</TableHead>
            <TableHead />
          </TableRow>
        </TableHeader>
        <TableBody>
          {data?.items.map((m) => (
            <TableRow key={m.id}>
              <TableCell title={m.eventType} className="font-medium">
                {shortEventType(m.eventType)}
              </TableCell>
              <TableCell className="whitespace-nowrap">
                {formatDateTime(m.occurredAt, i18n.language)}
              </TableCell>
              <TableCell className="tnum whitespace-nowrap">
                {m.attemptCount}
                <RetriedBadge retryOf={m.retryOf} />
              </TableCell>
              <TableCell className="max-w-xs text-xs break-words">{m.lastError}</TableCell>
              <TableCell className="text-right">
                <RetryButton
                  pending={retry.isPending && retry.variables.id === m.id}
                  onRetry={() => {
                    retry.mutate({ module, id: m.id });
                  }}
                />
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
      {data && data.totalCount > pageSize && (
        <Pagination
          page={page}
          pageSize={pageSize}
          totalCount={data.totalCount}
          onPageChange={setPage}
          onPageSizeChange={(size) => {
            setPageSize(size);
            setPage(1);
          }}
        />
      )}
    </>
  );
}
