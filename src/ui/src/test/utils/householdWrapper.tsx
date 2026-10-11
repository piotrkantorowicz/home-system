import { HouseholdContext } from '@modules/household/hooks/useHousehold';

import type { HouseholdMember } from '@modules/household';
import type { ReactNode } from 'react';

/** Provides a fixed household to components that call `useHousehold()`, without the OIDC-bound provider. */
export function HouseholdWrapper({
  myRole = 'Owner',
  myPersonId = 'person-1',
  members = [],
  isLoading = false,
  children,
}: {
  myRole?: string;
  myPersonId?: string;
  members?: HouseholdMember[];
  isLoading?: boolean;
  children: ReactNode;
}) {
  return (
    <HouseholdContext
      value={{
        household: isLoading ? null : { id: 'home-1', name: 'Home', myRole, members },
        myRole: isLoading ? null : myRole,
        myPersonId: isLoading ? null : myPersonId,
        members,
        isLoading,
        isError: false,
        refetch: () => undefined,
      }}
    >
      {children}
    </HouseholdContext>
  );
}
