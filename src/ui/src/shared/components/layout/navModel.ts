import { getModules, NAV_GROUP_SETTINGS } from '@shared/lib/module-registry';

/** localStorage key AppShell writes to on every navigation, and RootRedirect
 * reads from to send `/` back to the module last actually visited. */
export const LAST_MODULE_STORAGE_KEY = 'home-system-last-module';

/** The last product module visited, or `null` when none is stored or storage is unavailable. */
export function readLastModule(): string | null {
  try {
    return window.localStorage.getItem(LAST_MODULE_STORAGE_KEY);
  } catch {
    return null;
  }
}

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
  /** One-line summary for the module switcher; absent when the module declares none. */
  description?: string;
}

/** A sidebar footer destination backed by a `placement: 'footer'` module. */
export interface FooterDestination {
  name: string;
  /** The module's first nav item (its landing page). */
  href: string;
  icon: LucideIcon;
  label: string;
  Badge?: ComponentType;
  /** Exact-match the route, so `/household` is not active on `/household/anything`. */
  end: boolean;
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

/**
 * Whether a user holding token `roles` and household role `householdRole` may see the module
 * (see `AppModule.requiredRole` / `AppModule.householdRoles`).
 */
export function isModuleVisible(
  mod: AppModule,
  roles: readonly string[],
  householdRole: string | null = null,
): boolean {
  if (mod.requiredRole && !roles.includes(mod.requiredRole)) return false;
  return (
    !mod.householdRoles || (householdRole !== null && mod.householdRoles.includes(householdRole))
  );
}

/** Whether the household role may see a nav item (see `NavItem.householdRoles`). */
export function isNavItemVisible(
  nav: { householdRoles?: readonly string[] | undefined },
  householdRole: string | null,
): boolean {
  return (
    !nav.householdRoles || (householdRole !== null && nav.householdRoles.includes(householdRole))
  );
}

/** Whether the module is a product module listed in the module switcher (not a footer destination). */
export function isSwitcherModule(mod: AppModule): boolean {
  return mod.placement !== 'footer';
}

/** One tile per product module visible to `roles`, for the sidebar's module switcher. */
export function getModuleTiles(
  t: TFunction,
  labels: Readonly<Record<string, string>> = {},
  roles: readonly string[] = [],
  householdRole: string | null = null,
): ModuleTile[] {
  return getModules()
    .filter((mod) => isSwitcherModule(mod) && isModuleVisible(mod, roles, householdRole))
    .map((mod) => ({
      name: mod.name,
      basePath: mod.basePath,
      icon: mod.icon,
      label: labels[mod.name] ?? t(mod.translationKey),
      ...(mod.descriptionKey ? { description: t(mod.descriptionKey) } : {}),
    }));
}

/** Footer destinations (Household, Notifications, Admin…) visible to the caller, in registration order. */
export function getFooterDestinations(
  t: TFunction,
  labels: Readonly<Record<string, string>> = {},
  roles: readonly string[] = [],
  householdRole: string | null = null,
): FooterDestination[] {
  return getModules()
    .filter((mod) => !isSwitcherModule(mod) && isModuleVisible(mod, roles, householdRole))
    .map((mod) => {
      const landing = mod.navItems.find((n) => n.href === mod.basePath) ?? mod.navItems[0];
      const href = landing?.href ?? mod.basePath;
      return {
        name: mod.name,
        href,
        icon: mod.icon,
        label: labels[mod.name] ?? t(mod.translationKey),
        end: href === mod.basePath,
        ...(landing?.Badge ? { Badge: landing.Badge } : {}),
      };
    });
}

/**
 * The product module the sidebar shows nav for. A footer page (Household, Notifications…) keeps the
 * module you came from: the remembered one when still visible, else the first visible product module.
 */
export function resolveShellModule(
  pathname: string,
  roles: readonly string[],
  householdRole: string | null,
  lastModuleName: string | null,
): AppModule | undefined {
  const visible = getModules().filter(
    (mod) => isSwitcherModule(mod) && isModuleVisible(mod, roles, householdRole),
  );
  const active = getActiveModule(pathname);
  if (active && visible.includes(active)) return active;
  return visible.find((mod) => mod.name === lastModuleName) ?? visible[0];
}

/** The registered module whose `basePath` the given pathname falls under. */
export function getActiveModule(pathname: string): AppModule | undefined {
  return getModules()
    .filter((mod) => pathname === mod.basePath || pathname.startsWith(`${mod.basePath}/`))
    .sort((a, b) => b.basePath.length - a.basePath.length)[0];
}

/**
 * The module's nav items as ordered groups for the sidebar, plus the items marked
 * `NAV_GROUP_SETTINGS` (the sidebar's footer "Settings" link points at the first).
 */
export function getSectionGroups(
  t: TFunction,
  mod: AppModule,
  householdRole: string | null = null,
): { groups: NavGroup[]; pinned: RailNavItem[] } {
  const groups: NavGroup[] = [];
  const groupIndex = new Map<string | null, number>();
  const pinned: RailNavItem[] = [];

  for (const nav of mod.navItems.filter((n) => isNavItemVisible(n, householdRole))) {
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
export function getMobileNavItems(
  t: TFunction,
  mod: AppModule,
  householdRole: string | null = null,
): RailNavItem[] {
  return mod.navItems
    .filter((nav) => isNavItemVisible(nav, householdRole))
    .map((nav) => toRailNavItem(t, mod, nav.href, nav.icon, nav.translationKey, nav.Badge));
}
