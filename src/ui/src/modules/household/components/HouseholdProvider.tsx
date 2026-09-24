import { ModuleLabelsContext } from '@shared/context/ModuleLabelsContext';
import { NavigationAccessContext } from '@shared/context/NavigationAccessContext';
import { useTranslation } from 'react-i18next';

import { useHouseholdQuery } from '../api/queries';
import { HouseholdContext } from '../hooks/useHousehold';

import type { ReactNode } from 'react';

export function HouseholdProvider({ children }: { children: ReactNode }) {
  const query = useHouseholdQuery();
  const { t } = useTranslation('household');
  const household = query.data?.household ?? null;
  return (
    <HouseholdContext
      value={{
        household,
        myRole: household?.myRole ?? null,
        members: household?.members ?? [],
        isLoading: query.isPending,
        isError: query.isError,
        refetch: () => {
          void query.refetch();
        },
      }}
    >
      <ModuleLabelsContext value={household ? { household: household.name } : {}}>
        <NavigationAccessContext
          value={
            household
              ? null
              : {
                  allowedPath: '/household',
                  reason: t(
                    query.isPending ? 'loading' : query.isError ? 'load_error' : 'setup_required',
                  ),
                }
          }
        >
          {children}
        </NavigationAccessContext>
      </ModuleLabelsContext>
    </HouseholdContext>
  );
}
