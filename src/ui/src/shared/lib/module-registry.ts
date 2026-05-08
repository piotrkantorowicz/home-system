import type { LucideIcon } from 'lucide-react';
import type { ComponentType } from 'react';
import type { RouteObject } from 'react-router-dom';

export interface NavItem {
  name: string;
  href: string;
  icon: LucideIcon;
  translationKey: string;
  Badge?: ComponentType;
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
