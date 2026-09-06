import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

import { ModuleRail } from './ModuleRail';

vi.mock('@shared/lib/module-registry', () => ({
  getModules: () => [
    {
      name: 'diet-planner',
      basePath: '/diet-planner',
      translationKey: 'common.diet_planner',
      icon: () => <svg data-testid="dp-icon" />,
      navItems: [],
    },
    {
      name: 'notifications',
      basePath: '/notifications',
      translationKey: 'common.notifications',
      icon: () => <svg data-testid="notif-icon" />,
      navItems: [],
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
  useAuth: () => ({
    user: { profile: { name: 'Kamil Malinowski' } },
    signoutRedirect: vi.fn(),
  }),
}));

function renderRail(pathname = '/diet-planner') {
  return render(
    <MemoryRouter initialEntries={[pathname]}>
      <ModuleRail />
    </MemoryRouter>,
  );
}

describe('ModuleRail', () => {
  it('renders a tile for every registered module', () => {
    renderRail();
    expect(screen.getByRole('button', { name: 'common.diet_planner' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'common.notifications' })).toBeInTheDocument();
  });

  it('marks the active module tile with aria-current', () => {
    renderRail('/diet-planner/products');
    expect(screen.getByRole('button', { name: 'common.diet_planner' })).toHaveAttribute(
      'aria-current',
      'page',
    );
    expect(screen.getByRole('button', { name: 'common.notifications' })).not.toHaveAttribute(
      'aria-current',
    );
  });

  it('shows a theme toggle and the profile menu trigger', () => {
    renderRail();
    expect(screen.getByRole('button', { name: /dark theme/i })).toBeInTheDocument();
    expect(screen.getByText('KM')).toBeInTheDocument();
  });
});
