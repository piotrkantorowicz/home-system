import {
  Banner,
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  Select,
} from '@shared/components/ui';
import { Ellipsis } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { useHouseholdMutation } from '../api/queries';
import { useHousehold } from '../hooks/useHousehold';
import { HOUSEHOLD_ROLES } from '../types';
import { initials } from '../utils/initials';

import { ConfirmHouseholdAction } from './ConfirmHouseholdAction';

import type { Household, HouseholdMember, HouseholdRole } from '../types';

export function HouseholdMembers({ household }: { household: Household }) {
  const { t } = useTranslation('household');
  const { myPersonId } = useHousehold();
  const mutation = useHouseholdMutation();
  const [removing, setRemoving] = useState<HouseholdMember | null>(null);
  const owner = household.myRole === 'Owner';
  const ownerCount = household.members.filter((member) => member.role === 'Owner').length;
  return (
    <>
      {mutation.isError && <Banner variant="error">{t('save_error')}</Banner>}
      <ul className="divide-border divide-y">
        {household.members.map((member) => {
          // The last owner cannot be demoted or removed; the reason is stated once, beside Leave.
          const lastOwner = member.role === 'Owner' && ownerCount === 1;
          const you = member.personId === myPersonId;
          return (
            <li
              key={member.personId}
              className="flex min-h-[68px] flex-wrap items-center gap-3 py-3"
            >
              <div
                aria-hidden="true"
                className="bg-accent text-accent-foreground flex size-10 shrink-0 items-center justify-center overflow-hidden rounded-full text-sm font-semibold"
              >
                {member.avatarUrl ? (
                  <img src={member.avatarUrl} alt="" className="size-full object-cover" />
                ) : (
                  initials(member.nickname ?? member.displayName)
                )}
              </div>
              <div className="min-w-0 flex-1">
                <p className="font-semibold break-words">
                  {member.nickname ?? member.displayName}
                  {you ? <span className="text-text-2 font-normal"> {t('you')}</span> : null}
                </p>
                <p className="text-muted-foreground text-sm">
                  {t(member.isManaged ? 'managed_label' : 'account_label')}
                </p>
              </div>
              <div className="flex items-center gap-2">
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
                  <span className="text-text-2 px-3 py-1 text-sm">{t(`roles.${member.role}`)}</span>
                )}
                {owner && (
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button
                        variant="ghost"
                        size="icon"
                        disabled={lastOwner || mutation.isPending}
                        aria-label={t('person_actions', { name: member.displayName })}
                      >
                        <Ellipsis className="size-4" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                      <DropdownMenuItem
                        className="text-destructive focus:text-destructive"
                        aria-label={t('remove_person', { name: member.displayName })}
                        onSelect={() => {
                          setRemoving(member);
                        }}
                      >
                        {t('remove')}
                      </DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
                )}
              </div>
            </li>
          );
        })}
      </ul>
      <p className="text-muted-foreground border-border border-t pt-3 text-sm">{t('role_hint')}</p>
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
