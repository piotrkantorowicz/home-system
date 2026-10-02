import { createContext, use } from 'react';

/** The signed-in user's household role (`Owner`, `Adult`, `Child`, `Guest`), or `null` without a household. */
export const HouseholdRoleContext = createContext<string | null>(null);

export function useHouseholdRole() {
  return use(HouseholdRoleContext);
}
