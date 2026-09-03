import i18n from '@shared/lib/i18n';
import { getModules } from '@shared/lib/module-registry';
import { Home } from 'lucide-react';

import type { LucideIcon } from 'lucide-react';
import type { ComponentType } from 'react';

export interface RailNavItem {
  href: string;
  icon: LucideIcon;
  labelEn: string;
  labelPl: string;
  Badge?: ComponentType;
  /** Exact-match the route (index / module-root links). */
  end: boolean;
}

/**
 * Flattened navigation model shared by the desktop icon rail and the mobile
 * bottom tab bar: the system home followed by every registered module's
 * declared nav items. Both English and Polish labels are resolved up front so
 * the rail can stack them per the bilingual rule.
 */
export function getRailNavItems(): RailNavItem[] {
  const en = i18n.getFixedT('en');
  const pl = i18n.getFixedT('pl');

  const items: RailNavItem[] = [
    { href: '/', icon: Home, labelEn: en('common.home'), labelPl: pl('common.home'), end: true },
  ];

  for (const mod of getModules()) {
    for (const nav of mod.navItems) {
      items.push({
        href: nav.href,
        icon: nav.icon,
        labelEn: en(nav.translationKey),
        labelPl: pl(nav.translationKey),
        end: nav.href === mod.basePath,
        ...(nav.Badge ? { Badge: nav.Badge } : {}),
      });
    }
  }

  return items;
}
