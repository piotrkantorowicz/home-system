import { NAV_GROUP_SETTINGS } from '@shared/lib/module-registry';
import { describe, it, expect, vi } from 'vitest';

import { getModuleTiles, getActiveModule, getSectionGroups, getMobileNavItems } from './navModel';

import type { AppModule } from '@shared/lib/module-registry';
import type { TFunction } from 'i18next';
import type { LucideIcon } from 'lucide-react';

const DashIcon = (() => null) as unknown as LucideIcon;
const PlanIcon = (() => null) as unknown as LucideIcon;
const PrefIcon = (() => null) as unknown as LucideIcon;
const OtherModIcon = (() => null) as unknown as LucideIcon;

const dietPlanner: AppModule = {
  name: 'diet-planner',
  translationKey: 'common.diet_planner',
  basePath: '/diet-planner',
  icon: DashIcon,
  localeNamespaces: [],
  i18nResources: { en: {}, pl: {} },
  navItems: [
    {
      name: 'Dashboard',
      href: '/diet-planner',
      icon: DashIcon,
      translationKey: 'common.dashboard',
      group: 'nav_groups.plan',
    },
    {
      name: 'Meal plan',
      href: '/diet-planner/calendar',
      icon: PlanIcon,
      translationKey: 'common.meal_plan',
      group: 'nav_groups.plan',
    },
    {
      name: 'Products',
      href: '/diet-planner/products',
      icon: PlanIcon,
      translationKey: 'common.products',
      group: 'nav_groups.library',
    },
    {
      name: 'Preferences',
      href: '/diet-planner/preferences',
      icon: PrefIcon,
      translationKey: 'common.preferences',
      group: NAV_GROUP_SETTINGS,
    },
  ],
  routes: [],
};

const notifications: AppModule = {
  name: 'notifications',
  translationKey: 'common.notifications',
  basePath: '/notifications',
  icon: OtherModIcon,
  localeNamespaces: [],
  i18nResources: { en: {}, pl: {} },
  navItems: [
    { name: 'Inbox', href: '/notifications', icon: OtherModIcon, translationKey: 'common.inbox' },
  ],
  routes: [],
};

vi.mock('@shared/lib/module-registry', async (orig) => {
  // eslint-disable-next-line @typescript-eslint/consistent-type-imports
  const actual = await orig<typeof import('@shared/lib/module-registry')>();
  return { ...actual, getModules: () => [dietPlanner, notifications] };
});

const t = ((key: string) => key) as unknown as TFunction;

describe('getModuleTiles', () => {
  it('uses live module labels and preserves translated fallbacks', () => {
    const tiles = getModuleTiles(t, { 'diet-planner': 'My label' });
    expect(tiles[0]?.label).toBe('My label');
    expect(tiles[1]?.label).toBe('common.notifications');
  });
  it('returns one tile per registered module', () => {
    const tiles = getModuleTiles(t);
    expect(tiles.map((m) => m.name)).toEqual(['diet-planner', 'notifications']);
    expect(tiles[0]?.label).toBe('common.diet_planner');
  });
});

describe('getActiveModule', () => {
  it('matches a pathname under a module basePath', () => {
    expect(getActiveModule('/diet-planner/products')?.name).toBe('diet-planner');
    expect(getActiveModule('/notifications')?.name).toBe('notifications');
  });

  it('returns undefined for an unregistered path', () => {
    expect(getActiveModule('/')).toBeUndefined();
  });
});

describe('getSectionGroups', () => {
  it('groups items by their group key, in first-seen order', () => {
    const { groups } = getSectionGroups(t, dietPlanner);
    expect(groups.map((g) => g.label)).toEqual(['nav_groups.plan', 'nav_groups.library']);
    expect(groups[0]?.items.map((i) => i.href)).toEqual([
      '/diet-planner',
      '/diet-planner/calendar',
    ]);
  });

  it('pins NAV_GROUP_SETTINGS items separately, excluded from groups', () => {
    const { groups, pinned } = getSectionGroups(t, dietPlanner);
    expect(pinned.map((i) => i.href)).toEqual(['/diet-planner/preferences']);
    expect(groups.some((g) => g.items.some((i) => i.href === '/diet-planner/preferences'))).toBe(
      false,
    );
  });

  it('falls ungrouped items into a single unlabeled first group', () => {
    const { groups } = getSectionGroups(t, notifications);
    expect(groups).toHaveLength(1);
    expect(groups[0]?.label).toBeNull();
  });
});

describe('getMobileNavItems', () => {
  it('flattens every nav item of the module, in registration order', () => {
    const items = getMobileNavItems(t, dietPlanner);
    expect(items.map((i) => i.href)).toEqual([
      '/diet-planner',
      '/diet-planner/calendar',
      '/diet-planner/products',
      '/diet-planner/preferences',
    ]);
  });
});
