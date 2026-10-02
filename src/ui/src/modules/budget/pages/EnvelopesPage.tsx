import { useHousehold } from '@modules/household';
import { Badge, Banner, Button, EmptyState } from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { Plus, Wallet } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { errorStatus } from '../api/client';
import { useAccountsQuery, useBudgetMutation } from '../api/queries';
import { EnvelopeFormDialog } from '../components/EnvelopeFormDialog';

import type { Account } from '../types';

export default function EnvelopesPage() {
  const { t } = useTranslation('budget');
  const toast = useToast();
  const { members, myPersonId } = useHousehold();
  const query = useAccountsQuery();
  const mutation = useBudgetMutation();
  const [editing, setEditing] = useState<Account | 'new' | null>(null);

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

  const renderRow = (account: Account) => (
    <li
      key={account.id}
      className="border-border flex flex-wrap items-center justify-between gap-3 rounded-xl border p-4"
    >
      <div className="min-w-0">
        <p className="font-semibold break-words">{account.name}</p>
        <Badge variant="outline" className="mt-1">
          {ownerLabel(account)}
        </Badge>
      </div>
      <div className="flex gap-2">
        {!account.isArchived && (
          <Button
            variant="outline"
            size="sm"
            onClick={() => {
              setEditing(account);
            }}
            aria-label={t('rename_named', { name: account.name })}
          >
            {t('rename')}
          </Button>
        )}
        <Button
          variant="ghost"
          size="sm"
          disabled={mutation.isPending}
          onClick={() => {
            toggleArchive(account);
          }}
          aria-label={t(account.isArchived ? 'restore_named' : 'archive_named', {
            name: account.name,
          })}
        >
          {t(account.isArchived ? 'restore' : 'archive')}
        </Button>
      </div>
    </li>
  );

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div className="max-w-xl">
          <h2 className="text-xl font-semibold">{t('envelopes')}</h2>
          <p className="text-text-2 mt-1 text-sm">{t('envelopes_intro')}</p>
        </div>
        <Button
          className="gap-2"
          onClick={() => {
            setEditing('new');
          }}
        >
          <Plus className="size-4" />
          {t('new_envelope')}
        </Button>
      </header>

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
          icon={Wallet}
          title={t('no_envelopes_title')}
          description={t('no_envelopes_body')}
        />
      )}
      {active.length > 0 && <ul className="space-y-3">{active.map(renderRow)}</ul>}
      {archived.length > 0 && (
        <section aria-labelledby="archived-title" className="space-y-3">
          <h3 id="archived-title" className="text-text-2 text-sm font-semibold">
            {t('archived')}
          </h3>
          <ul className="space-y-3">{archived.map(renderRow)}</ul>
        </section>
      )}

      {editing && (
        <EnvelopeFormDialog
          {...(editing === 'new' ? {} : { account: editing })}
          onClose={() => {
            setEditing(null);
          }}
        />
      )}
    </div>
  );
}
