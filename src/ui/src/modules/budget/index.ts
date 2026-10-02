import { Receipt, Scale, Wallet } from 'lucide-react';
import { lazy } from 'react';

import { BudgetLayout } from './components/BudgetLayout';
import en from './locales/en.json';
import pl from './locales/pl.json';

import type { AppModule } from '@shared/lib/module-registry';

const BudgetPage = lazy(() => import('./pages/BudgetPage'));
const SettlementPage = lazy(() => import('./pages/SettlementPage'));
const EnvelopesPage = lazy(() => import('./pages/EnvelopesPage'));
const ExpensesPage = lazy(() => import('./pages/ExpensesPage'));
const ExpenseDetailPage = lazy(() => import('./pages/ExpenseDetailPage'));

export const budgetModule: AppModule = {
  name: 'budget',
  translationKey: 'budget_nav',
  basePath: '/budget',
  icon: Wallet,
  description: 'Household envelopes and spending',
  // Guests have no Budget access (server-enforced); this only hides the entry.
  householdRoles: ['Owner', 'Adult', 'Child'],
  localeNamespaces: ['budget'],
  i18nResources: { en: { budget: en }, pl: { budget: pl } },
  navItems: [
    { name: 'Overview', href: '/budget', icon: Wallet, translationKey: 'overview_nav' },
    { name: 'Expenses', href: '/budget/expenses', icon: Receipt, translationKey: 'expenses_nav' },
    {
      name: 'Settle up',
      href: '/budget/settlement',
      icon: Scale,
      translationKey: 'settlement_nav',
      // Shared debt is adult-only; children never see it.
      householdRoles: ['Owner', 'Adult'],
    },
    { name: 'Envelopes', href: '/budget/envelopes', icon: Wallet, translationKey: 'envelopes_nav' },
  ],
  routes: [
    {
      Component: BudgetLayout,
      children: [
        { index: true, Component: BudgetPage },
        { path: 'expenses', Component: ExpensesPage },
        { path: 'expenses/:id', Component: ExpenseDetailPage },
        { path: 'settlement', Component: SettlementPage },
        { path: 'envelopes', Component: EnvelopesPage },
      ],
    },
  ],
};
