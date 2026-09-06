import { getModules, NAV_GROUP_SETTINGS } from '@shared/lib/module-registry';

/** localStorage key AppShell writes to on every navigation, and RootRedirect
 * reads from to send `/` back to the module last actually visited. */
export const LAST_MODULE_STORAGE_KEY = 'home-system-last-module';

import type { AppModule } from '@shared/lib/module-registry';
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

export interface NavGroup {
  /** `null` for the unlabeled first group. */
  label: string | null;
  items: RailNavItem[];
}

export interface ModuleTile {
  name: string;
  basePath: string;
  icon: LucideIcon;
  label: string;
}

function toRailNavItem(
  t: TFunction,
  mod: AppModule,
  href: string,
  icon: LucideIcon,
  translationKey: string,
  Badge?: ComponentType,
): RailNavItem {
  return {
    href,
    icon,
    label: t(translationKey),
    end: href === mod.basePath,
    ...(Badge ? { Badge } : {}),
  };
}

/** One tile per registered module, for the 64px module rail. */
export function getModuleTiles(t: TFunction): ModuleTile[] {
  return getModules().map((mod) => ({
    name: mod.name,
    basePath: mod.basePath,
    icon: mod.icon,
    label: t(mod.translationKey),
  }));
}

/** The registered module whose `basePath` the given pathname falls under. */
export function getActiveModule(pathname: string): AppModule | undefined {
  return getModules()
    .filter((mod) => pathname === mod.basePath || pathname.startsWith(`${mod.basePath}/`))
    .sort((a, b) => b.basePath.length - a.basePath.length)[0];
}

/**
 * The active module's nav items as ordered groups for the 216px section
 * panel, plus the items pinned below the divider (`NAV_GROUP_SETTINGS`).
 */
export function getSectionGroups(
  t: TFunction,
  mod: AppModule,
): { groups: NavGroup[]; pinned: RailNavItem[] } {
  const groups: NavGroup[] = [];
  const groupIndex = new Map<string | null, number>();
  const pinned: RailNavItem[] = [];

  for (const nav of mod.navItems) {
    const item = toRailNavItem(t, mod, nav.href, nav.icon, nav.translationKey, nav.Badge);

    if (nav.group === NAV_GROUP_SETTINGS) {
      pinned.push(item);
      continue;
    }

    const key = nav.group ?? null;
    let idx = groupIndex.get(key);
    if (idx === undefined) {
      idx = groups.length;
      groupIndex.set(key, idx);
      groups.push({ label: key ? t(key) : null, items: [] });
    }
    groups[idx]?.items.push(item);
  }

  return { groups, pinned };
}

/** Every item of the active module, flattened in registration order — for the
 * mobile bottom tab bar (module-scoped, horizontally scrollable). */
export function getMobileNavItems(t: TFunction, mod: AppModule): RailNavItem[] {
  return mod.navItems.map((nav) =>
    toRailNavItem(t, mod, nav.href, nav.icon, nav.translationKey, nav.Badge),
  );
}
