import { NAV_GROUP_SETTINGS } from '@shared/lib/module-registry';
import { House } from 'lucide-react';
import { lazy } from 'react';

import en from './locales/en.json';
import pl from './locales/pl.json';

import type { AppModule } from '@shared/lib/module-registry';

export { HouseholdProvider } from './components/HouseholdProvider';
export { HouseholdRequired } from './components/HouseholdRequired';
export { useHousehold } from './hooks/useHousehold';
export type { Household, HouseholdMember, HouseholdRole } from './types';

const HouseholdPage = lazy(() => import('./pages/HouseholdPage'));

export const householdModule: AppModule = {
  name: 'household',
  translationKey: 'household_nav',
  basePath: '/household',
  icon: House,
  description: 'Manage the people in your home',
  localeNamespaces: ['household'],
  i18nResources: { en: { household: en }, pl: { household: pl } },
  navItems: [
    {
      name: 'Household',
      href: '/household',
      icon: House,
      translationKey: 'household_nav',
      group: NAV_GROUP_SETTINGS,
    },
  ],
  routes: [{ index: true, Component: HouseholdPage }],
};
