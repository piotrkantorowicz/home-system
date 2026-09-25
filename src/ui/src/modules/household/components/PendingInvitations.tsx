import { Banner, Button } from '@shared/components/ui';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { useInvitations } from '../api/queries';

import { ConfirmHouseholdAction } from './ConfirmHouseholdAction';

export function PendingInvitations({ id, owner }: { id: string; owner: boolean }) {
  const { t, i18n } = useTranslation('household');
  const query = useInvitations(id, true);
  const [revoke, setRevoke] = useState<string | null>(null);
  return (
    <section className="border-border border-t pt-6">
      <h2 className="text-lg font-semibold">{t('invitations')}</h2>
      <p className="text-text-2 mt-1 text-sm">{t('invite_hint')}</p>
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
      {query.isSuccess && query.data.length === 0 && (
        <p className="text-muted-foreground mt-4 text-sm">{t('no_invitations')}</p>
      )}
      <ul className="divide-border mt-2 divide-y">
        {query.data?.map((invitation) => {
          const label = invitation.email ?? invitation.targetDisplayName ?? '';
          return (
            <li
              key={invitation.id}
              className="flex flex-wrap items-center justify-between gap-3 py-3"
            >
              <div className="min-w-0">
                <p className="break-words">{label}</p>
                <p className="text-muted-foreground text-sm">
                  {t(`roles.${invitation.role}`)} ·{' '}
                  {t(new Date(invitation.expiresAt) <= new Date() ? 'expired' : 'expires', {
                    date: new Date(invitation.expiresAt).toLocaleDateString(i18n.language),
                  })}
                </p>
              </div>
              {owner && (
                <Button
                  variant="ghost"
                  size="sm"
                  aria-label={t('revoke_email', { email: label })}
                  onClick={() => {
                    setRevoke(invitation.id);
                  }}
                >
                  {t('revoke')}
                </Button>
              )}
            </li>
          );
        })}
      </ul>
      {revoke && (
        <ConfirmHouseholdAction
          title={t('revoke')}
          description={t('revoke_warning')}
          action={{ kind: 'revoke', id, invitationId: revoke }}
          onClose={() => {
            setRevoke(null);
          }}
        />
      )}
    </section>
  );
}
