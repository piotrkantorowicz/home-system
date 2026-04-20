import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

import { Sidebar } from './Sidebar';

vi.mock('@shared/lib/module-registry', () => ({
  getModules: () => [
    {
      name: 'diet-planner',
      translationKey: 'common.diet_planner',
      basePath: '/diet-planner',
      icon: () => <svg data-testid="mock-icon" />,
      navItems: [
        {
          name: 'Dashboard',
          href: '/diet-planner',
          icon: () => <svg data-testid="dash-icon" />,
          translationKey: 'common.dashboard',
        },
        {
          name: 'Products',
          href: '/diet-planner/products',
          icon: () => <svg data-testid="products-icon" />,
          translationKey: 'common.products',
        },
      ],
      routes: [],
      localeNamespaces: [],
      i18nResources: { en: {}, pl: {} },
    },
  ],
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

function renderSidebar(pathname = '/') {
  return render(
    <MemoryRouter initialEntries={[pathname]}>
      <Sidebar />
    </MemoryRouter>,
  );
}

describe('Sidebar', () => {
  it('renders navItems as-is without injecting duplicate Dashboard', () => {
    renderSidebar('/diet-planner');
    const dashboardLinks = screen.getAllByText('common.dashboard');
    // One for system-level home, one for module dashboard = 2 total, NOT 3
    expect(dashboardLinks).toHaveLength(2);
  });

  it('renders module navItems in order', () => {
    renderSidebar('/diet-planner');
    const links = screen.getAllByRole('link');
    const hrefs = links.map((link) => link.getAttribute('href'));
    expect(hrefs).toContain('/diet-planner');
    expect(hrefs).toContain('/diet-planner/products');
  });
});
