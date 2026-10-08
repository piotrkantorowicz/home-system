import { render, screen } from '@testing-library/react';
import { MemoryRouter, useLocation } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

import Profile from './Profile';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}));
vi.mock('@modules/diet-planner/components/settings', () => ({
  BodyStatsForm: () => <div>body-stats-form</div>,
  DietReminderSettingsForm: () => <div>reminders-form</div>,
  EnergyModel: () => <div>energy-model</div>,
  GoalsForm: () => <div>goals-form</div>,
  HydrationConfigForm: () => <div>water-form</div>,
  MealScheduleForm: () => <div>meal-form</div>,
  WeightHistorySection: () => <div>weight-history</div>,
}));

Element.prototype.scrollIntoView = vi.fn();

function Where() {
  const { search, hash } = useLocation();
  return <div data-testid="where">{`${search}|${hash}`}</div>;
}

function renderAt(url: string) {
  return render(
    <MemoryRouter initialEntries={[url]}>
      <Profile />
      <Where />
    </MemoryRouter>,
  );
}

describe('Profile settings page', () => {
  it('shows only the profile tab by default', () => {
    renderAt('/diet-planner/profile');
    expect(screen.getByText('body-stats-form')).toBeInTheDocument();
    expect(screen.getByText('weight-history')).toBeInTheDocument();
    expect(screen.queryByText('goals-form')).not.toBeInTheDocument();
    const nav = screen.getByRole('navigation', { name: 'profile.tabs' });
    expect(nav.querySelectorAll('a')).toHaveLength(5);
    expect(screen.getByRole('link', { name: 'profile.tab_profile' })).toHaveAttribute(
      'aria-current',
      'page',
    );
  });

  it.each([
    ['#goals', 'goals-form', 'profile.tab_goals'],
    ['#meal-times', 'meal-form', 'profile.tab_meal_times'],
    ['#water', 'water-form', 'profile.tab_water'],
    ['#reminders', 'reminders-form', 'profile.tab_reminders'],
    ['#profile-details', 'body-stats-form', 'profile.tab_profile'],
  ])('hash %s selects its tab', (hash, form, tab) => {
    renderAt(`/diet-planner/profile${hash}`);
    expect(screen.getByText(form)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: tab })).toHaveAttribute('aria-current', 'page');
    expect(screen.getAllByText(/-form$/)).toHaveLength(1);
  });

  it.each([
    ['goals', '#goals'],
    ['meal-schedule', '#meal-times'],
    ['hydration', '#water'],
    ['notifications', '#reminders'],
    ['body-stats', '#profile'],
    ['bogus', '#profile'],
  ])('redirects ?section=%s to %s', (section, hash) => {
    renderAt(`/diet-planner/profile?section=${section}`);
    expect(screen.getByTestId('where')).toHaveTextContent(`|${hash}`);
  });
});
