import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach } from 'vitest';

import Preferences from './Preferences';

const setTheme = vi.fn();
const changeLanguage = vi.fn();

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: { language: 'en', changeLanguage },
  }),
}));
vi.mock('@shared/context/ThemeContext', () => ({
  useTheme: () => ({ theme: 'system', setTheme }),
}));
vi.mock('react-oidc-context', () => ({
  useAuth: () => ({
    user: { profile: { name: 'Kamil M', email: 'k@example.com' } },
    signoutRedirect: vi.fn(),
  }),
}));

beforeEach(() => {
  window.localStorage.clear();
  setTheme.mockClear();
  changeLanguage.mockClear();
});

describe('Preferences', () => {
  it('sets the theme when a preview tile is clicked', async () => {
    render(<Preferences />);
    await userEvent.click(screen.getByRole('button', { name: /preferences\.theme_dark/i }));
    expect(setTheme).toHaveBeenCalledWith('dark');
  });

  it('changes the language through the segmented control', async () => {
    render(<Preferences />);
    await userEvent.click(screen.getByRole('radio', { name: 'Polski' }));
    expect(changeLanguage).toHaveBeenCalledWith('pl');
  });

  it('persists a unit change to localStorage', async () => {
    render(<Preferences />);
    await userEvent.selectOptions(
      screen.getByRole('combobox', { name: 'preferences.weight' }),
      'lb',
    );
    const raw = window.localStorage.getItem('home-system-prefs');
    expect(raw).toContain('"weightUnit":"lb"');
  });
});
