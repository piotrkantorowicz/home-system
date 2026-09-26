import { mealAccess, plannableMembers } from './householdAccess';

import type { HouseholdMember } from '@modules/household';

function member(personId: string, role: string, isManaged = false): HouseholdMember {
  return { personId, displayName: personId, avatarUrl: null, role, nickname: null, isManaged };
}

const me = member('me', 'Adult');
const kid = member('kid', 'Child', true);
const partner = member('partner', 'Adult');

describe('mealAccess', () => {
  it('lets anyone but a guest plan and log for themselves', () => {
    expect(mealAccess('Child', 'me', undefined)).toEqual({ canPlan: true, canLog: true });
    expect(mealAccess('Guest', 'me', undefined)).toEqual({ canPlan: false, canLog: false });
  });

  it('lets an adult plan for anyone but log only for a managed member', () => {
    expect(mealAccess('Adult', 'me', kid)).toEqual({ canPlan: true, canLog: true });
    expect(mealAccess('Owner', 'me', partner)).toEqual({ canPlan: true, canLog: false });
  });

  it('gives a child read-only access to other members', () => {
    expect(mealAccess('Child', 'me', partner)).toEqual({ canPlan: false, canLog: false });
  });
});

describe('plannableMembers', () => {
  it('lists every member for an adult, the caller first', () => {
    expect(plannableMembers([kid, partner, me], 'Adult', 'me').map((m) => m.personId)).toEqual([
      'me',
      'kid',
      'partner',
    ]);
  });

  it('lists only the caller for a child and nobody for a guest', () => {
    expect(plannableMembers([kid, me], 'Child', 'me').map((m) => m.personId)).toEqual(['me']);
    expect(plannableMembers([kid, me], 'Guest', 'me')).toEqual([]);
  });
});
