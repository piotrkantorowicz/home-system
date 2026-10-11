import { EmptyState, PageContainer, PageHeader } from '@shared/components/ui';
import { TrendingUp } from 'lucide-react';
import { useTranslation } from 'react-i18next';

export default function InvestmentsPage() {
  const { t } = useTranslation('investments');

  return (
    <PageContainer>
      <PageHeader title={t('net_worth_title')} />
      <EmptyState
        icon={TrendingUp}
        title={t('placeholder_title')}
        description={t('placeholder_description')}
      />
    </PageContainer>
  );
}
