import { createContext, use } from 'react';

export interface FakeHouseholdState {
  household: { id: string } | null;
  myRole: string | null;
  myPersonId: string | null;
  members: { personId: string; displayName: string; role: string; isManaged: boolean }[];
}

/** Reactive stand-in for `useHousehold()`: change the provided value and consumers re-render, like the real context. */
export const HouseholdStateContext = createContext<FakeHouseholdState | null>(null);

export function useFakeHousehold(): FakeHouseholdState {
  const state = use(HouseholdStateContext);
  if (!state) throw new Error('Wrap in HouseholdStateContext');
  return state;
}
