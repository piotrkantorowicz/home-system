import { HouseholdRoleContext } from '@shared/context/HouseholdRoleContext';
import { NavigationAccessContext } from '@shared/context/NavigationAccessContext';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, useLocation } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { OPEN_COMMAND_PALETTE_EVENT } from './CommandPalette';
import { Sidebar } from './Sidebar';

const icon = () => <svg />;
const Unread = () => <span>3 unread</span>;

const modules = [
  {
    name: 'diet-planner',
    basePath: '/diet-planner',
    translationKey: 'common.diet_planner',
    descriptionKey: 'diet_desc',
    icon,
    navItems: [
      {
        name: 'Today',
        href: '/diet-planner',
        icon,
        translationKey: 'nav.today',
        group: 'nav_groups.plan',
      },
      {
        name: 'Recipes',
        href: '/diet-planner/recipes',
        icon,
        translationKey: 'nav.recipes',
        group: 'nav_groups.library',
      },
      {
        name: 'Prefs',
        href: '/diet-planner/preferences',
        icon,
        translationKey: 'nav.prefs',
        group: '__settings__',
      },
    ],
  },
  {
    name: 'budget',
    basePath: '/budget',
    translationKey: 'budget_nav',
    descriptionKey: 'budget_desc',
    icon,
    householdRoles: ['Owner', 'Adult', 'Child'],
    navItems: [
      { name: 'Overview', href: '/budget', icon, translationKey: 'nav.overview' },
      {
        name: 'Settle up',
        href: '/budget/settlement',
        icon,
        translationKey: 'nav.settle',
        householdRoles: ['Owner', 'Adult'],
      },
    ],
  },
  {
    name: 'household',
    basePath: '/household',
    placement: 'footer',
    translationKey: 'household_nav',
    icon,
    navItems: [{ name: 'H', href: '/household', icon, translationKey: 'household_nav' }],
  },
  {
    name: 'notifications',
    basePath: '/notifications',
    placement: 'footer',
    translationKey: 'common.notifications',
    icon,
    navItems: [
      {
        name: 'Inbox',
        href: '/notifications',
        icon,
        translationKey: 'common.inbox',
        Badge: Unread,
      },
    ],
  },
  {
    name: 'admin',
    basePath: '/admin',
    placement: 'footer',
    requiredRole: 'admin',
    translationKey: 'common.admin',
    icon,
    navItems: [{ name: 'DL', href: '/admin', icon, translationKey: 'common.dead_letters' }],
  },
];

vi.mock('@shared/lib/module-registry', async (orig) => {
  // eslint-disable-next-line @typescript-eslint/consistent-type-imports
  const actual = await orig<typeof import('@shared/lib/module-registry')>();
  return { ...actual, getModules: () => modules };
});

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}));

vi.mock('@shared/context/ThemeContext', () => ({
  useTheme: () => ({ theme: 'light', resolvedTheme: 'light', setTheme: vi.fn() }),
}));

let roles: string[] = [];
vi.mock('react-oidc-context', () => ({
  useAuth: () => ({
    user: { profile: { name: 'Kamil Malinowski', roles } },
    signoutRedirect: vi.fn(),
  }),
}));

function LocationProbe() {
  return <output aria-label="Current path">{useLocation().pathname}</output>;
}

function renderSidebar(
  pathname = '/diet-planner',
  { household = 'Owner', restricted = false } = {},
) {
  return render(
    <MemoryRouter initialEntries={[pathname]}>
      <HouseholdRoleContext value={household}>
        <NavigationAccessContext
          value={
            restricted ? { allowedPath: '/household', reason: 'Create your home first' } : null
          }
        >
          <Sidebar />
          <LocationProbe />
        </NavigationAccessContext>
      </HouseholdRoleContext>
    </MemoryRouter>,
  );
}

beforeEach(() => {
  roles = [];
  window.localStorage.clear();
});

