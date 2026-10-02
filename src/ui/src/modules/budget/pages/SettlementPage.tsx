import { Badge, Banner, EmptyState } from '@shared/components/ui';
import { Lock, Scale } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { errorStatus } from '../api/client';
import { useSettlementQuery } from '../api/queries';
import { useBudgetAccess } from '../hooks/useBudgetAccess';

export default function SettlementPage() {
  const { t } = useTranslation('budget');
  const { level } = useBudgetAccess();
  const query = useSettlementQuery(level === 'adult');

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
      <header className="max-w-2xl">
        <h2 className="text-xl font-semibold">{t('settlement_title')}</h2>
        <p className="text-text-2 mt-1 text-sm">{t('settlement_intro')}</p>
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
                      className="border-border rounded-xl border p-4 text-sm"
                    >
                      {t('suggestion', {
                        from: s.fromDisplayName,
                        to: s.toDisplayName,
                        amount: s.amount,
                        currency: query.data.currency,
                      })}
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
    </div>
  );
}
