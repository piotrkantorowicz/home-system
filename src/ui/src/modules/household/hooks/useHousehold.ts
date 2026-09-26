import { createContext, use } from 'react';

import type { Household, HouseholdMember } from '../types';

export interface HouseholdContextValue {
  household: Household | null;
  myRole: string | null;
  /** The caller's `Person` id — known once signed in, even without a household or diet profile. */
  myPersonId: string | null;
  members: HouseholdMember[];
  isLoading: boolean;
  isError: boolean;
  refetch: () => void;
}

export const HouseholdContext = createContext<HouseholdContextValue | null>(null);

export function useHousehold() {
  const context = use(HouseholdContext);
  if (!context) throw new Error('useHousehold must be used within HouseholdProvider');
  return context;
}
