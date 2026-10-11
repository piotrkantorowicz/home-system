import { useHousehold } from '@modules/household';
import { Banner, Button, EmptyState, PageContainer, PageHeader } from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { Plus, Wallet as WalletIcon } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { errorStatus } from '../api/client';
import { useWalletMutation, useWalletsQuery } from '../api/queries';
import { WalletFormDialog } from '../components/WalletFormDialog';
import { WalletRow } from '../components/WalletRow';
import { WALLET_KINDS } from '../types';

import type { Wallet } from '../types';

export default function InvestmentsPage() {
  const { t } = useTranslation('investments');
  const toast = useToast();
  const { members, myPersonId } = useHousehold();
  const query = useWalletsQuery();
  const mutation = useWalletMutation();
  const [editing, setEditing] = useState<Wallet | 'new' | null>(null);

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
      <PageHeader title={t('wallets_title')} subtitle={t('wallets_subtitle')} actions={addButton} />
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
        {query.isSuccess && active.length === 0 && (
          <EmptyState
            icon={WalletIcon}
            title={t('no_wallets_title')}
            description={t('no_wallets_body')}
          />
        )}
        {groups.map(({ kind, rows }) => (
          <section key={kind} aria-labelledby={`kind-${kind}`} className="space-y-3">
            <h2 id={`kind-${kind}`} className="text-section font-semibold">
              {t(`kind_${kind}`)} · {rows.length}
            </h2>
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
