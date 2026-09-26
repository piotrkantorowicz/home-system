import type { HouseholdMember } from '@modules/household';

export interface MealAccess {
  /** Add, edit, delete planned meals. */
  canPlan: boolean;
  /** Complete, override, reset meals. */
  canLog: boolean;
}

// Mirrors DietPlanner.Application HouseholdRoster: Owner/Adult plan for anyone, Child for
// themselves, Guest for no one; logging is self or Owner/Adult for a managed member.
export function mealAccess(
  myRole: string | null,
  myPersonId: string | undefined,
  target: HouseholdMember | undefined,
): MealAccess {
  const isAdult = myRole === null || myRole === 'Owner' || myRole === 'Adult';
  if (target === undefined || target.personId === myPersonId) {
    const self = myRole !== 'Guest';
    return { canPlan: self, canLog: self };
  }
  return { canPlan: isAdult, canLog: isAdult && target.isManaged };
}

/** Create recipes / products. Mirrors LibraryAccess.CanWrite: a Guest is read-only. */
export function canWriteLibrary(myRole: string | null): boolean {
  return myRole !== 'Guest';
}

/** Members the caller may plan meals for, the caller first. */
export function plannableMembers(
  members: HouseholdMember[],
  myRole: string | null,
  myPersonId: string | undefined,
): HouseholdMember[] {
  return members
    .filter((m) => mealAccess(myRole, myPersonId, m).canPlan)
    .sort((a, b) => Number(b.personId === myPersonId) - Number(a.personId === myPersonId));
}
