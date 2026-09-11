import type { components } from './api/generated/schema';

export type Household = components['schemas']['MyHouseholdDto'];
export type HouseholdMember = components['schemas']['HouseholdMemberDto'];
export type HouseholdRole = 'Owner' | 'Adult' | 'Child' | 'Guest';
export const HOUSEHOLD_ROLES: HouseholdRole[] = ['Owner', 'Adult', 'Child', 'Guest'];
