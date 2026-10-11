import { TrendingUp } from 'lucide-react';
import { lazy } from 'react';

import en from './locales/en.json';
import pl from './locales/pl.json';

import type { AppModule } from '@shared/lib/module-registry';

const InvestmentsPage = lazy(() => import('./pages/InvestmentsPage'));
const WalletDetailPage = lazy(() => import('./pages/WalletDetailPage'));
const CheckInPage = lazy(() => import('./pages/CheckInPage'));

export const investmentsModule: AppModule = {
  name: 'investments',
  translationKey: 'investments_nav',
  basePath: '/investments',
  icon: TrendingUp,
  description: 'Wallets and household net worth',
  descriptionKey: 'investments_description',
  // Guests have no Investments access (server-enforced); this only hides the entry.
  householdRoles: ['Owner', 'Adult', 'Child'],
  localeNamespaces: ['investments'],
  i18nResources: { en: { investments: en }, pl: { investments: pl } },
  navItems: [
    {
      name: 'Net worth',
      href: '/investments',
      icon: TrendingUp,
      translationKey: 'net_worth_nav',
      mobileTab: true,
    },
  ],
  routes: [
    { index: true, Component: InvestmentsPage },
    { path: 'wallets/:id', Component: WalletDetailPage },
    { path: 'check-in', Component: CheckInPage },
  ],
};
