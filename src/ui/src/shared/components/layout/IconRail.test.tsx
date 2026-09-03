import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

import { IconRail } from './IconRail';

vi.mock('@shared/lib/module-registry', () => ({
  getModules: () => [
    {
      name: 'diet-planner',
      basePath: '/diet-planner',
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
    },
  ],
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}));

vi.mock('@shared/context/ThemeContext', () => ({
  useTheme: () => ({ resolvedTheme: 'light', setTheme: vi.fn() }),
}));

vi.mock('react-oidc-context', () => ({
  useAuth: () => ({ user: { profile: { name: 'Kamil Malinowski' } } }),
}));

function renderRail(pathname = '/') {
  return render(
    <MemoryRouter initialEntries={[pathname]}>
      <IconRail />
    </MemoryRouter>,
  );
}

describe('IconRail', () => {
  it('renders the system home and every module nav item as links', () => {
    renderRail();
    const hrefs = screen.getAllByRole('link').map((l) => l.getAttribute('href'));
    expect(hrefs).toContain('/');
    expect(hrefs).toContain('/diet-planner');
    expect(hrefs).toContain('/diet-planner/products');
  });

  it('marks the active route with the accent pill', () => {
    renderRail('/diet-planner/products');
    const active = screen.getByRole('link', { name: /common\.products/ });
    expect(active.className).toContain('bg-accent');
  });

  it('shows a theme toggle and an initials avatar', () => {
    renderRail();
    expect(screen.getByRole('button', { name: /dark theme/i })).toBeInTheDocument();
    expect(screen.getByText('KM')).toBeInTheDocument();
  });
});
