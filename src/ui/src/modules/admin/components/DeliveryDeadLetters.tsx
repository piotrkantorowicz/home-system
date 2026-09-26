import {
  Banner,
  EmptyState,
  Pagination,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@shared/components/ui';
import { useQuery } from '@tanstack/react-query';
import { CheckCircle2 } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { deliveryDeadLettersOptions, useRetryDelivery } from '../api/hooks/useDeliveryDeadLetters';
import { formatDateTime } from '../utils/format';

import { RetryButton } from './RetryButton';

export function DeliveryDeadLetters() {
  const { t, i18n } = useTranslation('admin');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const { data, isLoading, isError, refetch } = useQuery(
    deliveryDeadLettersOptions({ page, pageSize }),
  );
  const retry = useRetryDelivery();

  if (isError) {
    return (
      <Banner variant="error" onRetry={() => void refetch()} retryLabel={t('reload')}>
        {t('load_error')}
      </Banner>
    );
  }

  if (!isLoading && data?.items.length === 0) {
    return <EmptyState icon={CheckCircle2} title={t('deliveries.empty')} />;
  }

  return (
    <>
      <Table aria-label={t('deliveries.title')} aria-busy={isLoading}>
        <TableHeader>
          <TableRow>
            <TableHead>{t('deliveries.notification')}</TableHead>
            <TableHead>{t('deliveries.channel')}</TableHead>
            <TableHead>{t('attempts')}</TableHead>
            <TableHead>{t('last_attempt')}</TableHead>
            <TableHead>{t('last_error')}</TableHead>
            <TableHead />
          </TableRow>
        </TableHeader>
        <TableBody>
          {data?.items.map((d) => (
            <TableRow key={d.deliveryId}>
              <TableCell>
                <div className="font-medium">{d.title}</div>
                <div className="text-muted-foreground text-xs">
                  {d.type} · {d.userId}
                </div>
              </TableCell>
              <TableCell>{d.channel}</TableCell>
              <TableCell className="tnum">{d.attemptCount}</TableCell>
              <TableCell className="whitespace-nowrap">
                {formatDateTime(d.lastAttemptAt, i18n.language)}
              </TableCell>
              <TableCell className="max-w-xs text-xs break-words">{d.failureReason}</TableCell>
              <TableCell className="text-right">
                <RetryButton
                  pending={retry.isPending && retry.variables === d.deliveryId}
                  onRetry={() => {
                    retry.mutate(d.deliveryId);
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
