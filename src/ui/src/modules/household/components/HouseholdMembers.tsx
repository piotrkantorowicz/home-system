import { Banner, Button, Select } from '@shared/components/ui';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { useHouseholdMutation } from '../api/queries';
import { HOUSEHOLD_ROLES } from '../types';

import { ConfirmHouseholdAction } from './ConfirmHouseholdAction';

import type { Household, HouseholdMember, HouseholdRole } from '../types';

export function HouseholdMembers({ household }: { household: Household }) {
  const { t } = useTranslation('household');
  const mutation = useHouseholdMutation();
  const [removing, setRemoving] = useState<HouseholdMember | null>(null);
  const owner = household.myRole === 'Owner';
  const ownerCount = household.members.filter((member) => member.role === 'Owner').length;
  return (
    <>
      {mutation.isError && <Banner variant="error">{t('save_error')}</Banner>}
      <ul className="divide-border divide-y">
        {household.members.map((member) => {
          const lastOwner = member.role === 'Owner' && ownerCount === 1;
          return (
            <li key={member.personId} className="flex flex-wrap items-center gap-3 py-4">
              <div className="bg-accent text-accent-foreground flex size-11 shrink-0 items-center justify-center overflow-hidden rounded-full font-semibold">
                {member.avatarUrl ? (
                  <img src={member.avatarUrl} alt="" className="size-full object-cover" />
                ) : (
                  member.displayName.slice(0, 2).toLocaleUpperCase()
                )}
              </div>
              <div className="min-w-0 flex-1">
                <p className="font-semibold break-words">{member.nickname ?? member.displayName}</p>
                <p className="text-muted-foreground text-sm">
                  {t(member.isManaged ? 'managed_label' : 'account_label')}
                </p>
              </div>
              <div className="flex w-full items-center gap-2 sm:w-auto">
                {owner ? (
                  <Select
                    aria-label={t('role_for', { name: member.displayName })}
                    value={member.role}
                    disabled={lastOwner || mutation.isPending}
                    onChange={(event) => {
                      mutation.mutate({
                        kind: 'role',
                        id: household.id,
                        personId: member.personId,
                        role: event.target.value as HouseholdRole,
                      });
                    }}
                  >
                    {HOUSEHOLD_ROLES.map((role) => (
                      <option key={role} value={role}>
                        {t(`roles.${role}`)}
                      </option>
                    ))}
                  </Select>
                ) : (
                  <span className="bg-secondary rounded-md px-3 py-1 text-sm">
                    {t(`roles.${member.role}`)}
                  </span>
                )}
                {owner && (
                  <Button
                    variant="ghost"
                    size="sm"
                    disabled={lastOwner || mutation.isPending}
                    aria-label={t('remove_person', { name: member.displayName })}
                    onClick={() => {
                      setRemoving(member);
                    }}
                  >
                    {t('remove')}
                  </Button>
                )}
              </div>
            </li>
          );
        })}
      </ul>
      {owner && ownerCount === 1 && (
        <p className="text-muted-foreground text-sm">{t('last_owner')}</p>
      )}
      {removing && (
        <ConfirmHouseholdAction
          title={t('remove_person', { name: removing.displayName })}
          description={t('remove_warning')}
          action={{ kind: 'remove', id: household.id, personId: removing.personId }}
          onClose={() => {
            setRemoving(null);
          }}
        />
      )}
    </>
  );
}
