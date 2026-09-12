import { NavigationAccessContext } from '@shared/context/NavigationAccessContext';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, useLocation } from 'react-router-dom';
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

function LocationProbe() {
  return <output aria-label="Current path">{useLocation().pathname}</output>;
}

function renderRail(pathname = '/diet-planner', restricted = false) {
  return render(
    <MemoryRouter initialEntries={[pathname]}>
      <NavigationAccessContext
        value={restricted ? { allowedPath: '/household', reason: 'Create your home first' } : null}
      >
        <ModuleRail />
        <LocationProbe />
      </NavigationAccessContext>
    </MemoryRouter>,
  );
}

describe('ModuleRail', () => {
  it('does not navigate to another module before household setup', async () => {
    renderRail('/household', true);
    const tile = screen.getByRole('button', { name: 'common.diet_planner' });
    expect(tile).toBeDisabled();
    expect(tile).toHaveAttribute('title', 'Create your home first');
    await userEvent.click(tile);
    expect(screen.getByLabelText('Current path')).toHaveTextContent('/household');
    expect(tile).not.toHaveAttribute('aria-current');
  });

  it('disables module switcher destinations during setup', async () => {
    renderRail('/household', true);
    await userEvent.click(screen.getByRole('button', { name: 'common.switch_module' }));
    expect(await screen.findByRole('menuitem', { name: 'common.diet_planner' })).toHaveAttribute(
      'aria-disabled',
      'true',
    );
    expect(screen.getByText('Create your home first')).toBeInTheDocument();
  });

  it('allows module navigation once setup is complete', async () => {
    renderRail('/household');
    await userEvent.click(screen.getByRole('button', { name: 'common.diet_planner' }));
    expect(screen.getByLabelText('Current path')).toHaveTextContent('/diet-planner');
  });
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
