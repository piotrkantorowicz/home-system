import { useUserRoles } from '@shared/auth/useUserRoles';
import { EmptyState, MetricTile } from '@shared/components/ui';
import { useQuery } from '@tanstack/react-query';
import { ShieldAlert } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { deliveryBacklogOptions } from '../api/hooks/useDeliveryDeadLetters';
import { outboxBacklogOptions } from '../api/hooks/useOutboxDeadLetters';
import { DeliveryDeadLetters } from '../components/DeliveryDeadLetters';
import { OutboxDeadLetters } from '../components/OutboxDeadLetters';
import { ADMIN_ROLE } from '../constants';

import type { ReactNode } from 'react';

export default function DeadLetters() {
  const { t } = useTranslation('admin');
  const isAdmin = useUserRoles().includes(ADMIN_ROLE);

  if (!isAdmin) {
    return (
      <main className="mx-auto w-full max-w-5xl px-4 py-6 md:px-8">
        <EmptyState
          icon={ShieldAlert}
          title={t('forbidden_title')}
          description={t('forbidden_body')}
        />
      </main>
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
    <main className="mx-auto w-full max-w-5xl px-4 py-6 md:px-8">
      <header className="mb-6">
        <h1 className="text-26px font-bold tracking-tight">{t('title')}</h1>
        <p className="text-muted-foreground mt-0.5 text-sm">{t('subtitle')}</p>
      </header>

      <div className="mb-8 grid grid-cols-2 gap-3 md:grid-cols-4">
        <MetricTile
          label={t('tiles.dead_deliveries')}
          value={deliveries.data?.deadLettered ?? '—'}
          accent={deliveries.data?.deadLettered ? 'fat' : 'default'}
          loading={deliveries.isLoading}
        />
        <MetricTile
          label={t('tiles.retrying_deliveries')}
          value={deliveries.data?.retrying ?? '—'}
          loading={deliveries.isLoading}
        />
        <MetricTile
          label={t('tiles.dead_events')}
          value={outbox.data ? deadEvents : '—'}
          accent={deadEvents ? 'fat' : 'default'}
          loading={outbox.isLoading}
        />
        <MetricTile
          label={t('tiles.retrying_events')}
          value={outbox.data ? retryingEvents : '—'}
          loading={outbox.isLoading}
        />
      </div>

      <Section title={t('deliveries.title')} description={t('deliveries.description')}>
        <DeliveryDeadLetters />
      </Section>

      <Section title={t('events.title')} description={t('events.description')}>
        {outbox.data && modulesWithDead.length === 0 ? (
          <p className="text-muted-foreground text-sm">{t('events.empty')}</p>
        ) : (
          modulesWithDead.map((m) => (
            <div key={m.module} className="mb-6">
              <h3 className="mb-2 text-sm font-semibold">
                {t('events.module_heading', { module: m.module, count: m.deadLettered })}
              </h3>
              <OutboxDeadLetters module={m.module} />
            </div>
          ))
        )}
      </Section>
    </main>
  );
}

function Section({
  title,
  description,
  children,
}: {
  title: string;
  description: string;
  children: ReactNode;
}) {
  return (
    <section className="bg-card rounded-22px mb-6 border p-4 md:p-6">
      <h2 className="text-lg font-semibold">{title}</h2>
      <p className="text-muted-foreground mb-4 text-sm">{description}</p>
      {children}
    </section>
  );
}
