import type { LucideIcon } from 'lucide-react';
import type { ComponentType } from 'react';
import type { RouteObject } from 'react-router-dom';

/** Reserved `group` value: rendered pinned at the bottom of the section panel,
 * below a divider, without a group heading (e.g. a module's Preferences link). */
export const NAV_GROUP_SETTINGS = '__settings__';

export interface NavItem {
  name: string;
  href: string;
  icon: LucideIcon;
  translationKey: string;
  Badge?: ComponentType;
  /**
   * Section-panel group heading, as an i18n key (resolved with `t()`), e.g.
   * `"nav_groups.plan"`. Omit to fall into the first, unlabeled group — so a
   * module that hasn't adopted grouping yet keeps working. Use
   * `NAV_GROUP_SETTINGS` to pin the item at the bottom instead.
   */
  group?: string;
}

export interface AppModule {
  name: string;
  translationKey: string;
  description?: string;
  basePath: string;
  icon: LucideIcon;
  routes: RouteObject[];
  navItems: NavItem[];
  dashboardWidgets?: ComponentType[];
  localeNamespaces: string[];
  i18nResources: {
    en: Record<string, unknown>;
    pl: Record<string, unknown>;
  };
}

const modules: AppModule[] = [];

export function registerModule(mod: AppModule): void {
  if (!modules.find((m) => m.name === mod.name)) {
    modules.push(mod);
  }
}

export function getModules(): readonly AppModule[] {
  return modules;
}
