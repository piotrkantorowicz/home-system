import {
  Banner,
  Button,
  Card,
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

import { useContributionMutation, useContributionsQuery, useNetWorthQuery } from '../api/queries';
import { formatDate } from '../lib/dates';
import { change, toMinor } from '../lib/money';

import type { Wallet } from '../types';

export interface ContributionListProps {
  wallet: Wallet;
}

/** The wallet's deposits and withdrawals, newest first, with its invested capital and gain. */
export function ContributionList({ wallet }: ContributionListProps) {
  const { t, i18n } = useTranslation('investments');
  const { money } = useFormat();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const query = useContributionsQuery(wallet.id, page, pageSize);
  // Invested and gain come from net worth, so they match the dashboard; archived wallets are not in it.
  const netWorth = useNetWorthQuery('household');
  const mutation = useContributionMutation();
  const [pendingDelete, setPendingDelete] = useState<{ id: string; label: string } | null>(null);
  const currency = { currency: wallet.currency };
  const items = query.data?.items ?? [];
  const entry = netWorth.data?.wallets.find((w) => w.id === wallet.id);
  const invested = entry?.invested ?? null;
  const balance = entry?.balance ?? null;
  // In the wallet's currency, so currency moves don't count as gain (§14).
  const gain = invested === null || balance === null ? null : change(balance, invested);
  const percent =
    gain !== null && invested !== null && toMinor(invested) > 0
      ? new Intl.NumberFormat(i18n.language, {
          style: 'percent',
          maximumFractionDigits: 1,
          signDisplay: 'always',
        }).format(toMinor(gain) / toMinor(invested))
      : null;
  const newest = page === 1 ? items[0]?.date : undefined;
  const balanceStale = newest !== undefined && !!entry?.balanceAsOf && newest > entry.balanceAsOf;

  return (
    <section aria-labelledby="contributions-title" className="space-y-3">
      <h2 id="contributions-title" className="text-section font-semibold">
        {t('contributions')}
      </h2>
      {invested !== null && (
        <Card className="grid gap-4 p-5 sm:grid-cols-2">
          <div>
            <p className="text-muted-foreground text-meta">{t('invested')}</p>
            <p className="text-body tnum font-semibold">{money(invested, currency)}</p>
          </div>
          <div>
            <p className="text-muted-foreground text-meta">{t('gain')}</p>
            <p
              className={cn(
                'text-body tnum font-semibold',
                gain?.startsWith('-') ? 'text-destructive' : 'text-good',
              )}
            >
              {gain === null
                ? '—'
                : `${gain.startsWith('-') ? '' : '+'}${money(gain, currency)}${percent ? ` (${percent})` : ''}`}
            </p>
          </div>
          {balanceStale && (
            <p className="text-warning text-meta sm:col-span-2" role="status">
              {t('balance_older_than_contribution')}
            </p>
          )}
        </Card>
      )}
      {query.isError && (
        <Banner
          variant="error"
          onRetry={() => {
            void query.refetch();
          }}
          retryLabel={t('retry')}
        >
          {t('contributions_load_error')}
        </Banner>
      )}
      {mutation.isError && <Banner variant="error">{t('contribution_delete_error')}</Banner>}
      {query.isSuccess && items.length === 0 && (
        <p className="text-muted-foreground">{t('no_contributions')}</p>
      )}
      {items.length > 0 && (
        <ul className="border-border divide-border divide-y rounded-xl border">
          {items.map((c) => {
            const date = formatDate(c.date, i18n.language);
            const deposit = c.direction === 'Deposit';
            const label = t('delete_contribution', {
              direction: t(`direction_${c.direction}`),
              date,
            });
            return (
              <li key={c.id} className="flex items-center gap-4 px-4 py-2 text-sm">
                <span className="w-28 shrink-0">{date}</span>
                <span className="text-text-2 w-24 shrink-0">{t(`direction_${c.direction}`)}</span>
                <span
                  className={cn('tnum w-32 shrink-0 text-right', !deposit && 'text-destructive')}
                >
                  {`${deposit ? '+' : '−'}${money(c.amount, currency)}`}
                </span>
                <span className="text-text-2 min-w-0 flex-1 truncate">{c.note}</span>
                <Button
                  variant="ghost"
                  size="icon"
                  disabled={mutation.isPending}
                  aria-label={label}
                  onClick={() => {
                    setPendingDelete({ id: c.id, label });
                  }}
                >
                  <Trash2 className="size-4" />
                </Button>
              </li>
            );
          })}
        </ul>
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
            <DialogTitle>{t('delete_contribution_confirm_title')}</DialogTitle>
            <DialogDescription>{pendingDelete?.label}</DialogDescription>
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
                  { kind: 'delete', walletId: wallet.id, contributionId: pendingDelete.id },
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
