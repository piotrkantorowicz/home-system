import { HouseholdContext } from '@modules/household/hooks/useHousehold';

import type { HouseholdMember } from '@modules/household';
import type { ReactNode } from 'react';

/** Provides a fixed household to components that call `useHousehold()`, without the OIDC-bound provider. */
export function HouseholdWrapper({
  myRole = 'Owner',
  myPersonId = 'person-1',
  members = [],
  children,
}: {
  myRole?: string;
  myPersonId?: string;
  members?: HouseholdMember[];
  children: ReactNode;
}) {
  return (
    <HouseholdContext
      value={{
        household: { id: 'home-1', name: 'Home', myRole, members },
        myRole,
        myPersonId,
        members,
        isLoading: false,
        isError: false,
        refetch: () => undefined,
      }}
    >
      {children}
    </HouseholdContext>
  );
}
