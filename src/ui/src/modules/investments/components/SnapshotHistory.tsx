import {
  Banner,
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Pagination,
} from '@shared/components/ui';
import { useFormat } from '@shared/hooks/useFormat';
import { cn } from '@shared/lib/utils';
import { Trash2 } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { useSnapshotMutation, useSnapshotsQuery } from '../api/queries';
import { formatDate } from '../lib/dates';
import { change } from '../lib/money';

import type { Wallet } from '../types';

export interface SnapshotHistoryProps {
  wallet: Wallet;
}

/** The wallet's balances, newest first, with the change from the previous one. */
export function SnapshotHistory({ wallet }: SnapshotHistoryProps) {
  const { t, i18n } = useTranslation('investments');
  const { money } = useFormat();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const query = useSnapshotsQuery(wallet.id, page, pageSize);
  const mutation = useSnapshotMutation();
  const [pendingDelete, setPendingDelete] = useState<{ asOf: string; date: string } | null>(null);
  const currency = { currency: wallet.currency };
  const items = query.data?.items ?? [];

  return (
    <section aria-labelledby="history-title" className="space-y-3">
      <h2 id="history-title" className="text-section font-semibold">
        {t('history')}
      </h2>
      {query.isError && (
        <Banner
          variant="error"
          onRetry={() => {
            void query.refetch();
          }}
          retryLabel={t('retry')}
        >
          {t('history_load_error')}
        </Banner>
      )}
      {mutation.isError && <Banner variant="error">{t('balance_delete_error')}</Banner>}
      {query.isSuccess && items.length === 0 && (
        <p className="text-muted-foreground">{t('no_balances')}</p>
      )}
      {items.length > 0 && (
        <div className="border-border overflow-x-auto rounded-xl border">
          <table className="w-full min-w-[560px] border-collapse text-sm">
            <thead className="bg-secondary text-muted-foreground">
              <tr>
                <th scope="col" className="h-11 px-4 text-left text-xs font-semibold uppercase">
                  {t('date')}
                </th>
                <th scope="col" className="h-11 px-4 text-right text-xs font-semibold uppercase">
                  {t('balance')}
                </th>
                <th scope="col" className="h-11 px-4 text-right text-xs font-semibold uppercase">
                  {t('change')}
                </th>
                <th scope="col" className="h-11 px-4 text-right text-xs font-semibold uppercase">
                  PLN
                </th>
                <th scope="col" className="h-11 px-4 text-left text-xs font-semibold uppercase">
                  {t('note')}
                </th>
                <th scope="col" className="w-12">
                  <span className="sr-only">{t('actions')}</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {items.map((s) => {
                const delta = change(s.amount, s.previousAmount);
                const date = formatDate(s.asOf, i18n.language);
                return (
                  <tr key={s.asOf} className="border-border border-t">
                    <td className="px-4 py-2">{date}</td>
                    <td className="tnum px-4 text-right">{money(s.amount, currency)}</td>
                    <td
                      className={cn(
                        'tnum px-4 text-right',
                        delta?.startsWith('-') ? 'text-destructive' : 'text-text-2',
                      )}
                    >
                      {delta === null
                        ? '—'
                        : `${delta.startsWith('-') ? '' : '+'}${money(delta, currency)}`}
                    </td>
                    <td className="tnum text-text-2 px-4 text-right">
                      {/* PLN conversion for foreign wallets arrives with NBP rates. */}
                      {wallet.currency === 'PLN' ? money(s.amount, currency) : '—'}
                    </td>
                    <td className="text-text-2 px-4">{s.note}</td>
                    <td className="px-2">
                      <Button
                        variant="ghost"
                        size="icon"
                        disabled={mutation.isPending}
                        aria-label={t('delete_balance', { date })}
                        onClick={() => {
                          setPendingDelete({ asOf: s.asOf, date });
                        }}
                      >
                        <Trash2 className="size-4" />
                      </Button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
      {query.data && Number(query.data.totalCount) > pageSize && (
        <Pagination
          page={page}
          pageSize={pageSize}
          totalCount={Number(query.data.totalCount)}
          onPageChange={setPage}
          onPageSizeChange={(size) => {
            setPageSize(size);
            setPage(1);
          }}
        />
      )}
      <Dialog
        open={pendingDelete !== null}
        onOpenChange={(open) => {
          if (!open) setPendingDelete(null);
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {t('delete_balance_confirm_title', { date: pendingDelete?.date })}
            </DialogTitle>
            <DialogDescription>{t('delete_balance_confirm_body')}</DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              variant="ghost"
              onClick={() => {
                setPendingDelete(null);
              }}
            >
              {t('cancel')}
            </Button>
            <Button
              variant="destructive"
              disabled={mutation.isPending}
              onClick={() => {
                if (!pendingDelete) return;
                mutation.mutate(
                  { kind: 'delete', walletId: wallet.id, asOf: pendingDelete.asOf },
                  {
                    onSettled: () => {
                      setPendingDelete(null);
                    },
                  },
                );
              }}
            >
              {t('delete')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </section>
  );
}
