import { Banner, Button, Card, PageContainer, PageHeader } from '@shared/components/ui';
import { House, UserPlus } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import { AddMemberDialog } from '../components/AddMemberDialog';
import { ConfirmHouseholdAction } from '../components/ConfirmHouseholdAction';
import { HouseholdMembers } from '../components/HouseholdMembers';
import { HouseholdNameForm } from '../components/HouseholdNameForm';
import { MyInvitations } from '../components/MyInvitations';
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
    <PageContainer width="form">
      <PageHeader
        title={t('household_nav')}
        subtitle={
          household
            ? t('subtitle_household', { name: household.name, count: household.members.length })
            : t('subtitle')
        }
        actions={
          household && owner ? (
            <Button
              size="xl"
              onClick={() => {
                setAdding(true);
              }}
            >
              <UserPlus className="size-4" />
              {t('add_person')}
            </Button>
          ) : undefined
        }
      />
      {isLoading ? (
        <p role="status">{t('loading')}</p>
      ) : isError ? (
        <Banner variant="error" onRetry={refetch} retryLabel={t('retry')}>
          {t('load_error')}
        </Banner>
      ) : !household ? (
        <div className="space-y-6">
          <MyInvitations />
          <Card className="max-w-xl space-y-5 p-6">
            <House className="text-primary size-10" aria-hidden="true" />
            <div>
              <h2 className="text-xl font-semibold">{t('setup_title')}</h2>
              <p className="text-text-2 mt-2 text-sm">{t('setup_description')}</p>
            </div>
            <HouseholdNameForm />
          </Card>
        </div>
      ) : (
        <div className="space-y-4">
          {owner && household.members.length === 1 && (
            <section className="bg-accent rounded-lg p-6">
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
                <Button asChild variant="outline">
                  <Link to="/diet-planner">{t('continue_diet_planner')}</Link>
                </Button>
              </div>
            </section>
          )}
          <Card className="p-5 md:p-6">
            <h2 className="mb-1 text-lg font-semibold">{t('people')}</h2>
            <HouseholdMembers household={household} />
          </Card>
          <PendingInvitations id={household.id} owner={owner} />
          {owner && (
            <Card className="p-5 md:p-6">
              <h2 className="mb-3 text-lg font-semibold">{t('name')}</h2>
              <HouseholdNameForm key={household.id} household={household} />
            </Card>
          )}
          <Card className="border-over/40 divide-border divide-y p-0">
            <h2 className="sr-only">{t('membership')}</h2>
            <div className="flex flex-wrap items-center justify-between gap-3 p-5 md:px-6">
              <div className="min-w-0">
                <p className="font-semibold">{t('leave')}</p>
                <p className="text-text-2 text-sm">
                  {lastOwner ? t('last_owner') : t('leave_hint')}
                </p>
              </div>
              <Button
                variant="outline"
                disabled={lastOwner}
                onClick={() => {
                  setConfirm('leave');
                }}
              >
                {t('leave')}
              </Button>
            </div>
            {owner && (
              <div className="flex flex-wrap items-center justify-between gap-3 p-5 md:px-6">
                <div className="min-w-0">
                  <p className="font-semibold">{t('delete')}</p>
                  <p className="text-text-2 text-sm">{t('delete_hint')}</p>
                </div>
                <Button
                  variant="outline"
                  className="border-over/50 text-over hover:text-over"
                  onClick={() => {
                    setConfirm('delete');
                  }}
                >
                  {t('delete')}
                </Button>
              </div>
            )}
          </Card>
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
    </PageContainer>
  );
}
