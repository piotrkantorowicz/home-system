import { Badge, Banner, Button, EmptyState, Pagination } from '@shared/components/ui';
import { Lock, Scale } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { errorStatus } from '../api/client';
import { useRepaymentsQuery, useSettlementQuery } from '../api/queries';
import { RecordRepaymentDialog } from '../components/RecordRepaymentDialog';
import { VoidRepaymentDialog } from '../components/VoidRepaymentDialog';
import { useBudgetAccess } from '../hooks/useBudgetAccess';

import type { RepaymentPrefill } from '../components/RecordRepaymentDialog';
import type { Repayment } from '../types';

export default function SettlementPage() {
  const { t } = useTranslation('budget');
  const { level } = useBudgetAccess();
  const query = useSettlementQuery(level === 'adult');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const repayments = useRepaymentsQuery(page, pageSize, level === 'adult');
  const [recording, setRecording] = useState<{ prefill?: RepaymentPrefill } | null>(null);
  const [voiding, setVoiding] = useState<Repayment | null>(null);

  if (level !== 'adult' || errorStatus(query.error) === 403)
    return (
      <EmptyState
        icon={Lock}
        title={t('settlement_unavailable_title')}
        description={t('settlement_unavailable_body')}
      />
    );

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-start justify-between gap-3">
        <div className="max-w-2xl">
          <h2 className="text-xl font-semibold">{t('settlement_title')}</h2>
          <p className="text-text-2 mt-1 text-sm">{t('settlement_intro')}</p>
        </div>
        {query.isSuccess && (
          <Button
            onClick={() => {
              setRecording({});
            }}
          >
            {t('record_payment')}
          </Button>
        )}
      </header>

      {query.isPending && <p role="status">{t('loading')}</p>}
      {query.isError && (
        <Banner
          variant="error"
          onRetry={() => {
            void query.refetch();
          }}
          retryLabel={t('retry')}
        >
          {t('load_error')}
        </Banner>
      )}

      {query.isSuccess && (
        <>
          {query.data.isSettled ? (
            <EmptyState icon={Scale} title={t('settled_title')} description={t('settled_body')} />
          ) : (
            <>
              <section aria-labelledby="balances-title" className="space-y-3">
                <h3 id="balances-title" className="font-semibold">
                  {t('balances')}
                </h3>
                <ul className="space-y-2">
                  {query.data.balances.map((b) => {
                    const owes = b.net.startsWith('-');
                    const zero = /^-?0\.00$/.test(b.net);
                    return (
                      <li
                        key={b.personId}
                        className="border-border flex flex-wrap items-center justify-between gap-3 rounded-xl border p-4"
                      >
                        <p className="font-semibold break-words">
                          {b.displayName}
                          {b.isFormerAdult && (
                            <Badge variant="outline" className="ml-2">
                              {t('former_adult')}
                            </Badge>
                          )}
                        </p>
                        <p className="text-sm">
                          {zero
                            ? t('balance_zero')
                            : t(owes ? 'balance_owes' : 'balance_gets', {
                                amount: owes ? b.net.slice(1) : b.net,
                                currency: query.data.currency,
                              })}
                        </p>
                      </li>
                    );
                  })}
                </ul>
              </section>

              <section aria-labelledby="suggestions-title" className="space-y-3">
                <h3 id="suggestions-title" className="font-semibold">
                  {t('suggestions')}
                </h3>
                <ol className="space-y-2">
                  {query.data.suggestions.map((s) => (
                    <li
                      key={`${s.fromPersonId}-${s.toPersonId}`}
                      className="border-border flex flex-wrap items-center justify-between gap-3 rounded-xl border p-4 text-sm"
                    >
                      {t('suggestion', {
                        from: s.fromDisplayName,
                        to: s.toDisplayName,
                        amount: s.amount,
                        currency: query.data.currency,
                      })}
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => {
                          setRecording({
                            prefill: {
                              fromPersonId: s.fromPersonId,
                              toPersonId: s.toPersonId,
                              amount: s.amount,
                            },
                          });
                        }}
                      >
                        {t('record_payment')}
                      </Button>
                    </li>
                  ))}
                </ol>
                {query.data.balances.length >= 3 && (
                  <p className="text-text-2 text-sm">{t('simplification_note')}</p>
                )}
                <p className="text-text-2 text-sm">{t('suggestions_note')}</p>
              </section>
            </>
          )}

          <section aria-labelledby="repayments-title" className="space-y-3">
            <h3 id="repayments-title" className="font-semibold">
              {t('repayments_title')}
            </h3>
            {repayments.isPending && <p role="status">{t('loading')}</p>}
            {repayments.isError && (
              <Banner
                variant="error"
                onRetry={() => {
                  void repayments.refetch();
                }}
                retryLabel={t('retry')}
              >
                {t('repayments_error')}
              </Banner>
            )}
            {repayments.isSuccess &&
              (repayments.data.items.length === 0 ? (
                <p className="text-text-2 text-sm">{t('repayments_empty')}</p>
              ) : (
                <>
                  <ul className="space-y-2">
                    {repayments.data.items.map((r) => (
                      <li
                        key={r.id}
                        className="border-border flex flex-wrap items-center justify-between gap-3 rounded-xl border p-4 text-sm"
                      >
                        <div className="space-y-1">
                          <p className={r.isVoided ? 'line-through' : undefined}>
                            {t('repayment_line', {
                              from: r.fromDisplayName,
                              to: r.toDisplayName,
                              amount: r.amount,
                              currency: query.data.currency,
                              date: r.paidOn,
                            })}
                            {r.isVoided && (
                              <Badge variant="outline" className="ml-2 no-underline">
                                {t('voided')}
                              </Badge>
                            )}
                          </p>
                          {r.note && <p className="text-text-2">{r.note}</p>}
                          <p className="text-text-2">
                            {t('repayment_added_by', { name: r.addedByDisplayName })}
                            {r.isVoided && r.voidReason
                              ? ` · ${t('reason_is', { reason: r.voidReason })}`
                              : ''}
                          </p>
                        </div>
                        {!r.isVoided && (
                          <Button
                            size="sm"
                            variant="ghost"
                            onClick={() => {
                              setVoiding(r);
                            }}
                          >
                            {t('void_repayment')}
                          </Button>
                        )}
                      </li>
                    ))}
                  </ul>
                  <Pagination
                    page={Number(repayments.data.page)}
                    pageSize={Number(repayments.data.pageSize)}
                    totalCount={Number(repayments.data.totalCount)}
                    onPageChange={setPage}
                    onPageSizeChange={(size) => {
                      setPageSize(size);
                      setPage(1);
                    }}
                  />
                </>
              ))}
          </section>

          <section aria-labelledby="how-title" className="space-y-2">
            <h3 id="how-title" className="font-semibold">
              {t('how_title')}
            </h3>
            <ul className="text-text-2 list-disc space-y-1 pl-5 text-sm">
              <li>{t('how_counts')}</li>
              <li>{t('how_future')}</li>
              <li>{t('how_excluded')}</li>
              <li>{t('how_former')}</li>
            </ul>
          </section>
        </>
      )}

      {recording && query.isSuccess && (
        <RecordRepaymentDialog
          settlement={query.data}
          {...(recording.prefill ? { prefill: recording.prefill } : {})}
          onClose={() => {
            setRecording(null);
          }}
        />
      )}
      {voiding && (
        <VoidRepaymentDialog
          repayment={voiding}
          onClose={() => {
            setVoiding(null);
          }}
        />
      )}
    </div>
  );
}
