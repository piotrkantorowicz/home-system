import { ModuleLabelsContext } from '@shared/context/ModuleLabelsContext';
import { NavigationAccessContext } from '@shared/context/NavigationAccessContext';
import { useToast } from '@shared/context/ToastContext';
import { useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';

import { useHouseholdQuery } from '../api/queries';
import { HouseholdContext } from '../hooks/useHousehold';

import type { ReactNode } from 'react';

export function HouseholdProvider({ children }: { children: ReactNode }) {
  const query = useHouseholdQuery();
  const { t } = useTranslation('household');
  const toast = useToast();
  const announced = useRef<string | null>(null);
  const household = query.data?.household ?? null;
  useEffect(() => {
    if (query.data?.joined && household && announced.current !== household.id) {
      announced.current = household.id;
      toast.success(t('joined', { name: household.name }));
    }
  }, [query.data?.joined, household, toast, t]);
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
