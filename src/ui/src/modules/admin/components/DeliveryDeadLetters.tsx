import { Banner, Pagination, Skeleton } from '@shared/components/ui';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import {
  deliveryContentOptions,
  deliveryDeadLettersOptions,
  useRetryAllDeliveries,
  useRetryDelivery,
} from '../api/hooks/useDeliveryDeadLetters';
import { formatDateTime } from '../utils/format';

import { DeadLetterRow } from './DeadLetterRow';
import { DeadLetterSection } from './DeadLetterSection';
import { NothingWaiting } from './NothingWaiting';
import { RetryAllButton } from './RetryAllButton';

export function DeliveryDeadLetters() {
  const { t, i18n } = useTranslation('admin');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const { data, isLoading, isError, refetch } = useQuery(
    deliveryDeadLettersOptions({ page, pageSize }),
  );
  const retry = useRetryDelivery();
  const retryAll = useRetryAllDeliveries();

  // A retry can empty the last page; step back to the last page that still has rows.
  const lastPage = data ? Math.max(1, Math.ceil(data.totalCount / pageSize)) : page;
  if (page > lastPage) setPage(lastPage);

  const total = data?.totalCount ?? 0;

  return (
    <DeadLetterSection
      title={t('deliveries.title')}
      count={total}
      action={
        total > 0 ? (
          <RetryAllButton
            count={total}
            pending={retryAll.isPending}
            onConfirm={() => {
              retryAll.mutate();
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
      ) : data && total === 0 ? (
        <NothingWaiting />
      ) : (
        <>
          <ul aria-label={t('deliveries.title')} aria-busy={isLoading}>
            {data === undefined ? (
              <li className="px-4 pb-4 md:px-6">
                <Skeleton className="h-14 w-full" />
              </li>
            ) : null}
            {data?.items.map((d) => (
              <DeadLetterRow
                key={d.deliveryId}
                name={d.title}
                detail={`${d.type} · ${d.channel} · ${d.userId}`}
                time={formatDateTime(d.lastAttemptAt, i18n.language)}
                attempts={d.attemptCount}
                retryOf={d.retryOf}
                error={d.failureReason}
                payloadOptions={deliveryContentOptions(d.deliveryId)}
                pending={retry.isPending && retry.variables === d.deliveryId}
                onRetry={() => {
                  retry.mutate(d.deliveryId);
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
