import { HouseholdRoleContext } from '@shared/context/HouseholdRoleContext';
import { NavigationAccessContext } from '@shared/context/NavigationAccessContext';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, useLocation } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { BottomTabBar } from './BottomTabBar';

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
        mobileTab: true,
      },
      {
        name: 'Plan',
        href: '/diet-planner/calendar',
        icon,
        translationKey: 'nav.plan',
        group: 'nav_groups.plan',
        mobileTab: true,
      },
      {
        name: 'Import',
        href: '/diet-planner/import',
        icon,
        translationKey: 'nav.import',
        group: 'nav_groups.plan',
      },
      {
        name: 'Recipes',
        href: '/diet-planner/recipes',
        icon,
        translationKey: 'nav.recipes',
        group: 'nav_groups.library',
        mobileTab: true,
      },
      {
        name: 'Shopping',
        href: '/diet-planner/shopping-list',
        icon,
        translationKey: 'nav.shopping',
        group: 'nav_groups.library',
        mobileTab: true,
      },
      {
        name: 'Products',
        href: '/diet-planner/products',
        icon,
        translationKey: 'nav.products',
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
    icon,
    householdRoles: ['Owner', 'Adult', 'Child'],
    mobileAction: { translationKey: 'add_expense', href: '/budget/expenses?add=1', icon },
    navItems: [
      { name: 'Overview', href: '/budget', icon, translationKey: 'nav.overview', mobileTab: true },
      {
        name: 'Expenses',
        href: '/budget/expenses',
        icon,
        translationKey: 'nav.expenses',
        mobileTab: true,
      },
      {
        name: 'Settle up',
        href: '/budget/settlement',
        icon,
        translationKey: 'nav.settle',
        mobileTab: true,
        householdRoles: ['Owner', 'Adult'],
      },
      { name: 'Envelopes', href: '/budget/envelopes', icon, translationKey: 'nav.envelopes' },
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

vi.mock('react-oidc-context', () => ({
  useAuth: () => ({
    user: { profile: { name: 'Kamil Malinowski' } },
    signoutRedirect: vi.fn(),
  }),
}));

function LocationProbe() {
  const location = useLocation();
  return <output aria-label="Current path">{location.pathname + location.search}</output>;
}

function renderBar(pathname = '/diet-planner', { household = 'Owner', restricted = false } = {}) {
  return render(
    <MemoryRouter initialEntries={[pathname]}>
      <HouseholdRoleContext value={household}>
        <NavigationAccessContext
          value={
            restricted ? { allowedPath: '/household', reason: 'Create your home first' } : null
          }
        >
          <BottomTabBar />
          <LocationProbe />
        </NavigationAccessContext>
      </HouseholdRoleContext>
    </MemoryRouter>,
  );
}

beforeEach(() => {
  window.localStorage.clear();
});

describe('BottomTabBar', () => {
  it('shows the module tabs plus More — five slots — and marks the current tab', () => {
    renderBar('/diet-planner/recipes');
    const bar = screen.getByRole('navigation', { name: 'common.diet_planner' });
    const labels = [...within(bar).getAllByRole('link'), ...within(bar).getAllByRole('button')].map(
      (e) => e.textContent,
    );
    expect(labels).toEqual([
      'nav.today',
      'nav.plan',
      'nav.recipes',
      'nav.shopping',
      'common.shell.more',
    ]);
    expect(within(bar).getByRole('link', { name: 'nav.recipes' })).toHaveAttribute(
      'aria-current',
      'page',
    );
    expect(within(bar).getByRole('link', { name: 'nav.today' })).not.toHaveAttribute(
      'aria-current',
    );
  });

  it('puts the raised action between the tabs, linking to the add form', async () => {
    renderBar('/budget', { household: 'Owner' });
    const bar = screen.getByRole('navigation', { name: 'budget_nav' });
    const names = within(bar)
      .getAllByRole('link')
      .map((e) => e.textContent);
    expect(names).toEqual(['nav.overview', 'nav.expenses', 'add_expense', 'nav.settle']);
    await userEvent.click(within(bar).getByRole('link', { name: 'add_expense' }));
    expect(screen.getByLabelText('Current path')).toHaveTextContent('/budget/expenses?add=1');
  });

  it('drops the Settle up tab for children but keeps the add action', () => {
    renderBar('/budget', { household: 'Child' });
    const bar = screen.getByRole('navigation', { name: 'budget_nav' });
    expect(within(bar).queryByRole('link', { name: 'nav.settle' })).not.toBeInTheDocument();
    expect(within(bar).getByRole('link', { name: 'add_expense' })).toBeInTheDocument();
  });

  it('keeps the remembered module on a footer page and marks More active there', () => {
    window.localStorage.setItem('home-system-last-module', 'budget');
    renderBar('/notifications');
    const bar = screen.getByRole('navigation', { name: 'budget_nav' });
    expect(within(bar).getByRole('button', { name: 'common.shell.more' })).toHaveAttribute(
      'aria-current',
      'page',
    );
  });

  it('marks More active when the current page lives in it', () => {
    renderBar('/diet-planner/products');
    expect(screen.getByRole('button', { name: 'common.shell.more' })).toHaveAttribute(
      'aria-current',
      'page',
    );
  });

  it('marks More active on the pinned module settings page', () => {
    renderBar('/diet-planner/preferences');
    expect(screen.getByRole('button', { name: 'common.shell.more' })).toHaveAttribute(
      'aria-current',
      'page',
    );
  });

  it('disables tabs before household setup', async () => {
    renderBar('/household', { restricted: true });
    const disabled = screen.getByText('nav.today').closest('[aria-disabled="true"]');
    expect(disabled).toHaveAttribute('title', 'Create your home first');
    await userEvent.click(screen.getByText('nav.today'));
    expect(screen.getByLabelText('Current path')).toHaveTextContent('/household');
  });
});

describe('More sheet', () => {
  const openMore = async () => {
    await userEvent.click(screen.getByRole('button', { name: 'common.shell.more' }));
    return screen.findByRole('dialog', { name: 'common.shell.more' });
  };

  it('lists the remaining destinations, the module switcher and the footer group', async () => {
    renderBar('/diet-planner');
    const sheet = await openMore();
    expect(within(sheet).getByRole('link', { name: 'nav.import' })).toBeInTheDocument();
    expect(within(sheet).getByRole('link', { name: 'nav.products' })).toBeInTheDocument();
    expect(within(sheet).queryByRole('link', { name: 'nav.today' })).not.toBeInTheDocument();
    expect(within(sheet).getByRole('button', { name: /common.diet_planner/ })).toHaveAttribute(
      'aria-current',
      'true',
    );
    expect(within(sheet).getByRole('button', { name: /budget_nav/ })).toBeInTheDocument();
    expect(within(sheet).getByRole('link', { name: 'household_nav' })).toBeInTheDocument();
    expect(within(sheet).getByRole('link', { name: /common.notifications/ })).toHaveTextContent(
      '3 unread',
    );
    expect(within(sheet).getByRole('link', { name: 'common.shell.settings' })).toHaveAttribute(
      'href',
      '/diet-planner/preferences',
    );
    expect(within(sheet).getByRole('button', { name: 'common.user_menu' })).toBeInTheDocument();
  });

  it('navigates and closes when a destination is chosen', async () => {
    renderBar('/diet-planner');
    const sheet = await openMore();
    await userEvent.click(within(sheet).getByRole('link', { name: 'nav.products' }));
    expect(screen.getByLabelText('Current path')).toHaveTextContent('/diet-planner/products');
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('switches module from the sheet', async () => {
    renderBar('/diet-planner');
    const sheet = await openMore();
    await userEvent.click(within(sheet).getByRole('button', { name: /budget_nav/ }));
    expect(screen.getByLabelText('Current path')).toHaveTextContent('/budget');
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('moves focus into the sheet, keeps it there, and closes on Escape returning focus to More', async () => {
    renderBar('/diet-planner');
    const more = screen.getByRole('button', { name: 'common.shell.more' });
    const sheet = await openMore();
    expect(sheet).toContainElement(document.activeElement as HTMLElement);
    for (let i = 0; i < 30; i++) await userEvent.tab();
    expect(sheet).toContainElement(document.activeElement as HTMLElement);
    await userEvent.keyboard('{Escape}');
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    await waitFor(() => {
      expect(more).toHaveFocus();
    });
  });
});
