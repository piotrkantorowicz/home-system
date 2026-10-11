import { useHousehold } from '@modules/household';
import { Banner, Button, Card, EmptyState, PageContainer, PageHeader } from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { Plus, Wallet as WalletIcon } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router-dom';

import { errorStatus } from '../api/client';
import {
  useNetWorthHistoryQuery,
  useNetWorthQuery,
  useWalletMutation,
  useWalletsQuery,
} from '../api/queries';
import { KindBreakdown } from '../components/KindBreakdown';
import { NetWorthChart } from '../components/NetWorthChart';
import { NetWorthSummary } from '../components/NetWorthSummary';
import { WalletFormDialog } from '../components/WalletFormDialog';
import { WalletRow } from '../components/WalletRow';
import { NET_WORTH_SCOPES, WALLET_KINDS } from '../types';

import type { NetWorthScope, Wallet } from '../types';

export default function InvestmentsPage() {
  const { t } = useTranslation('investments');
  const toast = useToast();
  const { members, myPersonId } = useHousehold();
  const query = useWalletsQuery();
  const mutation = useWalletMutation();
  const [editing, setEditing] = useState<Wallet | 'new' | null>(null);
  const [params, setParams] = useSearchParams();
  const scope: NetWorthScope = params.get('scope') === 'mine' ? 'mine' : 'household';
  const netWorth = useNetWorthQuery(scope);
  const history = useNetWorthHistoryQuery(scope);

  const wallets = query.data ?? [];
  const active = wallets.filter((w) => !w.isArchived);
  const archived = wallets.filter((w) => w.isArchived);
  const groups = WALLET_KINDS.map((kind) => ({
    kind,
    rows: active.filter((w) => w.kind === kind),
  })).filter((g) => g.rows.length > 0);

  const ownerLabel = (wallet: Wallet) =>
    wallet.ownerPersonId === myPersonId
      ? null
      : t('owned_by', {
          name:
            members.find((m) => m.personId === wallet.ownerPersonId)?.displayName ??
            wallet.ownerName,
        });

  const toggleArchive = (wallet: Wallet) => {
    mutation.mutate(
      { kind: wallet.isArchived ? 'restore' : 'archive', id: wallet.id },
      {
        onSuccess: () => {
          toast.success(t(wallet.isArchived ? 'wallet_restored' : 'wallet_archived'));
        },
      },
    );
  };

  const renderRow = (wallet: Wallet) => (
    <WalletRow
      key={wallet.id}
      wallet={wallet}
      ownerLabel={ownerLabel(wallet)}
      busy={mutation.isPending}
      onEdit={() => {
        setEditing(wallet);
      }}
      onToggleArchive={() => {
        toggleArchive(wallet);
      }}
    />
  );

  const addButton = (
    <Button
      className="gap-2"
      onClick={() => {
        setEditing('new');
      }}
    >
      <Plus className="size-4" />
      {t('add_wallet')}
    </Button>
  );

  return (
    <PageContainer width="narrow">
      <PageHeader title={t('net_worth')} actions={addButton} />
      <div className="space-y-8">
        <div role="group" aria-label={t('scope')} className="flex gap-2">
          {NET_WORTH_SCOPES.map((value) => (
            <Button
              key={value}
              size="sm"
              variant={scope === value ? 'default' : 'outline'}
              aria-pressed={scope === value}
              onClick={() => {
                setParams(value === 'household' ? {} : { scope: value }, { replace: true });
              }}
            >
              {t(`scope_${value}`)}
            </Button>
          ))}
        </div>
        {netWorth.isError && (
          <Banner
            variant="error"
            onRetry={() => {
              void netWorth.refetch();
            }}
            retryLabel={t('retry')}
          >
            {t('net_worth_load_error')}
          </Banner>
        )}
        {netWorth.data && (
          <Card className="space-y-6 p-5">
            <NetWorthSummary netWorth={netWorth.data} history={history.data} />
            {history.data && history.data.length > 1 && <NetWorthChart points={history.data} />}
          </Card>
        )}
        {netWorth.data && (
          <KindBreakdown byKind={netWorth.data.byKind} total={netWorth.data.total} />
        )}
        <h2 className="text-section font-semibold">{t('wallets_title')}</h2>
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
        {query.isSuccess && active.length === 0 && (
          <EmptyState
            icon={WalletIcon}
            title={t('no_wallets_title')}
            description={t('no_wallets_body')}
          />
        )}
        {groups.map(({ kind, rows }) => (
          <section key={kind} aria-labelledby={`kind-${kind}`} className="space-y-3">
            <h3 id={`kind-${kind}`} className="font-semibold">
              {t(`kind_${kind}`)} · {rows.length}
            </h3>
            <ul className="space-y-2">{rows.map(renderRow)}</ul>
          </section>
        ))}
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
        <WalletFormDialog
          {...(editing === 'new' ? {} : { wallet: editing })}
          onClose={() => {
            setEditing(null);
          }}
        />
      )}
    </PageContainer>
  );
}
