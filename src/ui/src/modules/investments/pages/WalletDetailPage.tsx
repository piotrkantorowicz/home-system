import { useHousehold } from '@modules/household';
import { Banner, Button, Card, EmptyState, PageContainer, PageHeader } from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { useFormat } from '@shared/hooks/useFormat';
import { Wallet as WalletIcon } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';

import { errorStatus } from '../api/client';
import { useWalletMutation, useWalletQuery } from '../api/queries';
import { RecordBalanceForm } from '../components/RecordBalanceForm';
import { SnapshotHistory } from '../components/SnapshotHistory';
import { WalletFormDialog } from '../components/WalletFormDialog';
import { formatDate } from '../lib/dates';

export default function WalletDetailPage() {
  const { t, i18n } = useTranslation('investments');
  const { id = '' } = useParams();
  const { money } = useFormat();
  const toast = useToast();
  const { members, myPersonId } = useHousehold();
  const query = useWalletQuery(id);
  const mutation = useWalletMutation();
  const [editing, setEditing] = useState(false);
  const breadcrumb = [{ label: t('wallets_title'), href: '/investments' }];

  if (query.isPending)
    return (
      <PageContainer width="narrow">
        <p role="status">{t('loading')}</p>
      </PageContainer>
    );

  if (query.isError)
    return (
      <PageContainer width="narrow">
        <PageHeader title={t('wallet')} breadcrumb={breadcrumb} />
        {errorStatus(query.error) === 404 ? (
          <EmptyState
            icon={WalletIcon}
            title={t('wallet_not_found_title')}
            description={t('wallet_not_found_body')}
          />
        ) : (
          <Banner
            variant="error"
            onRetry={() => {
              void query.refetch();
            }}
            retryLabel={t('retry')}
          >
            {t('wallet_load_error')}
          </Banner>
        )}
      </PageContainer>
    );

  const wallet = query.data;
  const owner =
    wallet.ownerPersonId === myPersonId
      ? t('you')
      : (members.find((m) => m.personId === wallet.ownerPersonId)?.displayName ?? wallet.ownerName);
  const currency = { currency: wallet.currency };

  return (
    <PageContainer width="narrow">
      <PageHeader
        title={wallet.name}
        subtitle={[
          t(`kind_${wallet.kind}`),
          t(`institution_${wallet.institution}`),
          t('owned_by', { name: owner }),
          wallet.visibility === 'Private' ? t('private') : null,
          wallet.isArchived ? t('archived') : null,
        ]
          .filter(Boolean)
          .join(' · ')}
        breadcrumb={[...breadcrumb, { label: wallet.name }]}
        actions={
          <>
            <Button
              variant="outline"
              onClick={() => {
                setEditing(true);
              }}
            >
              {t('edit')}
            </Button>
            <Button
              variant="outline"
              disabled={mutation.isPending}
              onClick={() => {
                mutation.mutate(
                  { kind: wallet.isArchived ? 'restore' : 'archive', id: wallet.id },
                  {
                    onSuccess: () => {
                      toast.success(t(wallet.isArchived ? 'wallet_restored' : 'wallet_archived'));
                    },
                  },
                );
              }}
            >
              {wallet.isArchived ? t('restore') : t('archive')}
            </Button>
          </>
        }
      />
      <div className="space-y-8">
        <Card className="space-y-1 p-5">
          <p className="text-muted-foreground text-meta">{t('latest_balance')}</p>
          <p className="text-title tnum font-bold">
            {wallet.latestBalance === null ? '—' : money(wallet.latestBalance, currency)}
          </p>
          <p className="text-text-2 text-meta">
            {wallet.latestAsOf
              ? t('as_of', { date: formatDate(wallet.latestAsOf, i18n.language) })
              : t('no_balance_yet')}
          </p>
          {wallet.currency !== 'PLN' && wallet.latestBalancePln !== null && (
            <p className="text-body tnum">
              ≈ {money(wallet.latestBalancePln, { currency: 'PLN' })}
            </p>
          )}
          {wallet.rate && (
            <p className="text-muted-foreground text-meta">
              {t('rate_used', {
                currency: wallet.currency,
                mid: wallet.rate.mid,
                table: wallet.rate.tableNo,
                date: formatDate(wallet.rate.effectiveDate, i18n.language),
              })}
            </p>
          )}
          {wallet.rateMissing && (
            <p className="text-warning text-meta" role="status">
              {t('rate_missing')}
            </p>
          )}
        </Card>
        <RecordBalanceForm wallet={wallet} />
        <SnapshotHistory wallet={wallet} />
      </div>
      {editing && (
        <WalletFormDialog
          wallet={wallet}
          onClose={() => {
            setEditing(false);
          }}
        />
      )}
    </PageContainer>
  );
}
