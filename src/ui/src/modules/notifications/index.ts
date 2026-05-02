import { Bell } from 'lucide-react';
import { lazy } from 'react';

import en from './locales/en.json';
import pl from './locales/pl.json';

import type { AppModule } from '@shared/lib/module-registry';

const Inbox = lazy(() => import('./pages/Inbox'));

export const notificationsModule: AppModule = {
  name: 'notifications',
  translationKey: 'common.notifications',
  description: 'View and manage your notifications',
  basePath: '/notifications',
  icon: Bell,
  localeNamespaces: ['notifications'],
  i18nResources: {
    en: { notifications: en },
    pl: { notifications: pl },
  },
  navItems: [
    {
      name: 'Inbox',
      href: '/notifications',
      icon: Bell,
      translationKey: 'common.notifications',
    },
  ],
  routes: [{ index: true, Component: Inbox }],
};
