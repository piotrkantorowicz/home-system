import { NAV_GROUP_SETTINGS } from '@shared/lib/module-registry';
import { Bell, Inbox as InboxIcon, SlidersHorizontal } from 'lucide-react';
import { lazy } from 'react';

import { UnreadBadge } from './components/UnreadBadge';
import en from './locales/en.json';
import pl from './locales/pl.json';

import type { AppModule } from '@shared/lib/module-registry';

const Inbox = lazy(() => import('./pages/Inbox'));
const ChannelPreferences = lazy(() => import('./pages/ChannelPreferences'));

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
      icon: InboxIcon,
      translationKey: 'common.inbox',
      Badge: UnreadBadge,
    },
    {
      name: 'Preferences',
      href: '/notifications/preferences',
      icon: SlidersHorizontal,
      translationKey: 'common.preferences',
      group: NAV_GROUP_SETTINGS,
    },
  ],
  routes: [
    { index: true, Component: Inbox },
    { path: 'preferences', Component: ChannelPreferences },
  ],
};
