import { useUserRoles } from '@shared/auth/useUserRoles';
import { Card, EmptyState, PageContainer, PageHeader, Skeleton } from '@shared/components/ui';
import { cn } from '@shared/lib/utils';
import { useQuery } from '@tanstack/react-query';
import { ShieldAlert } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { deliveryBacklogOptions } from '../api/hooks/useDeliveryDeadLetters';
import { outboxBacklogOptions } from '../api/hooks/useOutboxDeadLetters';
import { DeadLetterSection } from '../components/DeadLetterSection';
import { DeliveryDeadLetters } from '../components/DeliveryDeadLetters';
import { NothingWaiting } from '../components/NothingWaiting';
import { OutboxDeadLetters } from '../components/OutboxDeadLetters';
import { ADMIN_ROLE } from '../constants';

export default function DeadLetters() {
  const { t } = useTranslation('admin');
  const isAdmin = useUserRoles().includes(ADMIN_ROLE);

  if (!isAdmin) {
    return (
      <PageContainer>
        <EmptyState
          icon={ShieldAlert}
          title={t('forbidden_title')}
          description={t('forbidden_body')}
        />
      </PageContainer>
    );
  }

  return <AdminDeadLetters />;
}

function AdminDeadLetters() {
  const { t } = useTranslation('admin');
  const deliveries = useQuery(deliveryBacklogOptions());
  const outbox = useQuery(outboxBacklogOptions());

  const modules = outbox.data ?? [];
  const deadEvents = modules.reduce((sum, m) => sum + m.deadLettered, 0);
  const retryingEvents = modules.reduce((sum, m) => sum + m.retrying, 0);
  const modulesWithDead = modules.filter((m) => m.deadLettered > 0);

  return (
    <PageContainer>
      <PageHeader title={t('title')} subtitle={t('subtitle')} />

      <Card className="mb-4 grid grid-cols-2 gap-x-6 gap-y-4 p-4 md:grid-cols-4 md:p-6">
        <Stat
          label={t('tiles.dead_deliveries')}
          value={deliveries.data?.deadLettered}
          loading={deliveries.isLoading}
          alert
        />
        <Stat
          label={t('tiles.retrying_deliveries')}
          value={deliveries.data?.retrying}
          loading={deliveries.isLoading}
        />
        <Stat
          label={t('tiles.dead_events')}
          value={outbox.data ? deadEvents : undefined}
          loading={outbox.isLoading}
          alert
        />
        <Stat
          label={t('tiles.retrying_events')}
          value={outbox.data ? retryingEvents : undefined}
          loading={outbox.isLoading}
        />
      </Card>

      <DeliveryDeadLetters />

      {outbox.data && modulesWithDead.length === 0 ? (
        <DeadLetterSection title={t('events.title')} count={0}>
          <NothingWaiting />
        </DeadLetterSection>
      ) : (
        modulesWithDead.map((m) => <OutboxDeadLetters key={m.module} module={m.module} />)
      )}
    </PageContainer>
  );
}

/** A neutral label/value pair; the value turns red only when it is above zero and `alert` is set. */
function Stat({
  label,
  value,
  loading,
  alert = false,
}: {
  label: string;
  value: number | undefined;
  loading: boolean;
  alert?: boolean;
}) {
  return (
    <div>
      <div className="text-muted-foreground text-label">{label}</div>
      {loading ? (
        <Skeleton className="mt-1 h-7 w-10" />
      ) : (
        <div
          className={cn(
            'numeral mt-0.5 text-2xl font-semibold',
            alert && value !== undefined && value > 0 && 'text-over',
          )}
        >
          {value ?? '—'}
        </div>
      )}
    </div>
  );
}
