import { Banner, EmptyState, PageContainer } from '@shared/components/ui';
import { Lock, Wallet } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Outlet } from 'react-router-dom';

import { errorStatus } from '../api/client';
import { useBudgetQuery } from '../api/queries';
import { useBudgetAccess } from '../hooks/useBudgetAccess';

import { BudgetCacheBoundary } from './BudgetCacheBoundary';
import { BudgetSetup } from './BudgetSetup';

/**
 * Gate for every Budget screen. Loading, setup-required, forbidden and load failure are distinct
 * states; nothing else renders — and no cached rows — until the server has said yes.
 */
function BudgetGate() {
  const state = useBudgetGateState();
  return state === null ? <Outlet /> : <PageContainer>{state}</PageContainer>;
}

/** The screen to show instead of the page, or null once the server has said yes. */
function useBudgetGateState() {
  const { t } = useTranslation('budget');
  const { level } = useBudgetAccess();
  const query = useBudgetQuery(level !== 'none');

  if (level === 'none' || errorStatus(query.error) === 403)
    return (
      <EmptyState icon={Lock} title={t('unavailable_title')} description={t('unavailable_body')} />
    );
  if (query.isPending) return <p role="status">{t('loading')}</p>;
  if (query.isError)
    return (
      <Banner
        variant="error"
        onRetry={() => {
          void query.refetch();
        }}
        retryLabel={t('retry')}
      >
        {t('load_error')}
      </Banner>
    );
  if (query.data === null)
    return level === 'adult' ? (
      <section aria-labelledby="setup-title" className="space-y-4">
        <h2 id="setup-title" className="text-xl font-semibold">
          {t('setup_title')}
        </h2>
        <BudgetSetup />
      </section>
    ) : (
      <EmptyState icon={Wallet} title={t('ask_adult_title')} description={t('ask_adult_body')} />
    );

  return null;
}

export function BudgetLayout() {
  return (
    <BudgetCacheBoundary>
      <BudgetGate />
    </BudgetCacheBoundary>
  );
}
