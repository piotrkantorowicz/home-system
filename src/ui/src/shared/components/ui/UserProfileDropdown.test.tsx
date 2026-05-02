import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

import { UserProfileDropdown } from './UserProfileDropdown';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: { language: 'en', changeLanguage: vi.fn() },
  }),
}));

vi.mock('@shared/context/ThemeContext', () => ({
  useTheme: () => ({ theme: 'light', setTheme: vi.fn() }),
}));

const defaultProps = {
  displayName: 'John Doe',
  email: 'john@example.com',
  onLogout: vi.fn(),
};

function renderDropdown(props = defaultProps) {
  return render(
    <MemoryRouter>
      <UserProfileDropdown {...props} />
    </MemoryRouter>,
  );
}

describe('UserProfileDropdown', () => {
  it('renders the trigger button with user avatar', () => {
    renderDropdown();
    expect(screen.getByRole('button', { name: /common.user_menu/i })).toBeInTheDocument();
  });

  it('shows menu items when opened', async () => {
    const user = userEvent.setup();
    renderDropdown();

    await user.click(screen.getByRole('button', { name: /common.user_menu/i }));

    expect(screen.getByText('common.profile')).toBeInTheDocument();
    expect(screen.getByText('common.notifications')).toBeInTheDocument();
    expect(screen.getByText('common.logout')).toBeInTheDocument();
  });

  it('calls onLogout when logout is clicked', async () => {
    const onLogout = vi.fn();
    const user = userEvent.setup();
    renderDropdown({ ...defaultProps, onLogout });

    await user.click(screen.getByRole('button', { name: /common.user_menu/i }));
    await user.click(screen.getByText('common.logout'));

    expect(onLogout).toHaveBeenCalledOnce();
  });

  it('contains navigation links to settings pages', async () => {
    const user = userEvent.setup();
    renderDropdown();

    await user.click(screen.getByRole('button', { name: /common.user_menu/i }));

    const profileLink = screen.getByText('common.profile').closest('a');
    expect(profileLink).toHaveAttribute('href', '/diet-planner/profile');

    const notificationsLink = screen.getByText('common.notifications').closest('a');
    expect(notificationsLink).toHaveAttribute('href', '/notifications');
  });
});
