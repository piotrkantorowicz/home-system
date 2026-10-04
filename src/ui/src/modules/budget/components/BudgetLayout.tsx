import { Banner, EmptyState } from '@shared/components/ui';
import { Lock, Wallet } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Outlet, useMatch } from 'react-router-dom';

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
  const { t } = useTranslation('budget');
  const { level } = useBudgetAccess();
  const query = useBudgetQuery(level !== 'none');

  if (level === 'none' || errorStatus(query.error) === 403)
    return (
      <EmptyState icon={Lock} title={t('unavailable_title')} description={t('unavailable_body')} />
    );
  if (query.isPending)
    return (
      <p role="status" className="p-6">
        {t('loading')}
      </p>
    );
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

  return <Outlet />;
}

export function BudgetLayout() {
  const { t } = useTranslation('budget');
  // The overview and expense history draw their own v3 PageHeader/PageContainer; the other screens still use this frame.
  const overview = useMatch({ path: '/budget', end: true });
  const expenses = useMatch({ path: '/budget/expenses', end: true });
  const detail = useMatch({ path: '/budget/expenses/:id', end: true });
  const settlement = useMatch({ path: '/budget/settlement', end: true });
  const envelopes = useMatch({ path: '/budget/envelopes', end: true });
  return (
    <BudgetCacheBoundary>
      {overview || expenses || detail || settlement || envelopes ? (
        <BudgetGate />
      ) : (
        <main className="mx-auto w-full max-w-4xl px-4 py-6 md:px-8">
          <h1 className="mb-6 text-3xl font-bold break-words">{t('budget_nav')}</h1>
          <BudgetGate />
        </main>
      )}
    </BudgetCacheBoundary>
  );
}
