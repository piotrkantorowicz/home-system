import { Banner, Button } from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { useTranslation } from 'react-i18next';

import { useHouseholdMutation, useMyInvitations } from '../api/queries';

export function MyInvitations() {
  const { t } = useTranslation('household');
  const toast = useToast();
  const query = useMyInvitations(true);
  const mutation = useHouseholdMutation();

  // Only a confirmed empty result hides the panel — a slow or failed request must stay visible,
  // or an invited user could create their own household while unable to see the one they were
  // actually invited to (membership is one household per person, so that would lock them out).
  if (query.isSuccess && query.data.length === 0) return null;

  return (
    <section className="bg-card border-border max-w-xl space-y-4 rounded-xl border p-6 shadow-sm">
      <div>
        <h2 className="text-xl font-semibold">{t('my_invitations_title')}</h2>
        <p className="text-text-2 mt-2 text-sm">{t('my_invitations_description')}</p>
      </div>
      {query.isPending && <p role="status">{t('loading')}</p>}
      {query.isError && (
        <Banner
          variant="error"
          onRetry={() => {
            void query.refetch();
          }}
          retryLabel={t('retry')}
        >
          {t('invitations_error')}
        </Banner>
      )}
      {mutation.isError && <Banner variant="error">{t('save_error')}</Banner>}
      <ul className="divide-border divide-y">
        {query.data?.map((invitation) => (
          <li
            key={invitation.id}
            className="flex flex-wrap items-center justify-between gap-3 py-3"
          >
            <p className="break-words">
              {t('my_invitation_from', {
                household: invitation.householdName,
                role: t(`roles.${invitation.role}`),
              })}
            </p>
            <div className="flex gap-2">
              <Button
                variant="ghost"
                size="sm"
                disabled={mutation.isPending}
                onClick={() => {
                  mutation.mutate(
                    { kind: 'decline-invitation', invitationId: invitation.id },
                    {
                      onSuccess: () => {
                        toast.success(t('invitation_declined'));
                      },
                    },
                  );
                }}
              >
                {t('decline')}
              </Button>
              <Button
                size="sm"
                disabled={mutation.isPending}
                onClick={() => {
                  mutation.mutate(
                    { kind: 'accept-invitation', invitationId: invitation.id },
                    {
                      onSuccess: () => {
                        toast.success(t('joined', { name: invitation.householdName }));
                      },
                    },
                  );
                }}
              >
                {t('accept')}
              </Button>
            </div>
          </li>
        ))}
      </ul>
    </section>
  );
}
