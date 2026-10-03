import { NAV_GROUP_SETTINGS } from '@shared/lib/module-registry';
import { describe, it, expect, vi } from 'vitest';

import {
  getModuleTiles,
  getActiveModule,
  getSectionGroups,
  getMobileNavItems,
  getFooterDestinations,
  resolveShellModule,
} from './navModel';

import type { AppModule } from '@shared/lib/module-registry';
import type { TFunction } from 'i18next';
import type { LucideIcon } from 'lucide-react';

const DashIcon = (() => null) as unknown as LucideIcon;
const PlanIcon = (() => null) as unknown as LucideIcon;
const PrefIcon = (() => null) as unknown as LucideIcon;
const OtherModIcon = (() => null) as unknown as LucideIcon;

const UnreadBadge = () => null;

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
  placement: 'footer',
  icon: OtherModIcon,
  localeNamespaces: [],
  i18nResources: { en: {}, pl: {} },
  navItems: [
    {
      name: 'Inbox',
      href: '/notifications',
      icon: OtherModIcon,
      translationKey: 'common.inbox',
      Badge: UnreadBadge,
    },
  ],
  routes: [],
};

const familyOnly: AppModule = {
  name: 'family-money',
  translationKey: 'common.family_money',
  basePath: '/family-money',
  icon: OtherModIcon,
  householdRoles: ['Owner', 'Adult', 'Child'],
  localeNamespaces: [],
  i18nResources: { en: {}, pl: {} },
  navItems: [],
  routes: [],
};

const adminOnly: AppModule = {
  name: 'admin',
  translationKey: 'common.admin',
  basePath: '/admin',
  placement: 'footer',
  icon: OtherModIcon,
  requiredRole: 'admin',
  localeNamespaces: [],
  i18nResources: { en: {}, pl: {} },
  navItems: [],
  routes: [],
};

vi.mock('@shared/lib/module-registry', async (orig) => {
  // eslint-disable-next-line @typescript-eslint/consistent-type-imports
  const actual = await orig<typeof import('@shared/lib/module-registry')>();
  return { ...actual, getModules: () => [dietPlanner, notifications, adminOnly, familyOnly] };
});

const t = ((key: string) => key) as unknown as TFunction;

describe('getModuleTiles', () => {
  it('uses live module labels and preserves translated fallbacks', () => {
    const tiles = getModuleTiles(t, { 'diet-planner': 'My label' });
    expect(tiles[0]?.label).toBe('My label');
  });
  it('lists only product modules, never footer destinations', () => {
    const tiles = getModuleTiles(t, {}, ['admin']);
    expect(tiles.map((m) => m.name)).toEqual(['diet-planner']);
    expect(tiles[0]?.label).toBe('common.diet_planner');
  });
  it('carries the translated description when the module declares one', () => {
    expect(getModuleTiles(t)[0]?.description).toBeUndefined();
  });
});

describe('getFooterDestinations', () => {
  it('lists footer modules with their landing page and badge, hiding role-gated ones', () => {
    const items = getFooterDestinations(t);
    expect(items.map((i) => i.name)).toEqual(['notifications']);
    expect(items[0]).toMatchObject({
      href: '/notifications',
      label: 'common.notifications',
      end: true,
    });
    expect(items[0]?.Badge).toBe(UnreadBadge);
  });
  it('shows Admin only to the admin role', () => {
    expect(getFooterDestinations(t, {}, ['editor']).map((i) => i.name)).not.toContain('admin');
    expect(getFooterDestinations(t, {}, ['admin']).map((i) => i.name)).toEqual([
      'notifications',
      'admin',
    ]);
  });
  it('uses live module labels', () => {
    expect(getFooterDestinations(t, { notifications: 'Inbox!' })[0]?.label).toBe('Inbox!');
  });
});

describe('resolveShellModule', () => {
  it('is the product module the path belongs to', () => {
    expect(resolveShellModule('/diet-planner/products', [], null, null)?.name).toBe('diet-planner');
  });
  it('keeps the remembered module on a footer page, else the first visible one', () => {
    expect(resolveShellModule('/notifications', [], 'Owner', 'family-money')?.name).toBe(
      'family-money',
    );
    expect(resolveShellModule('/notifications', [], 'Guest', 'family-money')?.name).toBe(
      'diet-planner',
    );
    expect(resolveShellModule('/admin', ['admin'], null, null)?.name).toBe('diet-planner');
  });
  it('never returns a role-gated product module the user cannot see', () => {
    expect(resolveShellModule('/family-money', [], 'Guest', null)?.name).toBe('diet-planner');
  });
});

describe('module visibility', () => {
  it('hides a module whose required role the user lacks', () => {
    expect(getFooterDestinations(t, {}, ['editor']).map((m) => m.name)).not.toContain('admin');
  });
  it('hides a household-role-gated module without a household or for a disallowed role', () => {
    expect(getModuleTiles(t).map((m) => m.name)).not.toContain('family-money');
    expect(getModuleTiles(t, {}, [], 'Guest').map((m) => m.name)).not.toContain('family-money');
  });
  it('shows a household-role-gated module to an allowed role', () => {
    for (const role of ['Owner', 'Adult', 'Child'])
      expect(getModuleTiles(t, {}, [], role).map((m) => m.name)).toContain('family-money');
  });
});

describe('nav items gated by household role', () => {
  const gated: AppModule = {
    ...dietPlanner,
    navItems: [
      { name: 'Open', href: '/x', icon: DashIcon, translationKey: 'open' },
      {
        name: 'Adults',
        href: '/x/adults',
        icon: DashIcon,
        translationKey: 'adults',
        householdRoles: ['Owner', 'Adult'],
      },
    ],
  };

  it('hides an item from roles it does not list, in the panel and the mobile bar', () => {
    const labels = (role: string | null) =>
      getSectionGroups(t, gated, role).groups.flatMap((g) => g.items.map((i) => i.label));
    expect(labels('Adult')).toEqual(['open', 'adults']);
    expect(labels('Child')).toEqual(['open']);
    expect(labels(null)).toEqual(['open']);
    expect(getMobileNavItems(t, gated, 'Child').map((i) => i.label)).toEqual(['open']);
    expect(getMobileNavItems(t, gated, 'Owner').map((i) => i.label)).toEqual(['open', 'adults']);
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
