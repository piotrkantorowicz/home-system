import { __resetPreferences } from '@shared/hooks/usePreferences';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';

import Preferences from './Preferences';

const setTheme = vi.fn();
const changeLanguage = vi.fn();
const signoutRedirect = vi.fn();

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: { language: 'en', changeLanguage },
  }),
}));
vi.mock('@modules/notifications', () => ({
  ChannelPreferencesSection: () => <div>channel-section</div>,
}));
vi.mock('@shared/context/ThemeContext', () => ({
  useTheme: () => ({ theme: 'system', setTheme }),
}));
vi.mock('react-oidc-context', () => ({
  useAuth: () => ({
    user: { profile: { name: 'Kamil M', email: 'k@example.com' } },
    signoutRedirect,
  }),
}));

function renderPage() {
  return render(
    <MemoryRouter>
      <Preferences />
    </MemoryRouter>,
  );
}

beforeEach(() => {
  window.localStorage.clear();
  __resetPreferences();
  setTheme.mockClear();
  changeLanguage.mockClear();
});

describe('Preferences', () => {
  it('sets the theme when a preview tile is clicked', async () => {
    renderPage();
    await userEvent.click(screen.getByRole('button', { name: /preferences\.theme_dark/i }));
    expect(setTheme).toHaveBeenCalledWith('dark');
  });

  it('changes the language through the segmented control', async () => {
    renderPage();
    await userEvent.click(screen.getByRole('radio', { name: 'Polski' }));
    expect(changeLanguage).toHaveBeenCalledWith('pl');
  });

  it('persists a unit change to localStorage', async () => {
    renderPage();
    await userEvent.selectOptions(
      screen.getByRole('combobox', { name: 'preferences.weight' }),
      'lb',
    );
    const raw = window.localStorage.getItem('home-system-prefs');
    expect(raw).toContain('"weightUnit":"lb"');
  });

  it('hosts the notification channels and links back to diet settings', () => {
    renderPage();
    expect(screen.getByText('channel-section')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /preferences\.diet_settings/ })).toHaveAttribute(
      'href',
      '/diet-planner/profile',
    );
  });

  it('shows a live example in the chosen units and no delete action', async () => {
    renderPage();
    expect(screen.getByText(/2.150 kcal · 72\.5 kg · 250 ml/)).toBeInTheDocument();
    await userEvent.selectOptions(
      screen.getByRole('combobox', { name: 'preferences.weight' }),
      'lb',
    );
    expect(screen.getByText(/159\.8 lb/)).toBeInTheDocument();
    expect(screen.getByText('preferences.delete_unavailable')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /delete/i })).not.toBeInTheDocument();
  });

  it('signs out through the account row', async () => {
    renderPage();
    await userEvent.click(screen.getByRole('button', { name: /preferences\.sign_out/ }));
    expect(signoutRedirect).toHaveBeenCalled();
  });
});
