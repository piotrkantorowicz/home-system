import { useHousehold } from '@modules/household';

export type BudgetAccessLevel = 'adult' | 'child' | 'none';

/**
 * What the caller's household role allows in Budget. Cosmetic: the API enforces access, and an
 * unknown role gets `none`. Guests have no Budget entry at all.
 */
export function useBudgetAccess(): { level: BudgetAccessLevel; role: string | null } {
  const { myRole } = useHousehold();
  const level: BudgetAccessLevel =
    myRole === 'Owner' || myRole === 'Adult' ? 'adult' : myRole === 'Child' ? 'child' : 'none';
  return { level, role: myRole };
}
