import { useHousehold } from '@modules/household';
import {
  Banner,
  Button,
  EmptyState,
  PageContainer,
  PageHeader,
  Skeleton,
} from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { Check, Lock } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { errorStatus } from '../api/client';
import { useRepaymentMutation, useSettlementQuery } from '../api/queries';
import { BalanceBars } from '../components/BalanceBars';
import { RecordRepaymentDialog } from '../components/RecordRepaymentDialog';
import { RepaymentList } from '../components/RepaymentList';
import { SettlementAnswerCard } from '../components/SettlementAnswerCard';
import { VoidRepaymentDialog } from '../components/VoidRepaymentDialog';
import { useBudgetAccess } from '../hooks/useBudgetAccess';

import type { RepaymentPrefill } from '../components/RecordRepaymentDialog';
import type { Repayment } from '../types';

interface Recording {
  prefill?: RepaymentPrefill;
  review?: boolean;
}

export default function SettlementPage() {
  const { t } = useTranslation('budget');
  const toast = useToast();
  const { level } = useBudgetAccess();
  const { myPersonId } = useHousehold();
  const query = useSettlementQuery(level === 'adult');
  const undoMutation = useRepaymentMutation();
  const [recording, setRecording] = useState<Recording | null>(null);
  const [voiding, setVoiding] = useState<Repayment | null>(null);
  // The payment this session just recorded: only that one can be undone from here.
  const [justRecorded, setJustRecorded] = useState<{ id: string; revision: number } | null>(null);

  if (level !== 'adult' || errorStatus(query.error) === 403)
    return (
      <EmptyState
        icon={Lock}
        title={t('settlement_unavailable_title')}
        description={t('settlement_unavailable_body')}
      />
    );

  const undo = () => {
    if (!justRecorded) return;
    undoMutation.mutate(
      {
        kind: 'void',
        id: justRecorded.id,
        expectedRevision: justRecorded.revision,
        reason: t('undo_reason'),
      },
      {
        onSuccess: () => {
          toast.success(t('payment_undone'));
          setJustRecorded(null);
        },
        // onSettled already refreshed everything; a stale undo must not be retried blindly.
        onError: () => {
          setJustRecorded(null);
        },
      },
    );
  };
  const undoButton = justRecorded && (
    <Button variant="outline" disabled={undoMutation.isPending} onClick={undo}>
      {t('undo')}
    </Button>
  );

  return (
    <PageContainer width="narrow">
      <PageHeader
        title={t('settlement_title')}
        subtitle={
          query.isSuccess ? t('settlement_subtitle', { currency: query.data.currency }) : undefined
        }
        actions={
          query.isSuccess ? (
            <Button
              variant="outline"
              onClick={() => {
                setRecording({});
              }}
            >
              {t('record_payment')}
            </Button>
          ) : undefined
        }
      />
      <div className="space-y-8">
        {query.isPending && <Skeleton className="h-40 w-full" />}
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
        {undoMutation.isError && <Banner variant="error">{t('undo_error')}</Banner>}

        {query.isSuccess && (
          <>
            {query.data.isSettled ? (
              <section
                aria-labelledby="settled-title"
                className="border-border flex flex-wrap items-center justify-between gap-3 rounded-xl border p-4"
              >
                <div className="flex items-start gap-3">
                  <Check aria-hidden="true" className="text-text-2 mt-0.5 size-5" />
                  <div>
                    <h2 id="settled-title" className="font-semibold">
                      {t('settled_title')}
                    </h2>
                    <p className="text-text-2 text-sm">{t('settled_body')}</p>
                  </div>
                </div>
                {undoButton}
              </section>
            ) : (
              <>
                {justRecorded && (
                  <div className="flex flex-wrap items-center justify-between gap-3">
                    <p className="text-text-2 text-sm">{t('undo_hint')}</p>
                    {undoButton}
                  </div>
                )}
                <section aria-labelledby="suggestions-title" className="space-y-3">
                  <h2 id="suggestions-title" className="sr-only">
                    {t('suggestions')}
                  </h2>
                  <ol className="space-y-3">
                    {query.data.suggestions.map((s) => (
                      <SettlementAnswerCard
                        key={`${s.fromPersonId}-${s.toPersonId}`}
                        suggestion={s}
                        currency={query.data.currency}
                        myPersonId={myPersonId}
                        onPay={() => {
                          setRecording({ prefill: s, review: true });
                        }}
                        onDifferentAmount={() => {
                          setRecording({ prefill: s });
                        }}
                      />
                    ))}
                  </ol>
                  {query.data.balances.length >= 3 && (
                    <p className="text-text-2 text-sm">{t('simplification_note')}</p>
                  )}
                  <p className="text-text-2 text-sm">{t('no_money_note')}</p>
                </section>

                <section aria-labelledby="balances-title" className="space-y-3">
                  <h2 id="balances-title" className="font-semibold">
                    {t('balances')}
                  </h2>
                  <BalanceBars balances={query.data.balances} currency={query.data.currency} />
                </section>
              </>
            )}

            <section aria-labelledby="repayments-title" className="space-y-3">
              <h2 id="repayments-title" className="font-semibold">
                {t('repayments_title')}
              </h2>
              <RepaymentList currency={query.data.currency} onVoid={setVoiding} />
            </section>

            <details className="border-border rounded-xl border p-4">
              <summary className="cursor-pointer font-semibold">{t('how_title')}</summary>
              <ul className="text-text-2 mt-3 list-disc space-y-1 pl-5 text-sm">
                <li>{t('how_counts')}</li>
                <li>{t('how_future')}</li>
                <li>{t('how_excluded')}</li>
                <li>{t('how_former')}</li>
              </ul>
            </details>
          </>
        )}
      </div>

      {recording && query.isSuccess && (
        <RecordRepaymentDialog
          settlement={query.data}
          {...(recording.prefill
            ? {
                prefill: {
                  fromPersonId: recording.prefill.fromPersonId,
                  toPersonId: recording.prefill.toPersonId,
                  amount: recording.prefill.amount,
                },
              }
            : {})}
          startAtReview={recording.review ?? false}
          onRecorded={setJustRecorded}
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
    </PageContainer>
  );
}
