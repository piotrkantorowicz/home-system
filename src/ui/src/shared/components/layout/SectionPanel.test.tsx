import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, beforeEach } from 'vitest';

import { SectionPanel } from './SectionPanel';

const dietPlanner = {
  name: 'diet-planner',
  basePath: '/diet-planner',
  translationKey: 'common.diet_planner',
  icon: () => <svg />,
  navItems: [
    {
      name: 'Dashboard',
      href: '/diet-planner',
      icon: () => <svg />,
      translationKey: 'common.dashboard',
      group: 'nav_groups.plan',
    },
    {
      name: 'Products',
      href: '/diet-planner/products',
      icon: () => <svg />,
      translationKey: 'common.products',
      group: 'nav_groups.library',
    },
    {
      name: 'Preferences',
      href: '/diet-planner/preferences',
      icon: () => <svg />,
      translationKey: 'common.preferences',
      group: '__settings__',
    },
  ],
};

vi.mock('@shared/lib/module-registry', async (orig) => {
  // eslint-disable-next-line @typescript-eslint/consistent-type-imports
  const actual = await orig<typeof import('@shared/lib/module-registry')>();
  return { ...actual, getModules: () => [dietPlanner] };
});

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}));

function renderPanel(pathname = '/diet-planner/products') {
  return render(
    <MemoryRouter initialEntries={[pathname]}>
      <SectionPanel />
    </MemoryRouter>,
  );
}

beforeEach(() => {
  window.localStorage.clear();
});

describe('SectionPanel', () => {
  it('renders grouped links for the active module, with headings', () => {
    renderPanel();

    expect(screen.getByText('nav_groups.plan')).toBeInTheDocument();
    expect(screen.getByText('nav_groups.library')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /common\.dashboard/ })).toHaveAttribute(
      'href',
      '/diet-planner',
    );
  });

  it('renders NAV_GROUP_SETTINGS items pinned below, without a group heading', () => {
    renderPanel();

    const prefsLink = screen.getByRole('link', { name: /common\.preferences/ });
    expect(prefsLink).toHaveAttribute('href', '/diet-planner/preferences');
    expect(screen.queryByText('__settings__')).not.toBeInTheDocument();
  });

  it('collapses to icon-only on the chevron click and persists the choice', async () => {
    renderPanel();

    await userEvent.click(screen.getByRole('button', { name: 'common.collapse_nav' }));

    expect(screen.queryByText('nav_groups.plan')).not.toBeInTheDocument();
    expect(window.localStorage.getItem('home-system-nav-collapsed')).toBe('1');
  });

  it('renders nothing when the path matches no registered module', () => {
    const { container } = renderPanel('/');
    expect(container).toBeEmptyDOMElement();
  });
});
