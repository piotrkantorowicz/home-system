import { Banner, Button } from '@shared/components/ui';
import { House, UserPlus } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { AddMemberDialog } from '../components/AddMemberDialog';
import { ConfirmHouseholdAction } from '../components/ConfirmHouseholdAction';
import { HouseholdMembers } from '../components/HouseholdMembers';
import { HouseholdNameForm } from '../components/HouseholdNameForm';
import { PendingInvitations } from '../components/PendingInvitations';
import { useHousehold } from '../hooks/useHousehold';

export default function HouseholdPage() {
  const { t } = useTranslation('household');
  const { household, isLoading, isError, refetch } = useHousehold();
  const [adding, setAdding] = useState(false);
  const [confirm, setConfirm] = useState<'leave' | 'delete' | null>(null);
  const owner = household?.myRole === 'Owner';
  const lastOwner =
    owner && household.members.filter((member) => member.role === 'Owner').length === 1;
  return (
    <main className="mx-auto w-full max-w-4xl px-4 py-6 md:px-8">
      <header className="mb-6 flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold break-words">
            {household?.name ?? t('household_nav')}
          </h1>
          <p className="text-text-2 mt-1 text-sm">{t('subtitle')}</p>
        </div>
        {household && owner && (
          <Button
            onClick={() => {
              setAdding(true);
            }}
            className="gap-2"
          >
            <UserPlus className="size-4" />
            {t('add_member')}
          </Button>
        )}
      </header>
      {isLoading ? (
        <p role="status">{t('loading')}</p>
      ) : isError ? (
        <Banner variant="error" onRetry={refetch} retryLabel={t('retry')}>
          {t('load_error')}
        </Banner>
      ) : !household ? (
        <section className="bg-card border-border max-w-xl space-y-5 rounded-xl border p-6 shadow-sm">
          <House className="text-primary size-10" aria-hidden="true" />
          <div>
            <h2 className="text-xl font-semibold">{t('setup_title')}</h2>
            <p className="text-text-2 mt-2 text-sm">{t('setup_description')}</p>
          </div>
          <HouseholdNameForm />
        </section>
      ) : (
        <div className="space-y-6">
          {owner && household.members.length === 1 && (
            <section className="bg-accent rounded-xl p-6">
              <h2 className="text-xl font-semibold">{t('ready_title')}</h2>
              <p className="text-text-2 mt-2 text-sm">{t('ready_description')}</p>
              <div className="mt-4 flex flex-wrap gap-3">
                <Button
                  onClick={() => {
                    setAdding(true);
                  }}
                >
                  {t('add_someone')}
                </Button>
              </div>
            </section>
          )}
          <section className="bg-card border-border rounded-xl border p-6 shadow-sm">
            <h2 className="text-lg font-semibold">
              {t('members', { count: household.members.length })}
            </h2>
            <HouseholdMembers household={household} />
          </section>
          <PendingInvitations id={household.id} owner={owner} />
          {owner && (
            <section className="border-border border-t pt-6">
              <h2 className="mb-4 text-lg font-semibold">{t('settings')}</h2>
              <div className="max-w-md">
                <HouseholdNameForm key={household.id} household={household} />
              </div>
            </section>
          )}
          <section className="border-border space-y-3 border-t pt-6">
            <h2 className="text-lg font-semibold">{t('membership')}</h2>
            {lastOwner && <p className="text-text-2 text-sm">{t('last_owner')}</p>}
            <div className="flex flex-wrap gap-3">
              <Button
                variant="outline"
                disabled={lastOwner}
                onClick={() => {
                  setConfirm('leave');
                }}
              >
                {t('leave')}
              </Button>
              {owner && (
                <Button
                  variant="ghost"
                  className="text-destructive"
                  onClick={() => {
                    setConfirm('delete');
                  }}
                >
                  {t('delete')}
                </Button>
              )}
            </div>
          </section>
        </div>
      )}
      {adding && household && owner && (
        <AddMemberDialog
          id={household.id}
          onClose={() => {
            setAdding(false);
          }}
        />
      )}
      {confirm && household && (
        <ConfirmHouseholdAction
          title={t(confirm)}
          description={t(`${confirm}_warning`)}
          action={{ kind: confirm, id: household.id }}
          onClose={() => {
            setConfirm(null);
          }}
        />
      )}
    </main>
  );
}
