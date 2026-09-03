import { getModules } from '@shared/lib/module-registry';
import { Home } from 'lucide-react';

import type { TFunction } from 'i18next';
import type { LucideIcon } from 'lucide-react';
import type { ComponentType } from 'react';

export interface RailNavItem {
  href: string;
  icon: LucideIcon;
  label: string;
  Badge?: ComponentType;
  /** Exact-match the route (index / module-root links). */
  end: boolean;
}

/**
 * Flattened navigation model shared by the desktop icon rail and the mobile
 * bottom tab bar: the system home followed by every registered module's
 * declared nav items. Labels resolve through the active i18n language.
 */
export function getRailNavItems(t: TFunction): RailNavItem[] {
  const items: RailNavItem[] = [{ href: '/', icon: Home, label: t('common.home'), end: true }];

  for (const mod of getModules()) {
    for (const nav of mod.navItems) {
      items.push({
        href: nav.href,
        icon: nav.icon,
        label: t(nav.translationKey),
        end: nav.href === mod.basePath,
        ...(nav.Badge ? { Badge: nav.Badge } : {}),
      });
    }
  }

  return items;
}
