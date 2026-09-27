import { MailWarning, ShieldCheck } from 'lucide-react';
import { lazy } from 'react';

import { DeadLetterBadge } from './components/DeadLetterBadge';
import { ADMIN_ROLE } from './constants';
import en from './locales/en.json';
import pl from './locales/pl.json';

import type { AppModule } from '@shared/lib/module-registry';

const DeadLetters = lazy(() => import('./pages/DeadLetters'));

export const adminModule: AppModule = {
  name: 'admin',
  translationKey: 'common.admin',
  description: 'Operational tools for administrators',
  basePath: '/admin',
  icon: ShieldCheck,
  requiredRole: ADMIN_ROLE,
  localeNamespaces: ['admin'],
  i18nResources: {
    en: { admin: en },
    pl: { admin: pl },
  },
  navItems: [
    {
      name: 'Dead letters',
      href: '/admin',
      icon: MailWarning,
      translationKey: 'common.dead_letters',
      Badge: DeadLetterBadge,
    },
  ],
  routes: [{ index: true, Component: DeadLetters }],
};
