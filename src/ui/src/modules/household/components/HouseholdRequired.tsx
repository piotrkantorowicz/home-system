import { Banner } from '@shared/components/ui';
import { useTranslation } from 'react-i18next';
import { Navigate, useLocation } from 'react-router-dom';

import { useHousehold } from '../hooks/useHousehold';

import type { ReactNode } from 'react';

export function HouseholdRequired({ children }: { children: ReactNode }) {
  const { household, isLoading, isError, refetch } = useHousehold();
  const { t } = useTranslation('household');
  const location = useLocation();

  // Keep setup, its loading state and recovery reachable inside the app shell.
  if (location.pathname === '/household' || location.pathname === '/household/') return children;
  if (isLoading)
    return (
      <p role="status" className="p-6">
        {t('loading')}
      </p>
    );
  if (isError)
    return (
      <div className="p-6">
        <Banner variant="error" onRetry={refetch} retryLabel={t('retry')}>
          {t('load_error')}
        </Banner>
      </div>
    );
  if (!household) return <Navigate to="/household" replace />;
  return children;
}
