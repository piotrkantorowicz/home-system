import { Card, CardContent, CardHeader, CardTitle } from '@shared/components/ui';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import { useAccountsQuery, useBudgetQuery } from '../api/queries';

export default function BudgetPage() {
  const { t } = useTranslation('budget');
  const budget = useBudgetQuery();
  const accounts = useAccountsQuery();
  const active = accounts.data?.filter((a) => !a.isArchived) ?? [];
  return (
    <div className="space-y-4">
      <p className="text-text-2 text-sm">{t('overview_intro')}</p>
      <Card>
        <CardHeader>
          <CardTitle>{t('overview_envelopes')}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-2">
          <p>{t('overview_currency', { currency: budget.data?.currency })}</p>
          {accounts.isSuccess && <p>{t('overview_count', { count: active.length })}</p>}
          <Link to="/budget/envelopes" className="text-primary font-medium underline">
            {t('manage_envelopes')}
          </Link>
          <Link to="/budget/expenses" className="text-primary ml-4 font-medium underline">
            {t('view_expenses')}
          </Link>
        </CardContent>
      </Card>
    </div>
  );
}
