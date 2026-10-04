import { useHousehold } from '@modules/household';
import { Banner, Button, EmptyState, PageContainer, PageHeader } from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { useQueryClient } from '@tanstack/react-query';
import { Plus, Wallet } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { errorStatus } from '../api/client';
import {
  useAccountsQuery,
  useBudgetMutation,
  useBudgetQuery,
  useSummaryQuery,
} from '../api/queries';
import { budgetQueryKeys } from '../api/queryKeys';
import { EnvelopeFormDialog } from '../components/EnvelopeFormDialog';
import { EnvelopeRow } from '../components/EnvelopeRow';
import { LimitDialog } from '../components/LimitDialog';
import { useBudgetAccess } from '../hooks/useBudgetAccess';
import { currentMonth } from '../lib/dates';

import type { Account } from '../types';
import type { ReactNode } from 'react';

export default function EnvelopesPage() {
  const { t, i18n } = useTranslation('budget');
  const client = useQueryClient();
  const budget = useBudgetQuery();
  const toast = useToast();
  const { members, myPersonId } = useHousehold();
  const query = useAccountsQuery();
  const mutation = useBudgetMutation();
  const [editing, setEditing] = useState<Account | 'new' | null>(null);
  const [limitFor, setLimitFor] = useState<string | null>(null);
  // Counts and limits are the server's, for the current month; nothing is recomputed here.
  const [month] = useState(currentMonth());
  const level = useBudgetAccess().level;
  const sharedSummary = useSummaryQuery(month, 'shared', null, level === 'adult');
  const personalSummary = useSummaryQuery(month, 'personal', null);
  const summaryEnvelopes = [
    ...(sharedSummary.data?.envelopes ?? []),
    ...(personalSummary.data?.envelopes ?? []),
  ];
  const summaryById = new Map(summaryEnvelopes.map((e) => [e.accountId, e]));
  const limitEnvelope = summaryById.get(limitFor ?? '');
  const currency =
    sharedSummary.data?.currency ?? personalSummary.data?.currency ?? budget.data?.currency ?? '';
  const monthName = new Intl.DateTimeFormat(i18n.language, {
    month: 'long',
    timeZone: 'UTC',
  }).format(new Date(`${month}-01T00:00:00Z`));

  const ownerLabel = (account: Account) => {
    if (account.visibility === 'Household') return t('audience_shared');
    const owner =
      account.ownerPersonId === myPersonId
        ? t('you')
        : (members.find((m) => m.personId === account.ownerPersonId)?.displayName ?? '');
    return t('personal_of', { name: owner });
  };

  const toggleArchive = (account: Account) => {
    mutation.mutate(
      {
        kind: account.isArchived ? 'restore' : 'archive',
        id: account.id,
        expectedRevision: Number(account.revision),
      },
      {
        onSuccess: () => {
          toast.success(t(account.isArchived ? 'restored' : 'archived_toast'));
        },
      },
    );
  };

  const accounts = query.data ?? [];
  const active = accounts.filter((a) => !a.isArchived);
  const archived = accounts.filter((a) => a.isArchived);
  const sharedRows = active.filter((a) => a.visibility === 'Household');
  const personalRows = active.filter((a) => a.visibility !== 'Household');
  const allMine = personalRows.every((a) => a.ownerPersonId === myPersonId);

  const renderRow = (account: Account) => {
    const entry = summaryById.get(account.id);
    return (
      <EnvelopeRow
        key={account.id}
        account={account}
        month={entry ? { expenseCount: Number(entry.expenseCount), limit: entry.limit } : null}
        monthName={monthName}
        ownerLabel={
          account.visibility !== 'Household' && account.ownerPersonId !== myPersonId
            ? ownerLabel(account)
            : null
        }
        busy={mutation.isPending}
        onRename={() => {
          setEditing(account);
        }}
        onLimit={() => {
          setLimitFor(account.id);
        }}
        onToggleArchive={() => {
          toggleArchive(account);
        }}
      />
    );
  };
  const section = (id: string, title: string, rows: Account[], note: ReactNode) => (
    <section aria-labelledby={id} className="space-y-3">
      <div>
        <h2 id={id} className="text-section font-semibold">
          {title} · {rows.length}
        </h2>
        <p className="text-muted-foreground text-meta">{note}</p>
      </div>
      <ul className="space-y-2">{rows.map(renderRow)}</ul>
    </section>
  );

  return (
    <PageContainer width="narrow">
      <PageHeader
        title={t('envelopes')}
        subtitle={t('envelopes_subtitle')}
        actions={
          <Button
            className="gap-2"
            onClick={() => {
              setEditing('new');
            }}
          >
            <Plus className="size-4" />
            {t('new_envelope')}
          </Button>
        }
      />
      <div className="space-y-8">
        {mutation.isError && (
          <Banner variant="error">
            {errorStatus(mutation.error) === 409 ? t('conflict_error') : t('save_error')}
          </Banner>
        )}
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
        {(sharedSummary.isError || personalSummary.isError) && (
          <Banner
            variant="error"
            onRetry={() => {
              void sharedSummary.refetch();
              void personalSummary.refetch();
            }}
            retryLabel={t('retry')}
          >
            {t('load_error')}
          </Banner>
        )}
        {query.isSuccess && active.length === 0 && (
          <EmptyState
            icon={Wallet}
            title={t('no_envelopes_title')}
            description={t('no_envelopes_body')}
          />
        )}
        {sharedRows.length > 0 &&
          section('shared-title', t('audience_shared'), sharedRows, t('shared_note'))}
        {personalRows.length > 0 &&
          section(
            'personal-title',
            t('audience_personal'),
            personalRows,
            allMine ? t('personal_note') : t('personal_note_mixed'),
          )}
        {archived.length > 0 && (
          <details className="border-border rounded-xl border p-4">
            <summary className="cursor-pointer font-semibold">
              {t('archived')} · {archived.length}
            </summary>
            <ul className="mt-3 space-y-2">{archived.map(renderRow)}</ul>
          </details>
        )}
      </div>

      {editing && (
        <EnvelopeFormDialog
          {...(editing === 'new' ? {} : { account: editing })}
          onClose={() => {
            setEditing(null);
          }}
        />
      )}
      {limitEnvelope && (
        <LimitDialog
          accountId={limitEnvelope.accountId}
          accountName={limitEnvelope.name}
          month={month}
          currency={currency}
          limit={
            limitEnvelope.limit === null || limitEnvelope.limitRevision === null
              ? null
              : { amount: limitEnvelope.limit, revision: Number(limitEnvelope.limitRevision) }
          }
          onClose={() => {
            setLimitFor(null);
          }}
          onReload={() => {
            setLimitFor(null);
            void client.invalidateQueries({ queryKey: budgetQueryKeys.all() });
          }}
        />
      )}
    </PageContainer>
  );
}
