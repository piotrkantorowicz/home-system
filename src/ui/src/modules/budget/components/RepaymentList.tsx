import {
  Badge,
  Banner,
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  Pagination,
} from '@shared/components/ui';
import { Ellipsis } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { useRepaymentsQuery } from '../api/queries';

import type { Repayment } from '../types';

const RECENT = 3;

export interface RepaymentListProps {
  currency: string;
  onVoid: (repayment: Repayment) => void;
}

/** The latest few payments with a ⋯ menu; "Show all" switches to the paged history. */
export function RepaymentList({ currency, onVoid }: RepaymentListProps) {
  const { t } = useTranslation('budget');
  const [all, setAll] = useState(false);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const repayments = useRepaymentsQuery(all ? page : 1, all ? pageSize : 10);

  if (repayments.isPending) return <p role="status">{t('loading')}</p>;
  if (repayments.isError)
    return (
      <Banner
        variant="error"
        onRetry={() => {
          void repayments.refetch();
        }}
        retryLabel={t('retry')}
      >
        {t('repayments_error')}
      </Banner>
    );
  const { items, totalCount } = repayments.data;
  if (items.length === 0) return <p className="text-text-2 text-sm">{t('repayments_empty')}</p>;
  const total = Number(totalCount);
  const shown = all ? items : items.slice(0, RECENT);

  return (
    <>
      <ul className="space-y-2">
        {shown.map((r) => (
          <li
            key={r.id}
            className="border-border flex items-start justify-between gap-3 rounded-xl border p-4 text-sm"
          >
            <div className="min-w-0 space-y-1">
              <p className={r.isVoided ? 'line-through' : undefined}>
                {t('repayment_line', {
                  from: r.fromDisplayName,
                  to: r.toDisplayName,
                  amount: r.amount,
                  currency,
                  date: r.paidOn,
                })}
              </p>
              {r.isVoided && (
                <Badge variant="outline" className="no-underline">
                  {t('voided')}
                </Badge>
              )}
              {r.note && <p className="text-text-2">{r.note}</p>}
              <p className="text-text-2">
                {t('repayment_added_by', { name: r.addedByDisplayName })}
                {r.isVoided && r.voidReason ? ` · ${t('reason_is', { reason: r.voidReason })}` : ''}
              </p>
            </div>
            {!r.isVoided && (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="ghost" size="icon" aria-label={t('payment_actions')}>
                    <Ellipsis className="size-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem
                    className="text-destructive focus:text-destructive"
                    onSelect={() => {
                      onVoid(r);
                    }}
                  >
                    {t('void_repayment')}
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            )}
          </li>
        ))}
      </ul>
      {all ? (
        <>
          <Pagination
            page={Number(repayments.data.page)}
            pageSize={Number(repayments.data.pageSize)}
            totalCount={total}
            onPageChange={setPage}
            onPageSizeChange={(size) => {
              setPageSize(size);
              setPage(1);
            }}
          />
          <Button
            variant="link"
            className="h-auto p-0"
            onClick={() => {
              setAll(false);
            }}
          >
            {t('show_recent')}
          </Button>
        </>
      ) : (
        total > RECENT && (
          <Button
            variant="link"
            className="h-auto p-0"
            onClick={() => {
              setAll(true);
            }}
          >
            {t('show_all', { count: total })}
          </Button>
        )
      )}
    </>
  );
}
