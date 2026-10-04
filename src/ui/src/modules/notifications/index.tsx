import { Bell, Inbox as InboxIcon } from 'lucide-react';
import { lazy } from 'react';
import { Navigate } from 'react-router-dom';

import { UnreadBadge } from './components/UnreadBadge';
import en from './locales/en.json';
import pl from './locales/pl.json';

import type { AppModule } from '@shared/lib/module-registry';

const Inbox = lazy(() => import('./pages/Inbox'));

export { ChannelPreferencesSection } from './components/ChannelPreferencesSection';

// Channel preferences now live in App & account (#502).
const APP_SETTINGS_PATH = '/diet-planner/settings/app';

export const notificationsModule: AppModule = {
  name: 'notifications',
  translationKey: 'common.notifications',
  description: 'View and manage your notifications',
  basePath: '/notifications',
  placement: 'footer',
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
      icon: InboxIcon,
      translationKey: 'common.inbox',
      Badge: UnreadBadge,
    },
  ],
  routes: [
    { index: true, Component: Inbox },
    { path: 'preferences', element: <Navigate to={APP_SETTINGS_PATH} replace /> },
  ],
};