describe('Sidebar', () => {
  it('shows the active module nav in groups, with the current page marked', () => {
    renderSidebar('/diet-planner/recipes');
    const nav = screen.getByRole('navigation', { name: 'common.diet_planner' });
    expect(within(nav).getByText('nav_groups.plan')).toBeInTheDocument();
    expect(within(nav).getByText('nav_groups.library')).toBeInTheDocument();
    expect(within(nav).getByRole('link', { name: 'nav.recipes' })).toHaveAttribute(
      'aria-current',
      'page',
    );
    expect(within(nav).getByRole('link', { name: 'nav.today' })).not.toHaveAttribute(
      'aria-current',
    );
  });

  it('marks the module root active only on an exact match', () => {
    renderSidebar('/diet-planner/recipes');
    expect(screen.getByRole('link', { name: 'nav.today' })).not.toHaveAttribute('aria-current');
  });

  it('puts Household, Notifications (with badge) and Settings in the footer; Admin only for admins', () => {
    renderSidebar();
    const footer = screen.getByRole('navigation', { name: 'common.shell.destinations' });
    expect(within(footer).getByRole('link', { name: 'household_nav' })).toHaveAttribute(
      'href',
      '/household',
    );
    expect(within(footer).getByRole('link', { name: /common.notifications/ })).toHaveTextContent(
      '3 unread',
    );
    expect(within(footer).getByRole('link', { name: 'common.shell.settings' })).toHaveAttribute(
      'href',
      '/diet-planner/preferences',
    );
    expect(within(footer).queryByRole('link', { name: 'common.admin' })).not.toBeInTheDocument();
  });

  it('shows Admin to the admin role', () => {
    roles = ['admin'];
    renderSidebar();
    expect(screen.getByRole('link', { name: 'common.admin' })).toHaveAttribute('href', '/admin');
  });

  it('hides nav items the household role may not see', () => {
    renderSidebar('/budget', { household: 'Child' });
    expect(screen.getByRole('link', { name: 'nav.overview' })).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'nav.settle' })).not.toBeInTheDocument();
  });

  it('keeps the remembered module nav on a footer page', () => {
    window.localStorage.setItem('home-system-last-module', 'budget');
    renderSidebar('/notifications');
    expect(screen.getByRole('navigation', { name: 'budget_nav' })).toBeInTheDocument();
  });

  it('falls back to the first visible module for a guest whose remembered module is hidden', () => {
    window.localStorage.setItem('home-system-last-module', 'budget');
    renderSidebar('/household', { household: 'Guest' });
    expect(screen.getByRole('navigation', { name: 'common.diet_planner' })).toBeInTheDocument();
  });

  it('disables every destination but the allowed one before household setup', async () => {
    renderSidebar('/household', { restricted: true });
    expect(screen.getByRole('link', { name: 'household_nav' })).toBeInTheDocument();
    const disabled = screen.getByText('nav.today').closest('[aria-disabled="true"]');
    expect(disabled).toHaveAttribute('title', 'Create your home first');
    await userEvent.click(screen.getByText('nav.today'));
    expect(screen.getByLabelText('Current path')).toHaveTextContent('/household');
  });

  it('opens the command palette from the search button', async () => {
    const opened = vi.fn();
    window.addEventListener(OPEN_COMMAND_PALETTE_EVENT, opened);
    renderSidebar();
    await userEvent.click(screen.getByRole('button', { name: /common.search_everything/ }));
    expect(opened).toHaveBeenCalledOnce();
    window.removeEventListener(OPEN_COMMAND_PALETTE_EVENT, opened);
  });

  it('shows the user and the account menu trigger', () => {
    renderSidebar();
    expect(screen.getByText('Kamil Malinowski')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'common.user_menu' })).toBeInTheDocument();
  });
});

describe('module switcher', () => {
  it('lists product modules with summaries, marks the current one and links to Household', async () => {
    renderSidebar('/diet-planner');
    await userEvent.click(screen.getByRole('button', { name: 'common.switch_module' }));
    const current = await screen.findByRole('menuitem', { name: /common.diet_planner/ });
    expect(current).toHaveTextContent('diet_desc');
    expect(within(current).getByLabelText('common.current')).toBeInTheDocument();
    expect(screen.getByRole('menuitem', { name: /budget_nav/ })).toHaveTextContent('budget_desc');
    expect(screen.getByRole('menuitem', { name: 'household_nav' })).toBeInTheDocument();
    expect(screen.queryByRole('menuitem', { name: /common.admin/ })).not.toBeInTheDocument();
  });

  it('switches module from the keyboard', async () => {
    renderSidebar('/diet-planner');
    await userEvent.click(screen.getByRole('button', { name: 'common.switch_module' }));
    await userEvent.keyboard('{ArrowDown}{ArrowDown}{Enter}');
    expect(screen.getByLabelText('Current path')).toHaveTextContent('/budget');
  });

  it('disables destinations during household setup', async () => {
    renderSidebar('/household', { restricted: true });
    await userEvent.click(screen.getByRole('button', { name: 'common.switch_module' }));
    expect(await screen.findByRole('menuitem', { name: /common.diet_planner/ })).toHaveAttribute(
      'aria-disabled',
      'true',
    );
    expect(screen.getByText('Create your home first')).toBeInTheDocument();
  });
});
